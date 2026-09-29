using FanslationStudio.Installer.Core;
using FanslationStudio.LlmKit.Release;
using Translate.Utility;

namespace Translate.Tests;

public class FileOutputWorkflowTests
{
    public const string WorkingDirectory = TranslationWorkflowTests.WorkingDirectory;
    public const string GameFolder = TranslationWorkflowTests.GameFolder;

    const string PackagingInputsFolder = "../../../../Files/Packaging";

    // Repo, gh account and zip name come from Installer/installer.json. A null ghAccount opens the
    // prefilled releases/new page instead; ambient gh/git auth is never used.
    static InstallerConfig LoadInstallerConfig() => InstallerConfig.Load("../../../../Installer/installer.json");

    [Fact(DisplayName = "6. Package to Game Files")]
    public static async Task PackageFinalTranslation()
    {
        await FileOutputHandling.PackageFinalTranslationAsync(WorkingDirectory);

        var sourceDirectory = $"{WorkingDirectory}/Mod/English";
        var modDirectory = $"{GameFolder}/BepinEx/english";

        if (Directory.Exists(modDirectory))
            Directory.Delete(modDirectory, true);

        FileOutputHandling.CopyDirectory(sourceDirectory, modDirectory);

        TextResizerTests.MoveResizersIntoPathBasedFiles();
        TextResizerTests.MoveSpritesIntoPathBasedFiles();
        TextResizerTests.MoveLayoutsIntoPathBasedFiles();
    }

    // The patch zip holds only this project's files. BepInEx itself (winhttp.dll, doorstop, BepInEx/core) is
    // installed by the installer from the pin in Installer/installer.json, so it must never be in the zip.
    [Fact(DisplayName = "7. Package Release")]
    public static async Task PackageRelease()
    {
        var outputFolder = $"{GameFolder}/ReleaseFolder";
        var stagingFolder = $"{outputFolder}/Files";

        var installer = LoadInstallerConfig();
        var notes = BuildReleaseNotes(installer);

        PrepareStagingFolder(stagingFolder);

        var result = ReleasePackager.Package(new ReleaseOptions
        {
            ReleaseNotes = notes,
            Version = ModHelper.CalculateVersionNumber(),
            // The plugin PostBuild steps write the DLLs straight into the staging folder.
            StagingFolder = stagingFolder,
            OutputFolder = outputFolder,
            ZipPrefix = installer.PatchZipPrefix,
            // Lets the in-game updater find its repo and relaunch the game without any per-game config.
            GitHubRepo = installer.GitHubRepo,
            SteamAppId = installer.SteamAppId,
            // Explicit files only: copying the game's whole config and plugins folders would ship stray dev plugins
            // and stale configs.
            Mappings =
            [
                new($"{WorkingDirectory}/Mod/English", "BepInEx/english"),
                new($"{WorkingDirectory}/Resizers", "BepInEx/resizers"),
                // Tailored per game, so it comes from the release files: seed-only, so players' edits survive updates.
                new($"{PackagingInputsFolder}/BepInEx.cfg", "BepInEx/config/BepInEx.cfg"),
            ],
            OwnedFolders = ["BepInEx/english", "BepInEx/resizers", "BepInEx/config"],
            // zzAddedResizers.yaml is the editor's auto-created file: step 6 moves its entries into path-based files.
            RemoveAfterStaging = ["BepInEx/resizers/zzAddedResizers.yaml"],
            SeedOnly = ["BepInEx/config/**"],
        });

        var assets = new[] { result.ZipPath, result.ManifestPath };
        var url = ReleasePublisher.BuildReleaseUrl(installer.GitHubRepo, result.Version, notes);

        var ghFailure = installer.GhAccount == null
            ? "no ghAccount configured"
            : ReleasePublisher.TryPublishWithGh(installer.GitHubRepo, installer.GhAccount, result.Version, assets, result.NotesPath);

        if (ghFailure != null)
        {
            Console.WriteLine($"Not published automatically ({ghFailure}). Attach {Path.GetFileName(result.ZipPath)} at {url}");
            ReleasePublisher.TryOpen(Path.GetFullPath(outputFolder));
            ReleasePublisher.TryOpen(url);
        }

        await Task.CompletedTask;
    }

