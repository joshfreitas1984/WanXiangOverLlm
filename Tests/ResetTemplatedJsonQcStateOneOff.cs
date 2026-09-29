using FanslationStudio.LlmKit.Support;
using FanslationStudio.LlmKit.Utility;

namespace Translate.Tests;

/// <summary>
/// One-off remediation for the QC column-grouping bug fixed in FanslationStudio.LlmKit's
/// QualityReviewWorkflow. Unlike the first pass, this unconditionally resets every Qc* field
/// (including QcRuleCheckFailureCount/Baseline, which ResetQcState() deliberately leaves alone in
/// normal operation but which can still hold corrupted values - e.g. a baseline string built from
/// the wrong field's reconstructed template - left over from the bug) on every split in any line
/// with a templates: block. Delete this file after running it once.
/// </summary>
public class ResetTemplatedJsonQcStateOneOff
{
    [Fact]
    public void ResetQcStateForLinesWithTemplates()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Files", "Converted");
        var deserializer = YamlHelper.CreateDeserializer();
        var serializer = YamlHelper.CreateSerializer();

        int filesChanged = 0, linesTouched = 0, splitsReset = 0;

        foreach (var path in Directory.GetFiles(dir, "*.json.yaml"))
        {
            var lines = deserializer.Deserialize<List<TranslationLine>>(File.ReadAllText(path));
            bool fileChanged = false;

            foreach (var line in lines)
            {
                if (line.Templates == null || line.Templates.Count == 0)
                    continue;

                linesTouched++;
                foreach (var split in line.Splits)
                {
                    bool hadState = split.QcStatus != QcStatus.NotReviewed
                        || split.QcQualityScore != null
                        || split.QcRuleCheckFailureCount != 0
                        || !string.IsNullOrEmpty(split.QcRuleCheckFailureBaseline);

                    if (!hadState)
                        continue;

                    split.ResetQcState();
                    split.QcRuleCheckFailureCount = 0;
                    split.QcRuleCheckFailureBaseline = string.Empty;
                    splitsReset++;
                    fileChanged = true;
                }
            }

            if (fileChanged)
            {
                File.WriteAllText(path, serializer.Serialize(lines));
                filesChanged++;
            }
        }

        Console.WriteLine($"Files changed: {filesChanged}, lines with templates: {linesTouched}, splits reset: {splitsReset}");
    }
}
