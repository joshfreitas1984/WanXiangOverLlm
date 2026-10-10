using FanslationStudio.LlmKit;
using FanslationStudio.LlmKit.Configuration;
using FanslationStudio.LlmKit.Utility;
using Translate;

namespace Tests;

/// <summary>
/// Manual, live-LLM diagnostic (skips unless DIAG_TEXTS is set) - see the diagnose-translation-failure skill.
/// DIAG_TEXTS = path to a UTF-8 file with one exact split Text per line; DIAG_FILE = TextFilesToSplit path
/// (default EventDialog.json); DIAG_ATTEMPTS (default 3); report is written to DIAG_OUT (default TestResults/DiagnoseTranslation.txt).
/// </summary>
public class DiagnoseTranslationTests
{
    [Fact]
    public async Task DiagnoseTranslation()
    {
        var textsPath = Environment.GetEnvironmentVariable("DIAG_TEXTS");
        if (string.IsNullOrEmpty(textsPath))
            return;

        var workingDirectory = GameFileHandling.WorkingDirectory;
        var config = ConfigurationExtensions.GetConfiguration(workingDirectory, GameFileHandling.Hooks);
        var path = Environment.GetEnvironmentVariable("DIAG_FILE") ?? "EventDialog.json";
        var textFile = GameTextFiles.TextFilesToSplit.First(x => string.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase));
        var attempts = int.TryParse(Environment.GetEnvironmentVariable("DIAG_ATTEMPTS"), out var n) ? n : 3;
        var output = Environment.GetEnvironmentVariable("DIAG_OUT") ?? Path.Combine(workingDirectory, "TestResults", "DiagnoseTranslation.txt");

        var texts = (await File.ReadAllLinesAsync(textsPath)).Where(l => l.Length > 0);
        await TranslationDiagnostics.DiagnoseAsync(config, textFile, workingDirectory, texts, attempts, output);
        Console.WriteLine($"Report: {Path.GetFullPath(output)}");
    }
}
