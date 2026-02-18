using System.Text.RegularExpressions;
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

                var key = keyElement.GetInt32();
                var line = new TranslationLine
                {
                    Raw = entry.GetRawText(),
                    RawIndex = key.ToString(),
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
            if (!File.Exists($"{convertedPath}/{file.Name}"))
                File.Copy($"{outputPath}/{file.Name}", $"{convertedPath}/{file.Name}");
        }
    }

    public static async Task MergeFilesIntoTranslatedAsync(string workingDirectory)
    {
        await FileIteration.IterateTranslatedFilesAsync(workingDirectory, async (outputFile, textFileToTranslate, fileLines) =>
        {
            var newCount = 0;

            ////Disable for now since they should be same
            //if (textFileToTranslate.TextFileType == TextFileType.RegularDb)
            //    return;

            var deserializer = Yaml.CreateDeserializer();
            var exportFile = outputFile.Replace("Converted", "Raw/Export");
            var exportLines = deserializer.Deserialize<List<TranslationLine>>(File.ReadAllText(exportFile));

            foreach (var line in exportLines)
            {
                if (textFileToTranslate.TextFileType == TextFileType.RegularDb)
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
                else if (textFileToTranslate.TextFileType == TextFileType.DynamicStrings)
                {
                    var found = fileLines.FirstOrDefault(x => x.Raw == line.Raw);
                    if (found != null)
                    {
                        foreach (var split in line.Splits)
                        {
                            var found2 = found.Splits.FirstOrDefault(x => x.Text == split.Text);
                            if (found2 != null)
                                split.Translated = found2.Translated;
                        }
                    }
                    else
                    {
                        // Try matching on split instead of line incase they changed line format
                        foreach (var split in line.Splits)
                        {
                            var found2 = fileLines
                                .Select(x => x.Splits.FirstOrDefault(s => s.Text == split.Text))
                                .FirstOrDefault(s => s != null);

                            if (found2 != null)
                                split.Translated = found2.Translated;
                            else
                                newCount++;
                        }
                    }
                }
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

    public static void ExportDumpedPrefabToCustomFormat(string workingDirectory)
    {
        string inputPath = $"{workingDirectory}/Raw/ExportedText";
        string outputPath = $"{workingDirectory}/Raw/Export";

        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        var serializer = Yaml.CreateSerializer();
        var pattern = LineValidation.ChineseCharPattern;

        var dir = new DirectoryInfo(inputPath);
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            var foundLines = new List<TranslationLine>();
            var lines = File.ReadAllLines(file.FullName);
            var lineIncrement = 0;

            foreach (var line in lines)
            {
                lineIncrement++;
                var splits = new string[] { line };
                var foundSplits = new List<TranslationSplit>();

                // Default to line number when it doesnt have line number in split
                if (!long.TryParse(splits[0], out long lineNum))
                    lineNum = lineIncrement;

                // Find Chinese
                for (int i = 0; i < splits.Length; i++)
                {
                    if (Regex.IsMatch(splits[i], pattern))
                    {
                        foundSplits.Add(new TranslationSplit()
                        {
                            Split = i,
                            Text = splits[i],
                        });
                    }
                }

                //The translation line
                foundLines.Add(new TranslationLine()
                {
                    //LineNum = lineNum,
                    Raw = line,
                    Splits = foundSplits,
                });
            }

            // Write the found lines
            var yaml = serializer.Serialize(foundLines);
            File.WriteAllText($"{outputPath}/{file.Name}", yaml);
        }
    }

    public static void ExportDynamicStringsToCustomFormat(string workingDirectory)
    {
        string inputFile = $"{workingDirectory}/Raw/Dumped/dynamicStrings.txt";
        string outputFile = $"{workingDirectory}/Raw/Export/dynamicStrings.txt";

        var serializer = Yaml.CreateSerializer();
        var pattern = LineValidation.ChineseCharPattern;

        var foundLines = new List<TranslationLine>();
        var lines = File.ReadAllLines(inputFile);
        var lineIncrement = 0;

        foreach (var line in lines)
        {
            lineIncrement++;
            var splits = line.Split(",");
            var foundSplits = new List<TranslationSplit>();

            // Default to line number when it doesnt have line number in split
            if (!long.TryParse(splits[0], out long lineNum))
                lineNum = lineIncrement;

            // Find Chinese
            for (int i = 0; i < splits.Length; i++)
            {
                if (Regex.IsMatch(splits[i], pattern))
                {
                    var cleaned = splits[i];
                    if (cleaned.StartsWith('\"'))
                        cleaned = cleaned[1..];
                    if (cleaned.EndsWith('\"'))
                        cleaned = cleaned[..^1];

                    foundSplits.Add(new TranslationSplit()
                    {
                        Split = i,
                        Text = cleaned,
                    });
                }
            }

            //The translation line
            foundLines.Add(new TranslationLine()
            {
                //LineNum = lineNum,
                Raw = line,
                Splits = foundSplits,
            });
        }

        // Write the found lines
        var yaml = serializer.Serialize(foundLines);
        File.WriteAllText($"{outputFile}", yaml);
    }

}
