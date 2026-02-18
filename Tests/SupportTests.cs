using Newtonsoft.Json.Linq;
using SweetPotato;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Translate.Support;
using Translate.Utility;
using Xunit.Sdk;

namespace Translate.Tests;

public class SupportTests
{
    const string workingDirectory = "../../../../Files";

    [Fact]
    public async Task GetNames()
    {
        var config = Configuration.GetConfiguration(workingDirectory,
            ""
            //$"{workingDirectory}/TestResults"
            );

        var names = new List<string>();

        await FileIteration.IterateTranslatedFilesAsync(workingDirectory, async (outputFile, textFileToTranslate, fileLines) =>
        {
            //if (textFileToTranslate.Path != "Hero.json")
            //    return;

            if (textFileToTranslate.Path != "Monster.json")
                return;

            foreach (var line in fileLines)
            {
                foreach (var split in line.Splits)
                {
                    if (split.SplitPath == "NameFinal")
                    {
                        if (!names.Contains(split.Text))
                            names.Add(split.Text);
                    }
                }

            }

            await Task.CompletedTask;
        });

        Console.WriteLine($"Found {names.Count} names");

        var client = new HttpClient();
        var glossary = new List<string>();
        int batchSize = 10;

        for (int i = 0; i < names.Count; i += batchSize)
        {
            var batch = names.Skip(i).Take(batchSize).ToList();
            var tasks = batch.Select(async (name) =>
            {
                glossary.Add($"- raw: {name}");
            }).ToArray();

            await Task.WhenAll(tasks);
        }

        File.WriteAllLines($"{workingDirectory}/TestResults/Export/Names.yaml", glossary);
    }

    [Fact]
    public async Task GetSects()
    {
        var config = Configuration.GetConfiguration(workingDirectory,
            ""
            //$"{workingDirectory}/TestResults"
            );

        var names = new List<string>();

        await FileIteration.IterateTranslatedFilesAsync(workingDirectory, async (outputFile, textFileToTranslate, fileLines) =>
        {
            //if (textFileToTranslate.Path != "Hero.json")
            //    return;

            if (textFileToTranslate.Path != "Dictionary.json")
                return;

            foreach (var line in fileLines)
            {
                foreach (var split in line.Splits)
                {
                    if (split.Text == "门派")
                    {
                        var sect = line.Splits.FirstOrDefault(s => s.SplitPath == "Dictionary")?.Text;

                        if (!string.IsNullOrEmpty(sect) && !names.Contains(sect))
                            names.Add(sect);
                    }
                }

            }

            await Task.CompletedTask;
        });

        Console.WriteLine($"Found {names.Count} names");

        var client = new HttpClient();
        var glossary = new List<string>();
        int batchSize = 10;

        for (int i = 0; i < names.Count; i += batchSize)
        {
            var batch = names.Skip(i).Take(batchSize).ToList();
            var tasks = batch.Select(async (name) =>
            {
                glossary.Add($"- raw: {name}");
            }).ToArray();

            await Task.WhenAll(tasks);
        }

        File.WriteAllLines($"{workingDirectory}/TestResults/Export/Sects.yaml", glossary);
    }

    [Fact]
    public async Task GetProps()
    {
        var config = Configuration.GetConfiguration(workingDirectory,
            ""
            //$"{workingDirectory}/TestResults"
            );

        var names = new List<(string, string)>();

        await FileIteration.IterateTranslatedFilesAsync(workingDirectory, async (outputFile, textFileToTranslate, fileLines) =>
        {
            if (textFileToTranslate.Path != "Property.json")
                return;

            foreach (var line in fileLines)
            {
                foreach (var split in line.Splits)
                {
                    if (split.SplitPath == "Name")
                    {
                        if (!names.Any(n => n.Item1 == split.Text))
                            names.Add((split.Text, split.Translated));
                    }
                }

            }

            await Task.CompletedTask;
        });

        var client = new HttpClient();
        var glossary = new List<string>();
        int batchSize = 10;

        for (int i = 0; i < names.Count; i += batchSize)
        {
            var batch = names.Skip(i).Take(batchSize).ToList();
            var tasks = batch.Select(async (name) =>
            {
                glossary.Add($"- raw: {name.Item1}");
                glossary.Add($"  result: {name.Item2}");
            }).ToArray();

            await Task.WhenAll(tasks);
        }

        File.WriteAllLines($"{workingDirectory}/TestResults/Export/Property.yaml", glossary);
    }
}