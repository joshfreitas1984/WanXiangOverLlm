using FanslationStudio.LlmKit.Workflow;

namespace Translate.Tests;

public class QualityControlWorkflowTests
{
    public const string WorkingDirectory = TranslationWorkflowTests.WorkingDirectory;

    // The full "I changed the glossary / got file updates / exported more dynamic strings / added a
    // bad word / needed a new game repair" workflow in one call: brute-forces Translated back to
    // clean (TranslationWorkflow.TranslateLinesBruteForce), then does the same for QcTranslated
    // (QualityReviewWorkflow.RunBruteForce - a no-op if qualityReview.enabled is false), then
    // packages. Use this instead of running "0. TranslateLinesBruteForce" (TranslationWorkflowTests)
    // and "2" separately when you want QC kept in sync too.
    [Fact(DisplayName = "0. TranslateAndQualityReviewBruteForce")]
    public async Task TranslateAndQualityReviewBruteForce()
    {
        await TranslationWorkflow.TranslateLinesBruteForce(WorkingDirectory, GameTextFiles.TextFilesToSplit, GameFileHandling.Hooks);
        await QualityReviewWorkflow.RunBruteForce(WorkingDirectory, GameTextFiles.TextFilesToSplit, hooks: GameFileHandling.Hooks);
        await FileOutputWorkflowTests.PackageFinalTranslation();
    }

    // Run this BEFORE "2" the first time you try a candidate qualityReview model - reviews only a
    // small random sample (see QualityReviewWorkflow.RunAsync's sampleSize) instead of every
    // eligible column, so you can judge a model's real speed/score-distribution/correction-quality
    // on your hardware before committing an entire run to it. A no-op if Config.yaml's
    // qualityReview.enabled is false (currently the case for this repo).
    [Fact(DisplayName = "1. RunQualityReviewPassSample")]
    public async Task RunQualityReviewPassSample()
    {
        await QualityReviewWorkflow.RunAsync(WorkingDirectory, GameTextFiles.TextFilesToSplit, sampleSize: 300, hooks: GameFileHandling.Hooks);
    }

    // Independent of the main translate/apply-rules/translate-lines steps in TranslationWorkflowTests
    // - reviews already-translated text against a separately configured model (Config.yaml's
    // qualityReview: section), proposes corrections, and validates them before writing anything. A
    // no-op (logs and returns) if qualityReview.enabled is false, so it's safe to run even before the
    // feature is configured for a real run.
    [Fact(DisplayName = "2. RunQualityReviewPass")]
    public async Task RunQualityReviewPass()
    {
        await QualityReviewWorkflow.RunAsync(WorkingDirectory, GameTextFiles.TextFilesToSplit, hooks: GameFileHandling.Hooks);
    }

    // Run this after a glossary/config change so already-QC'd QcTranslated text picks up the same
    // rule changes TranslationWorkflowTests' "3. ApplyRulesToCurrentTranslation" applies to
    // Translated - otherwise QC's output would silently drift out of sync with the current rules.
    [Fact(DisplayName = "3. ApplyRulesToQCReview")]
    public async Task ApplyRulesToQCReview()
    {
        await QualityReviewWorkflow.ApplyRulesToCurrentQcTranslated(WorkingDirectory, GameTextFiles.TextFilesToSplit, GameFileHandling.Hooks);
    }

    // Reporting-only, mirrors TranslationWorkflowTests' failure-finding facts but scoped to
    // quality-review flags (a rejected correction, or a low QcQualityScore) instead of translation
    // failures - see QualityReviewWorkflow.GetFlaggedQcReviews.
    [Fact(DisplayName = "4. Find Flagged Quality Review Items")]
    public async Task FindFlaggedQcReviews()
    {
        var flagged = await QualityReviewWorkflow.GetFlaggedQcReviews(WorkingDirectory, GameTextFiles.TextFilesToSplit);

        var serializer = FanslationStudio.LlmKit.Utility.YamlHelper.CreateSerializer();
        var yaml = serializer.Serialize(flagged);
        FanslationStudio.LlmKit.Utility.FileHelper.WriteAllTextWithRetry($"{WorkingDirectory}/TestResults/FlaggedQcReviews.yaml", yaml);
    }

