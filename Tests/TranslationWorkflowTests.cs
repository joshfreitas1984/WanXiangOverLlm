using FanslationStudio.LlmKit.Support;
using FanslationStudio.LlmKit.Workflow;

namespace Translate.Tests;

public class TranslationWorkflowTests
{
    public const string WorkingDirectory = "../../../../Files";
    public const string GameFolder = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\wanxiang\\wanxiang\\";

    [Fact(DisplayName = "3. ApplyRulesToCurrentTranslation")]
    public async Task ApplyRulesToCurrentTranslation()
    {
        await TranslationWorkflow.ApplyAllRulesToCurrentTranslation(WorkingDirectory, GameTextFiles.TextFilesToSplit);
    }

    // Run this BEFORE "3b" the first time you try a candidate qualityReview model - reviews only a
    // small random sample (see QualityReviewWorkflow.RunAsync's sampleSize) instead of every
    // eligible column, so you can judge a model's real speed/score-distribution/correction-quality
    // on your hardware before committing an entire run to it. A no-op if Config.yaml's
    // qualityReview.enabled is false (currently the case for this repo).
    [Fact(DisplayName = "3a. RunQualityReviewPassSample")]
    public async Task RunQualityReviewPassSample()
    {
        await QualityReviewWorkflow.RunAsync(WorkingDirectory, GameTextFiles.TextFilesToSplit, sampleSize: 300);
    }

    // Independent of the main translate/apply-rules/translate-lines steps above - reviews
    // already-translated text against a separately configured model (Config.yaml's qualityReview:
    // section), proposes corrections, and validates them before writing anything. A no-op (logs and
    // returns) if qualityReview.enabled is false, so it's safe to run even before the feature is
    // configured for a real run.
    [Fact(DisplayName = "3b. RunQualityReviewPass")]
    public async Task RunQualityReviewPass()
    {
        await QualityReviewWorkflow.RunAsync(WorkingDirectory, GameTextFiles.TextFilesToSplit);
    }

    // Reporting-only, mirrors the other failure-finding facts but scoped to quality-review flags (a
    // rejected correction, or a low QcQualityScore) instead of translation failures - see
    // QualityReviewWorkflow.GetFlaggedQcReviews.
    [Fact(DisplayName = "3c. Find Flagged Quality Review Items")]
    public async Task FindFlaggedQcReviews()
    {
        var flagged = await QualityReviewWorkflow.GetFlaggedQcReviews(WorkingDirectory, GameTextFiles.TextFilesToSplit);

        var serializer = FanslationStudio.LlmKit.Utility.YamlHelper.CreateSerializer();
        var yaml = serializer.Serialize(flagged);
        FanslationStudio.LlmKit.Utility.FileHelper.WriteAllTextWithRetry($"{WorkingDirectory}/TestResults/FlaggedQcReviews.yaml", yaml);
    }

    // Run this after fixing whatever was causing a persistent QC rule violation (e.g. removed a
    // false-positive bad word, loosened a glossary rule) so columns QualityReviewWorkflow.RunBruteForce
    // already gave up on (see TranslationSplit.QcRuleCheckFailureCount) get retried instead of
    // staying parked forever. A no-op for everything else.
    [Fact(DisplayName = "3d. Reset Qc Retry Limits")]
    public async Task ResetQcRetryLimits()
    {
        await QualityReviewWorkflow.ResetQcRetryLimits(WorkingDirectory, GameTextFiles.TextFilesToSplit);
    }

    [Fact(DisplayName = "4. TranslateLines")]
    public async Task TranslateLines()
    {
        await TranslationWorkflow.TranslateLines(WorkingDirectory, GameTextFiles.TextFilesToSplit);
        await FileOutputWorkflowTests.PackageFinalTranslation();
    }

    [Fact(DisplayName = "0. TranslateLinesBruteForce")]
    public async Task TranslateLinesBruteForce()
    {
        await TranslationWorkflow.TranslateLinesBruteForce(WorkingDirectory, GameTextFiles.TextFilesToSplit);
        await FileOutputWorkflowTests.PackageFinalTranslation();
    }

    [Fact(DisplayName = "0. Reset All Flags")]
    public async Task ResetAllFlags()
    {
        await TranslationWorkflow.ResetAllFlags(WorkingDirectory, GameTextFiles.TextFilesToSplit);
    }

    [Fact(DisplayName = "5. Flag some regexes")]
    public async Task SetSplitAsInvalid()
    {
        var badStrings = new List<string> { };

        await TranslationWorkflow.SetSplitAsInvalid(WorkingDirectory, GameTextFiles.TextFilesToSplit, badStrings);
    }

    [Fact(DisplayName = "6. Clean up some regexes")]
    public static async Task CleanUpSomeRegexes()
    {
        var regex = new List<(string pattern, string replacement)>
        {
            // Look for Number then "coin" or "wen" or "money" or "quan" or "liang", get the number portion
            (@"(\d+)(\s*)(coin|wen|money|quan|liang)", "$1 coin"),
        };

        await TranslationWorkflow.CleanUpSomeRegexes(WorkingDirectory, GameTextFiles.TextFilesToSplit, regex);
    }
}
