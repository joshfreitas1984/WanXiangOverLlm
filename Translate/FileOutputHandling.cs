using SharedAssembly.DynamicStrings;
using System.Text.RegularExpressions;
using Translate;
using Translate.Utility;

public class FileOutputHandling
{
    public static async Task PackageFinalTranslationAsync(string workingDirectory)
    {
        string inputPath = $"{workingDirectory}/Converted";
        string outputPath = $"{workingDirectory}/Mod";

        if (Directory.Exists(outputPath))
            Directory.Delete(outputPath, true);

        Directory.CreateDirectory(outputPath);
        Directory.CreateDirectory($"{outputPath}/English");

        var finalDb = new List<string>();
        var passedCount = 0;
        var failedCount = 0;

        await FileIteration.IterateTranslatedFilesAsync(workingDirectory, async (outputFile, textFileToTranslate, fileLines) =>
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

            File.WriteAllText($"{outputPath}/English/{textFileToTranslate.Path}", jsonContent);

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