    // Turns "4"'s flat dump into something a human can actually work down over time, instead of
    // eyeballing every row or hand-marking individual lines as "ok" (the data model has no such
    // field, and QualityReviewWorkflow deliberately has no manual-approval gate). Writes
    // QcTriageSummary.yaml/QcTriageByReason.yaml/QcTriageLowScoreSample.yaml under TestResults. Safe
    // and cheap to re-run any time (no LLM calls) - re-run after any prompt/glossary/config fix to
    // see the reason clusters shrink and the low-score sample shift as real progress is made.
    [Fact(DisplayName = "5. Triage Flagged Quality Review Items")]
    public async Task TriageFlaggedQcReviews()
    {
        await QualityReviewWorkflow.WriteTriageReportAsync(WorkingDirectory, GameTextFiles.TextFilesToSplit);
    }

    // Turns "5"'s clusters into ready-to-paste prompts for a Claude chat (QcTriagePrompts.md),
    // rather than a fully automated fix pipeline - the actual edits (BaseQualityReviewPrompt.txt
    // wording, a glossary rule, qualityReview.minAcceptableScore) are small and judgment-heavy
    // enough that a human should read the examples and apply the change themselves, not have an LLM
    // edit prompt files unsupervised. Paste a section at a time into a chat; apply whatever fix
    // comes back by hand, then use "7. Reset Qc Retry Limits"/"Reset Leaked Quality Review
    // Corrections" + a re-run to see the cluster shrink next time "4"/"5" run.
    [Fact(DisplayName = "6. Generate Quality Review Fix Prompts")]
    public async Task GenerateQcFixPrompts()
    {
        await QualityReviewWorkflow.WriteFixPromptsAsync(WorkingDirectory, GameTextFiles.TextFilesToSplit);
    }

    // Run this after fixing whatever was causing a persistent QC rule violation (e.g. removed a
    // false-positive bad word, loosened a glossary rule) so columns QualityReviewWorkflow.RunBruteForce
    // already gave up on (see TranslationSplit.QcRuleCheckFailureCount) get retried instead of
    // staying parked forever. A no-op for everything else.
    [Fact(DisplayName = "7. Reset Qc Retry Limits")]
    public async Task ResetQcRetryLimits()
    {
        await QualityReviewWorkflow.ResetQcRetryLimits(WorkingDirectory, GameTextFiles.TextFilesToSplit);
    }

    // Sweeps every already-QC'd column for a stored QcTranslated/QcRejectedCorrection that leaked QC
    // protocol text (a stray "NONE", "SCORE:", "CORRECTED:", etc. - see
    // QualityReviewWorkflow.ContainsLeakedProtocolText) rather than a clean correction. Any match is
    // reset back to QcStatus.NotReviewed (full TranslationSplit.ResetQcState) so the next "1"/"2"
    // run gives it a genuinely fresh review. Safe to run any time - a no-op once the corpus is clean.
    [Fact(DisplayName = "Reset Leaked Quality Review Corrections")]
    public async Task ResetLeakedQcCorrections()
    {
        await QualityReviewWorkflow.ResetLeakedQcCorrections(WorkingDirectory, GameTextFiles.TextFilesToSplit);
    }

    // Run this after changing how QC's score is judged (BaseQualityReviewPrompt.txt's scoring
    // rubric, or switching qualityReview.modelName to a model that scores on a different scale) so
    // every column currently sitting below minAcceptableScore under the OLD calculation gets a
    // genuinely fresh score under the new one. Leaves every already-accepted column with an
    // acceptable score untouched (unlike "Reset ALL Quality Review State", which re-reviews
    // everything) - only the columns actually worth another look get re-sent to the LLM. A
    // rejected-correction column (Reason set, QcQualityScore already cleared to null) is never
    // touched here - use "7. Reset Qc Retry Limits" for those.
    [Fact(DisplayName = "7. Reset Low-Score Quality Review State")]
    public async Task ResetLowScoreQcState()
    {
        await QualityReviewWorkflow.ResetLowScoreQcState(WorkingDirectory, GameTextFiles.TextFilesToSplit);
    }

