using FanslationStudio.LlmKit.Support;

namespace Translate;

/// <summary>
/// Thin convenience wrapper over <see cref="global::FileIteration"/> (FanslationStudio.LlmKit) that
/// defaults to iterating this repo's own <see cref="GameTextFiles.TextFilesToSplit"/> registry,
/// matching this repo's existing call-site convention of not passing an explicit file list. Callers
/// that need a specific subset (e.g. only <see cref="TextFileType.RawCsv"/> entries) call
/// <see cref="global::FileIteration"/> directly instead.
/// </summary>
public class FileIteration
{
    public static Task IterateTranslatedFilesAsync(string workingDirectory,
        Func<string, TextFileToSplit, List<TranslationLine>, Task> performActionAsync) =>
        global::FileIteration.IterateTranslatedFilesAsync(workingDirectory, GameTextFiles.TextFilesToSplit, performActionAsync);

    public static Task IterateTranslatedFilesInParallelAsync(string workingDirectory,
        Func<string, TextFileToSplit, List<TranslationLine>, Task> performActionAsync) =>
        global::FileIteration.IterateTranslatedFilesInParallelAsync(workingDirectory, GameTextFiles.TextFilesToSplit, performActionAsync);
}
