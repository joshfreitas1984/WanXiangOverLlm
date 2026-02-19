using System.IO.Compression;
using Translate.Utility;
using static SweetPotato.FileView;

namespace Translate.Tests;

public class FileOutputWorkflowTests
{
    public const string WorkingDirectory = TranslationWorkflowTests.WorkingDirectory;
    public const string GameFolder = TranslationWorkflowTests.GameFolder;


    [Fact(DisplayName = "6. Package to Game Files")]
    public static async Task PackageFinalTranslation()
    {
        await FileOutputHandling.PackageFinalTranslationAsync(WorkingDirectory);

        var sourceDirectory = $"{WorkingDirectory}/Mod/English";
        var modDirectory = $"{GameFolder}/BepinEx/english";

        if (Directory.Exists(modDirectory))
            Directory.Delete(modDirectory, true);

        FileOutputHandling.CopyDirectory(sourceDirectory, modDirectory);
    }

    [Fact(DisplayName = "7. Zip Release")]
    public static async Task ZipRelease()
    {
        var version = ModHelper.CalculateVersionNumber();

        string releaseFolder = $"{GameFolder}/ReleaseFolder/Files";

        FileOutputHandling.CopyDirectory($"{WorkingDirectory}/Mod/English", $"{releaseFolder}/BepInEx/english");
        FileOutputHandling.CopyDirectory($"{WorkingDirectory}/Resizers", $"{releaseFolder}/BepInEx/resizers");

        FileOutputHandling.CopyDirectory($"{GameFolder}/BepInEx/config", $"{releaseFolder}/BepInEx/config");
        FileOutputHandling.CopyDirectory($"{GameFolder}/BepInEx/plugins", $"{releaseFolder}/BepInEx/plugins");

        ZipFile.CreateFromDirectory($"{releaseFolder}", $"{releaseFolder}/../EnglishPatch-{version}.zip");
        await Task.CompletedTask;
    }
}
