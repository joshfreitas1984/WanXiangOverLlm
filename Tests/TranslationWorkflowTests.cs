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
        await TranslationWorkflow.ApplyAllRulesToCurrentTranslation(WorkingDirectory, GameTextFiles.TextFilesToSplit, GameFileHandling.Hooks);
    }

    // QC-pipeline facts (RunQualityControlPassSample "3a" through the full-reset fact) now live in
    // QualityControlWorkflowTests.cs, matching DragonHierOverLlm's structure.

    [Fact(DisplayName = "4. TranslateLines")]
    public async Task TranslateLines()
    {
        await TranslationWorkflow.TranslateLines(WorkingDirectory, GameTextFiles.TextFilesToSplit, GameFileHandling.Hooks);
        await FileOutputWorkflowTests.PackageFinalTranslation();
    }

    [Fact(DisplayName = "0. TranslateLinesBruteForce")]
    public async Task TranslateLinesBruteForce()
    {
        await TranslationWorkflow.TranslateLinesBruteForce(WorkingDirectory, GameTextFiles.TextFilesToSplit, GameFileHandling.Hooks);
        await FileOutputWorkflowTests.PackageFinalTranslation();
    }

    [Fact(DisplayName = "0. Reset All Flags")]
    public async Task ResetAllFlags()
    {
        await TranslationWorkflow.ResetAllFlags(WorkingDirectory, GameTextFiles.TextFilesToSplit);
    }

    // Finds translations with an invented gender (he/she where the source states none) or subject-less narration
    // written as "I". The dry run writes only TestResults/PronounRetranslation.yaml; run it first to read the count.
    // This game has no LineContextProvider, so speaker-gender checks are not available, and its prose setting
    // (lines whose translation names a character are skipped) comes from Config.yaml pronounCheck.
    [Fact(DisplayName = "5. Count lines needing pronoun retranslation (dry run)")]
    public async Task CountPronounRetranslation() =>
        await PronounDefectWorkflow.RunAsync(WorkingDirectory, GameTextFiles.TextFilesToSplit,
            flagForRetranslation: false, GameFileHandling.Hooks);

    // Also sets FlaggedForRetranslation on each hit. Flagged lines are not packaged until retranslated, so follow it
    // straight away with a translate-flagged run.
    [Fact(DisplayName = "5. Flag lines needing pronoun retranslation")]
    public async Task FlagPronounRetranslation() =>
        await PronounDefectWorkflow.RunAsync(WorkingDirectory, GameTextFiles.TextFilesToSplit,
            flagForRetranslation: true, GameFileHandling.Hooks);

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
