using System.Text.RegularExpressions;
using FanslationStudio.LlmKit.Support;
using FanslationStudio.LlmKit.Workflow;
using Translate.Utility;


namespace Translate;

public class InputFileHandling
{
    public static void ExportTextAssetsToCustomFormat(string workingDirectory)
    {
        string inputPath = $"{workingDirectory}/Raw/Dumped";
        string outputPath = $"{workingDirectory}/Raw/Export";
        string convertedPath = $"{workingDirectory}/Converted";

        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        if (!Directory.Exists(convertedPath))
            Directory.CreateDirectory(convertedPath);

        var serializer = Yaml.CreateSerializer();
        var pattern = LineValidation.ChineseCharPattern;

        var dir = new DirectoryInfo(inputPath);
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            if (!file.FullName.EndsWith("json"))
                continue;

            var foundLines = new List<TranslationLine>();

            // 1. Open the file as json - it is an array of objects with Key property and string properties
            // 2. Turn each entry into a TranslationLine
            //      - Raw = JSON serialized object (for reference)
            //      - RawIndex = the Key property of the object (to be used for merging back later)
            // 3. For each object, find properties with Chinese text and add to the Splits list
            //    - SplitPath = the name of the property that has chinese in it (to be used for merging back later)
            //    - Text = the value of the property that has chinese in it
            // 4. Add to foundLines and write to yaml

            var jsonContent = File.ReadAllText(file.FullName);
            using var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonContent);
            var entries = jsonDoc.RootElement;

            if (entries.ValueKind != System.Text.Json.JsonValueKind.Array)
                continue;

            foreach (var entry in entries.EnumerateArray())
            {
                // Extract the Key property
                if (!entry.TryGetProperty("Key", out var keyElement))
                    continue;

                var key = keyElement.ToString();
                var line = new TranslationLine
                {
                    Raw = entry.GetRawText(),
                    RawIndex = key,
                    Splits = new List<TranslationSplit>()
                };

                // Iterate through all properties to find Chinese text
                foreach (var property in entry.EnumerateObject())
                {
                    // Skip the Key property itself
                    if (property.Name == "Key")
                        continue;

                    if (property.Name.ToLower().Replace("list", "").EndsWith("tw"))
                        continue;

                    if (property.Name.ToLower().Replace("list", "").EndsWith("final"))
                        continue;

                    if (property.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        var text = property.Value.GetString();
                        if (!string.IsNullOrEmpty(text) && Regex.IsMatch(text, pattern))
                        {
                            line.Splits.Add(new TranslationSplit
                            {
                                SplitPath = property.Name,
                                Text = text
                            });
                        }
                    }
                    else if (property.Value.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        var index = 0;
                        foreach (var arrayElement in property.Value.EnumerateArray())
                        {
                            if (arrayElement.ValueKind == System.Text.Json.JsonValueKind.String)
                            {
                                var text = arrayElement.GetString();
                                if (!string.IsNullOrEmpty(text) && Regex.IsMatch(text, pattern))
                                {
                                    line.Splits.Add(new TranslationSplit
                                    {
                                        SplitPath = $"{property.Name}[{index}]",
                                        Text = text,
                                        Split = index
                                    });
                                }
                            }
                            index++;
                        }
                    }
                }

                if (line.Splits.Count > 0)
                    foundLines.Add(line);
            }

            // Write the found lines
            var yaml = serializer.Serialize(foundLines);
            File.WriteAllText($"{outputPath}/{file.Name}", yaml);

            // Add missing converted file if it doesnt exist yet
            if (!File.Exists($"{convertedPath}/{file.Name}.yaml"))
                File.Copy($"{outputPath}/{file.Name}", $"{convertedPath}/{file.Name}.yaml");
        }
    }

    public static async Task MergeFilesIntoTranslatedAsync(string workingDirectory)
    {
        var llmKitFiles = GameTextFiles.TextFilesToSplit
            .Where(f => f.TextFileType is TextFileType.PrefabText or TextFileType.DynamicStrings)
            .ToArray();

        if (llmKitFiles.Length > 0)
            await FanslationStudio.LlmKit.GameFileHandlingBase.MergeFilesIntoTranslatedAsync(workingDirectory, llmKitFiles);

        var jsonFiles = GameTextFiles.TextFilesToSplit
            .Where(f => f.TextFileType == TextFileType.RawCsv)
            .ToArray();

        await global::FileIteration.IterateTranslatedFilesAsync(workingDirectory, jsonFiles, async (outputFile, textFileToTranslate, fileLines) =>
        {
            var newCount = 0;

            var deserializer = Yaml.CreateDeserializer();
            var exportFile = outputFile.Replace("Converted", "Raw/Export").Replace(".yaml", "");
            var exportLines = deserializer.Deserialize<List<TranslationLine>>(File.ReadAllText(exportFile));

            foreach (var line in exportLines)
            {
                var found = fileLines.FirstOrDefault(x => x.RawIndex == line.RawIndex);
                if (found != null)
                {
                    foreach (var split in line.Splits)
                    {
                        var found2 = found.Splits.FirstOrDefault(x => x.SplitPath == split.SplitPath);
                        if (found2 != null)
                            split.Translated = found2.Translated;
                        else
                            newCount++;
                    }
                }
                else
                    newCount++;
            }

            Console.WriteLine($"New Lines {textFileToTranslate.Path}: {newCount}");

            //if (newCount > 0 || exportLines.Count != fileLines.Count) //Always Write because they might have changed format
            {
                var serializer = Yaml.CreateSerializer();
                File.WriteAllText(outputFile, serializer.Serialize(exportLines));
            }

            await Task.CompletedTask;
        });
    }

    /// <summary>
    /// Delegates to <see cref="PrefabTextWorkflow.ExportPrefabTextToCustomFormat"/> for every
    /// registered <see cref="TextFileType.PrefabText"/> entry. Currently a no-op - dumpedPrefabText.txt
    /// is commented out in <see cref="GameTextFiles.TextFilesToSplit"/> pending confirmation that
    /// EnglishPatch is ready to consume its packaged output (see
    /// docs/plans/llmkit-migration-piece1-prefab-dynamicstrings.md).
    /// </summary>
    public static void ExportDumpedPrefabToCustomFormat(string workingDirectory)
    {
        foreach (var textFile in GameTextFiles.TextFilesToSplit.Where(f => f.TextFileType == TextFileType.PrefabText))
            PrefabTextWorkflow.ExportPrefabTextToCustomFormat(workingDirectory, textFile);
    }

    public static void ExportDynamicStringsToCustomFormat(string workingDirectory)
    {
        foreach (var textFile in GameTextFiles.TextFilesToSplit.Where(f => f.TextFileType == TextFileType.DynamicStrings))
            DynamicStringsCecilWorkflow.ExportDynamicStringsToCustomFormat(workingDirectory, textFile);
    }

}
