using FanslationStudio.LlmKit.Support;
using FanslationStudio.LlmKit.Workflow;
using System.Text.RegularExpressions;
using Translate;
using Translate.Utility;
using YamlDotNet.Serialization;

public class FileOutputHandling
{
    public static async Task PackageFinalTranslationAsync(string workingDirectory)
    {
        string inputPath = $"{workingDirectory}/Converted";
        string outputPath = $"{workingDirectory}/Mod";

        if (Directory.Exists(outputPath))
            Directory.Delete(outputPath, true);

        Directory.CreateDirectory(outputPath);
        string fileOutputPath = $"{outputPath}/English";
        Directory.CreateDirectory(fileOutputPath);

        var finalDb = new List<string>();
        var passedCount = 0;
        var failedCount = 0;

        foreach (var textFile in GameTextFiles.TextFilesToSplit.Where(f => f.TextFileType == TextFileType.PrefabText))
        {
            var (passed, failed) = await PrefabTextWorkflow.PackagePrefabTextAsync(workingDirectory, textFile);
            passedCount += passed;
            failedCount += failed;
            MoveLlmKitPackagedFileIntoEnglishFolder(workingDirectory, fileOutputPath, textFile.Path);
        }

        foreach (var textFile in GameTextFiles.TextFilesToSplit.Where(f => f.TextFileType == TextFileType.DynamicStrings))
        {
            var (passed, failed) = await DynamicStringsCecilWorkflow.PackageDynamicStringsCecilAsync(workingDirectory, textFile);
            passedCount += passed;
            failedCount += failed;
            MoveLlmKitPackagedFileIntoEnglishFolder(workingDirectory, fileOutputPath, textFile.Path);
        }

        var jsonFiles = GameTextFiles.TextFilesToSplit.Where(f => f.TextFileType == TextFileType.RawCsv).ToArray();

        await global::FileIteration.IterateTranslatedFilesAsync(workingDirectory, jsonFiles, async (outputFile, textFileToTranslate, fileLines) =>
        {
            // Convert fileLines back into the original JSON array format
            var jsonArray = new List<Dictionary<string, object>>();

            foreach (var line in fileLines)
            {
                var jsonObject = new Dictionary<string, object>();

                // Add the Key property from RawIndex
                if (int.TryParse(line.RawIndex, out int key))
                {
                    jsonObject["Key"] = key;
                }
                else
                {
                    // If RawIndex is not an int, use it as-is (fallback)
                    jsonObject["Key"] = line.RawIndex;
                }

                // Add each split as a property
                foreach (var split in line.Splits)
                {
                    var arrayMatch = Regex.Match(split.SplitPath, @"^(.+)\[(\d+)\]$");
                    if (arrayMatch.Success)
                    {
                        var propertyName = arrayMatch.Groups[1].Value;
                        var index = int.Parse(arrayMatch.Groups[2].Value);

                        // Initialize the list from the original JSON the first time we see this property
                        if (!jsonObject.ContainsKey(propertyName))
                        {
                            using var originalDoc = System.Text.Json.JsonDocument.Parse(line.Raw);
                            if (originalDoc.RootElement.TryGetProperty(propertyName, out var originalArray)
                                && originalArray.ValueKind == System.Text.Json.JsonValueKind.Array)
                            {
                                jsonObject[propertyName] = originalArray.EnumerateArray()
                                    .Select(e => e.ValueKind == System.Text.Json.JsonValueKind.String ? e.GetString() ?? string.Empty : string.Empty)
                                    .ToList();
                            }
                            else
                            {
                                jsonObject[propertyName] = new List<string>();
                            }
                        }

                        var list = (List<string>)jsonObject[propertyName];
                        if (split.FlaggedForRetranslation)
                        {
                            failedCount++;
                        }
                        else if (index < list.Count)
                        {
                            list[index] = string.IsNullOrEmpty(split.Translated) ? split.Text : split.Translated;
                            passedCount++;
                        }
                    }
                    else if (split.FlaggedForRetranslation)
                    {
                        // Use original text and increment failed count
                        jsonObject[split.SplitPath] = split.Text;
                        failedCount++;
                    }
                    else
                    {
                        // Use translated text (or fallback to original if empty) and increment passed count
                        jsonObject[split.SplitPath] = string.IsNullOrEmpty(split.Translated) ? split.Text : split.Translated;
                        passedCount++;
                    }
                }

                jsonArray.Add(jsonObject);
            }

            // Serialize to JSON and write to output file
            var jsonOptions = new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var jsonContent = System.Text.Json.JsonSerializer.Serialize(jsonArray, jsonOptions);

            File.WriteAllText($"{fileOutputPath}/{textFileToTranslate.Path}", jsonContent);

            await Task.CompletedTask;
        });


        Console.WriteLine($"Passed: {passedCount}");
        Console.WriteLine($"Failed: {failedCount}");
    }

    /// <summary>
    /// LlmKit's Prefab/DynamicStrings workflows always write their packaged output to
    /// Mod/{path}.yaml. This repo's own convention (see FileOutputWorkflowTests/ZipRelease) copies
    /// the whole Mod/English folder into the game's BepInEx/english folder, so the packaged file is
    /// moved there under its original name (no ".yaml" suffix) to preserve that existing contract.
    /// </summary>
    private static void MoveLlmKitPackagedFileIntoEnglishFolder(string workingDirectory, string fileOutputPath, string path)
    {
        var source = $"{workingDirectory}/Mod/{path}.yaml";
        if (File.Exists(source))
            File.Move(source, $"{fileOutputPath}/{path}", true);
    }

    public static void CopyDirectory(string sourceDir, string destDir)
    {
        // Get the subdirectories for the specified directory.
        var dir = new DirectoryInfo(sourceDir);

        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory does not exist or could not be found: {sourceDir}");

        // If the destination directory doesn't exist, create it.
        if (!Directory.Exists(destDir))
            Directory.CreateDirectory(destDir);

        // Get the files in the directory and copy them to the new location.
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            var tempPath = Path.Combine(destDir, file.Name);
            file.CopyTo(tempPath, true);
        }

        // Copy each subdirectory using recursion
        DirectoryInfo[] dirs = dir.GetDirectories();
        foreach (DirectoryInfo subdir in dirs)
        {
            if (subdir.Name == ".git" || subdir.Name == ".vs")
                continue;

            var tempPath = Path.Combine(destDir, subdir.Name);
            CopyDirectory(subdir.FullName, tempPath);
        }
    }
}