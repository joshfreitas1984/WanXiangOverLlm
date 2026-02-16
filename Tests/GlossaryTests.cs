using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Translate.Support;
using Translate.Utility;
using TriangleNet.Topology.DCEL;

namespace Translate.Tests;

public class GlossaryTests
{
    const string workingDirectory = "../../../../Files";

    [Fact]
    public void CheckForDuplicateGlossaryItems()
    {
        var config = Configuration.GetConfiguration(workingDirectory);

        // Check for duplicate `raw` entries in config.GlossaryLines and config.ManualTranslations
        var cache = new Dictionary<string, string>();
        var duplicates = new List<string>();
        var allEntries = config.GlossaryLines.Concat(config.ManualTranslations);

        foreach (var entry in allEntries)
        {
            if (cache.ContainsKey(entry.Raw))
            {
                if (!duplicates.Contains(entry.Raw))
                    duplicates.Add(entry.Raw);
            }
            else
            {
                cache.Add(entry.Raw, entry.Result);
            }
        }

        Directory.CreateDirectory($"{workingDirectory}/TestResults");
        File.WriteAllLines($"{workingDirectory}/TestResults/DupeGlossary.yaml", duplicates);

        // Check for entries in `raw` with similar `raw` but different `result` in config.GlossaryLines
        var similarEntries = new List<string>();
        var glossaryList = config.GlossaryLines.ToList();

        for (int i = 0; i < glossaryList.Count; i++)
        {
            for (int j = i + 1; j < glossaryList.Count; j++)
            {
                var entry1 = glossaryList[i];
                var entry2 = glossaryList[j];

                // Check if raw values are similar but results are different
                if (AreSimilar(entry1.Raw, entry2.Raw) && entry1.Result != entry2.Result)
                {
                    var similarPair = $"- raw1: \"{entry1.Raw}\" result1: \"{entry1.Result}\"" +
                                    $"\n  raw2: \"{entry2.Raw}\" result2: \"{entry2.Result}\"";
                    if (!similarEntries.Contains(similarPair))
                        similarEntries.Add(similarPair);
                }
            }
        }

        File.WriteAllLines($"{workingDirectory}/TestResults/SimilarGlossary.yaml", similarEntries);
    }

    private static bool AreSimilar(string str1, string str2)
    {
        if (str1 == str2)
            return false; // Exact duplicates are not "similar", they're identical

        // Check case-insensitive equality
        if (string.Equals(str1, str2, StringComparison.OrdinalIgnoreCase))
            return true;

        // Check trimmed equality
        if (str1.Trim() == str2.Trim())
            return true;

        // Check Levenshtein distance for close matches
        var distance = LevenshteinDistance(str1, str2);
        var maxLength = Math.Max(str1.Length, str2.Length);
        return maxLength > 0 && (double)distance / maxLength <= 0.2; // 20% difference threshold
    }

    private static int LevenshteinDistance(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1))
            return string.IsNullOrEmpty(s2) ? 0 : s2.Length;
        if (string.IsNullOrEmpty(s2))
            return s1.Length;

        var matrix = new int[s1.Length + 1, s2.Length + 1];

        for (int i = 0; i <= s1.Length; i++)
            matrix[i, 0] = i;
        for (int j = 0; j <= s2.Length; j++)
            matrix[0, j] = j;

        for (int i = 1; i <= s1.Length; i++)
        {
            for (int j = 1; j <= s2.Length; j++)
            {
                int cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(Math.Min(
                    matrix[i - 1, j] + 1,      // deletion
                    matrix[i, j - 1] + 1),     // insertion
                    matrix[i - 1, j - 1] + cost); // substitution
            }
        }

        return matrix[s1.Length, s2.Length];
    }

    [Fact]
    public void CleanupManualTranslations_RemovesEmptyEntries()
    {
        var config = Configuration.GetConfiguration(workingDirectory);

        var cache = new Dictionary<string, string>();
        var badDupes = new List<string>();
        var cleanedManuals = new List<GlossaryLine>();
        var cleanMe = new List<string>();

        foreach (var k in config.ManualTranslations)
        {
            if (cache.ContainsKey(k.Raw))
            {
                if (k.Result == cache[k.Raw])
                    cleanMe.Add(k.Raw);
                else
                    badDupes.Add(k.Raw);
            }
            else
            {
                cache.Add(k.Raw, k.Result);
                cleanedManuals.Add(k);
            }
        }

        if (badDupes.Count > 0)
        {
            File.WriteAllLines($"{workingDirectory}/TestResults/DupeBadGlossary.yaml", badDupes);
            throw new Exception($"Duplicate manual translations with different results found");
        }

        File.WriteAllLines($"{workingDirectory}/TestResults/DupeGlossary.yaml", cleanMe);

        //var serializer = Yaml.CreateSerializer();
        //var clean = serializer.Serialize(cleanedManuals);
        //File.WriteAllText($"{workingDirectory}/TestResults/CleanManualTranslations.yaml", clean);
    }
}
