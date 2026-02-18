using SharedAssembly.DynamicStrings;
using System.Diagnostics.Contracts;
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

        await FileIteration.IterateTranslatedFilesAsync(workingDirectory, async (outputFile, textFileToTranslate, fileLines) =>
        {
            if (textFileToTranslate.TextFileType == TextFileType.PrefabText)
            {
                var outputLines = new List<string>();

                foreach (var line in fileLines)
                {
                    foreach (var split in line.Splits)
                        if (!split.FlaggedForRetranslation && !(string.IsNullOrEmpty(split.Translated)))
                            outputLines.Add($"- raw: {split.Text}\n  result: {split.Translated}");
                        else if (!split.SafeToTranslate)
                            continue; // Do not count failure
                        else
                            failedCount++;
                }

                File.WriteAllLines($"{fileOutputPath}/{textFileToTranslate.Path}", outputLines);
                return;
            }
            else if (textFileToTranslate.TextFileType == TextFileType.DynamicStrings)
            {
                var serializer = Yaml.CreateSerializer();
                var contracts = new List<DynamicStringContract>();

                foreach (var line in fileLines)
                {
                    if (line.Splits.Count != 1)
                    {
                        failedCount++;
                        continue;
                    }

                    // Do not package but dont count as failure
                    if (!line.Splits[0].SafeToTranslate)
                        continue;

                    var lineRaw = line.Raw;
                    var splits = lineRaw.Split(",");

                    var lineTrans = line.Splits[0].Translated
                        .Replace("，", ","); // Replace Wide quotes back

                    if (splits.Length != 5
                        || string.IsNullOrEmpty(lineTrans)
                        || line.Splits[0].FlaggedForRetranslation)
                    {
                        failedCount++;
                        continue;
                    }

                    string[] parameters = DynamicStringSupport.PrepareMethodParameters(splits[4]);

                    var contract = new DynamicStringContract()
                    {
                        Type = splits[0],
                        Method = splits[1],
                        ILOffset = long.Parse(splits[2]),
                        Raw = splits[3],
                        Translation = lineTrans,
                        Parameters = parameters
                    };

                    if (DynamicStringSupport.IsSafeContract(contract, false))
                        contracts.Add(contract);
                }

                File.WriteAllText($"{fileOutputPath}/{textFileToTranslate.Path}", serializer.Serialize(contracts));
                passedCount += contracts.Count;

                await Task.CompletedTask;
                return;
            }


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
                    if (split.FlaggedForRetranslation)
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
            file.CopyTo(tempPath, false);
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