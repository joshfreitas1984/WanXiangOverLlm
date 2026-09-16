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

    // QC-pipeline facts (RunQualityReviewPassSample "3a" through the full-reset fact) now live in
    // QualityControlWorkflowTests.cs, matching DragonHierOverLlm's structure.

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