    // Only needed when the installer code or Installer/installer.json changes (e.g. a new BepInEx pin). The
    // binaries go on one rolling "installer" pre-release with a fixed download URL, so the in-game updater and
    // new players always fetch the latest, and patch releases never rebuild or re-upload them.
    [Fact(DisplayName = "7b. Package Installer")]
    public static void PackageInstaller()
    {
        var installer = LoadInstallerConfig();
        var outputFolder = $"{GameFolder}/ReleaseFolder";
        const string project = "../../../../Installer/Installer.csproj";

        var assets = new[]
        {
            DotnetPublisher.PublishSingleFile(project, "win-x64", outputFolder, "Installer", InstallerAssets.WindowsFileName),
            DotnetPublisher.PublishSingleFile(project, "linux-x64", outputFolder, "Installer", InstallerAssets.LinuxFileName),
        };

        var ghFailure = installer.GhAccount == null
            ? "no ghAccount configured"
            : ReleasePublisher.TryPublishRollingWithGh(installer.GitHubRepo, installer.GhAccount, InstallerAssets.ReleaseTag, "Installer", assets);

        if (ghFailure != null)
        {
            // The rolling release normally exists already: then the two files are replaced on its edit page
            // (delete the old ones, upload the new ones). Only the first ever run needs the new-release form.
            var exists = ReleasePublisher.TryReleaseExists(installer.GitHubRepo, InstallerAssets.ReleaseTag);
            var url = exists == true
                ? InstallerAssets.EditReleaseUrl(installer.GitHubRepo)
                : InstallerAssets.NewReleaseUrl(installer.GitHubRepo);

            Console.WriteLine($"Not published automatically ({ghFailure}).");
            Console.WriteLine(exists == true
                ? $"The '{InstallerAssets.ReleaseTag}' release exists: on the edit page, delete the old files and upload {string.Join(" and ", assets.Select(Path.GetFileName))}. Then Update release."
                : $"Attach {string.Join(" and ", assets.Select(Path.GetFileName))} at {url} (tick 'pre-release' and keep the tag 'installer').");
            ReleasePublisher.TryOpen(Path.GetFullPath(outputFolder));
            ReleasePublisher.TryOpen(url);
        }
    }

    // Older releases shipped BepInEx inside the zip, and its files (doorstop, winhttp.dll, changelog.txt, the
    // BepInEx core/cache/patchers folders) are still sitting in the staging folder. Remove them so the patch never
    // overwrites the installer's pinned BepInEx, then check the plugin DLLs arrived.
    static void PrepareStagingFolder(string stagingFolder)
    {
        if (!Directory.Exists(stagingFolder))
            throw new DirectoryNotFoundException($"Build the plugin projects first so their PostBuild copies the DLLs into {Path.GetFullPath(stagingFolder)}");

        foreach (var file in new[] { ".doorstop_version", "doorstop_config.ini", "winhttp.dll", "changelog.txt" })
            File.Delete(Path.Combine(stagingFolder, file));

        var bepInEx = Path.Combine(stagingFolder, "BepInEx");
        var existing = Directory.GetDirectories(stagingFolder, "BepInEx").SingleOrDefault();
        if (existing != null && Path.GetFileName(existing) != "BepInEx")
        {
            // Two-step move: a case-only rename is a no-op on a case-insensitive file system.
            var temp = Path.Combine(stagingFolder, $"BepInEx-rename-{Guid.NewGuid():N}");
            Directory.Move(existing, temp);
            Directory.Move(temp, bepInEx);
        }

        foreach (var folder in new[] { "core", "cache", "patchers" })
        {
            var path = Path.Combine(bepInEx, folder);
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }

        // The packager ships everything in staging, so keep plugins to exactly the two DLLs the patch needs. Older
        // releases also carried a stale FanslationStudio.SharedAssembly.dll that nothing loads any more.
        string[] pluginDlls = ["FanslationStudio.EnglishPatch.dll", "FanslationStudio.Plugins.dll"];
        var pluginsFolder = Path.Combine(bepInEx, "plugins");

        foreach (var dll in pluginDlls)
        {
            if (!File.Exists(Path.Combine(pluginsFolder, dll)))
                throw new FileNotFoundException($"{dll} is missing from {Path.GetFullPath(pluginsFolder)}: build the plugin projects first.");
        }

        foreach (var file in Directory.GetFiles(pluginsFolder).Where(f => !pluginDlls.Contains(Path.GetFileName(f))))
            File.Delete(file);
    }

    // Game version (Files/Packaging/GameVersion.txt, required, one line) first, then the installer links, then the git commits
    // since the newest tag. Tags are fetched from origin first (read-only). The notes prefill the
    // GitHub release page, where they can be edited before publishing.
    static string BuildReleaseNotes(InstallerConfig installer)
    {
        var versionFile = $"{PackagingInputsFolder}/GameVersion.txt";

        if (!File.Exists(versionFile) || string.IsNullOrWhiteSpace(File.ReadAllText(versionFile)))
            throw new FileNotFoundException($"Put the current game version on one line in {Path.GetFullPath(versionFile)}");

        var notes = $"**Game version: {File.ReadAllText(versionFile).Trim()}**";

        // Always present: new players land on a release page without having the installer yet.
        notes += $"{Environment.NewLine}{Environment.NewLine}New install? Download the installer: " +
                 $"[Windows]({InstallerAssets.DownloadUrl(installer.GitHubRepo, true)}) | " +
                 $"[Linux]({InstallerAssets.DownloadUrl(installer.GitHubRepo, false)}). " +
                 "Already installed? The game offers the update when it starts.";

        // Git problems must not block a release, but must be visible rather than silently dropping the changes.
        string commits;
        try
        {
            commits = FanslationStudio.LlmKit.Release.GitReleaseNotes.Generate(Path.GetFullPath($"{PackagingInputsFolder}/../.."), fetchTags: true);
        }
        catch (Exception ex)
        {
            commits = $"_Could not generate the change list: {ex.Message}_";
            Console.WriteLine(commits);
        }

        notes += $"{Environment.NewLine}{Environment.NewLine}{commits}";

        return notes + Environment.NewLine;
    }
}
