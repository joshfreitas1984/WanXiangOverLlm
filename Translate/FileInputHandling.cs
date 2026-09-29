using FanslationStudio.LlmKit.Support;
using FanslationStudio.LlmKit.Workflow;

namespace Translate;

public class InputFileHandling
{
    public static void ExportTextAssetsToCustomFormat(string workingDirectory)
    {
        foreach (var textFile in GameTextFiles.TextFilesToSplit.Where(f => f.TextFileType == TextFileType.RawJson))
            JsonGameDataWorkflow.ExportToCustomFormat(workingDirectory, textFile);
    }

    public static async Task MergeFilesIntoTranslatedAsync(string workingDirectory)
    {
        var llmKitFiles = GameTextFiles.TextFilesToSplit
            .Where(f => f.TextFileType is TextFileType.PrefabText or TextFileType.DynamicStrings or TextFileType.RawJson)
            .ToArray();

        if (llmKitFiles.Length > 0)
            await FanslationStudio.LlmKit.GameFileHandlingBase.MergeFilesIntoTranslatedAsync(workingDirectory, llmKitFiles);
    }

    /// <summary>
    /// Delegates to <see cref="PrefabTextWorkflow.ExportPrefabTextToCustomFormat"/> for every
    /// registered <see cref="TextFileType.PrefabText"/> entry. Currently a no-op - dumpedPrefabText.txt
    /// is commented out in <see cref="GameTextFiles.TextFilesToSplit"/> pending confirmation that
    /// EnglishPatch is ready to consume its packaged output (see
    /// docs/features/translation-pipeline/workflow-execution-reference.md).
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