    // Run this once to pick up the 2026-09-16 CONSISTENCY/SCORING ANCHORS prompt fix
    // (../FanslationStudio.LlmKit/docs/features/translation-pipeline/quality-review-pass.md postmortem #4) - every
    // Corrected column reviewed under either the old always-<40 rubric or the overcorrected
    // always-85+ intermediate rubric needs a fresh review under the final graduated-scale prompt.
    // "7. Reset Low-Score Quality Review State" can't reach these anymore once they were
    // overcorrected upward past minAcceptableScore, so this targets QcStatus.Corrected directly
    // instead of inferring the affected set from score.
    [Fact(DisplayName = "7. Reset Corrected Quality Review State")]
    public async Task ResetCorrectedQcState()
    {
        await QualityReviewWorkflow.ResetCorrectedQcState(WorkingDirectory, GameTextFiles.TextFilesToSplit);
    }

    // DEFECT-category counterpart to "7" (which resets by score threshold instead). Resets every
    // flagged column whose DEFECT category is NOT in Config.yaml's qualityReview.autoAcceptDefectCategories
    // back to NotReviewed for a fresh review. QcDefectCategory.Unknown always lands in this bucket (a
    // line whose response predates the DEFECT-first prompt, or otherwise failed to parse a DEFECT:
    // line). Safe to re-run any time autoAcceptDefectCategories changes (a category's hand-validated
    // precision verdict is added or revised) to pull the newly-decided set back out of "flagged" one
    // way or the other on the next "1"/"2" pass.
    [Fact(DisplayName = "8. Reset Non-Auto-Accepted Quality Review State")]
    public async Task ResetNonAutoAcceptedQcState()
    {
        await QualityReviewWorkflow.ResetNonAutoAcceptedQcState(WorkingDirectory, GameTextFiles.TextFilesToSplit);
    }

    // Run this ONCE after adding a new dropped-stutter defect category/stutter-handling rule to
    // BaseSystemPrompt.txt/BaseQualityReviewPrompt.txt: before that change, a source-language
    // stammer/stutter had no named defect to score against, so a dropped stutter almost always
    // passed QC silently at a high score instead of getting flagged. This resets every column whose
    // SOURCE actually contains the pattern back to NotReviewed regardless of its old score/status,
    // so the next "1"/"2" pass gives it a genuinely fresh review under the new prompt - far cheaper
    // than "Reset ALL Quality Review State" since it targets only the columns a plain regex scan
    // finds, with no LLM call of its own.
    [Fact(DisplayName = "9. Reset Stutter-Affected Quality Review State")]
    public async Task ResetStutterAffectedQcState()
    {
        await QualityReviewWorkflow.ResetStutterAffectedQcState(WorkingDirectory, GameTextFiles.TextFilesToSplit);
    }

    // Full do-over, NOT a routine step - unlike "7. Reset Qc Retry Limits"/"Reset Leaked Quality
    // Review Corrections" (which only un-stick/repair specific stuck-or-corrupted columns), this
    // wipes every column's Qc* state back to NotReviewed regardless of its current status, so the
    // next "1"/"2" pass reviews the ENTIRE corpus again from scratch. IsQcReviewFresh only tracks
    // whether Translated changed, never whether the QC model/prompt that produced an existing
    // verdict did - so swapping the QC model, or a prompt change significant enough that already-
    // recorded Passed/Corrected verdicts can no longer be trusted, is when to run this - never as a
    // matter of routine, since a full re-review is the same many-hours job a first full run already
    // was.
    [Fact(DisplayName = "Reset ALL Quality Review State (full re-review)")]
    public async Task ResetAllQcState()
    {
        await QualityReviewWorkflow.ResetAllQcState(WorkingDirectory, GameTextFiles.TextFilesToSplit);
    }
}
