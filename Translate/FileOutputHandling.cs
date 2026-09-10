using FanslationStudio.LlmKit.Support;
using FanslationStudio.LlmKit.Workflow;
using Translate;

public class FileOutputHandling
{
    public static async Task PackageFinalTranslationAsync(string workingDirectory)
    {
        string inputPath = $"{workingDirectory}/Converted";
        string outputPath = $"{workingDirectory}/Mod";

        if (Directory.Exists(outputPath))
            Directory.Delete(outputPath, true);

        Directory.CreateDirectory(outputPath);
        string fileOutputPath = $"{outputPath}/English";
        Directory.CreateDirectory(fileOutputPath);

        var finalDb = new List<string>();
        var passedCount = 0;
        var failedCount = 0;

        foreach (var textFile in GameTextFiles.TextFilesToSplit.Where(f => f.TextFileType == TextFileType.PrefabText))
        {
            var (passed, failed) = await PrefabTextWorkflow.PackagePrefabTextAsync(workingDirectory, textFile);
            passedCount += passed;
            failedCount += failed;
            MoveLlmKitPackagedFileIntoEnglishFolder(workingDirectory, fileOutputPath, textFile.Path);
        }

        foreach (var textFile in GameTextFiles.TextFilesToSplit.Where(f => f.TextFileType == TextFileType.DynamicStrings))
        {
            var (passed, failed) = await DynamicStringsCecilWorkflow.PackageDynamicStringsCecilAsync(workingDirectory, textFile);
            passedCount += passed;
            failedCount += failed;
            MoveLlmKitPackagedFileIntoEnglishFolder(workingDirectory, fileOutputPath, textFile.Path);
        }

        foreach (var textFile in GameTextFiles.TextFilesToSplit.Where(f => f.TextFileType == TextFileType.RawJson))
        {
            var (passed, failed) = await JsonGameDataWorkflow.PackageAsync(workingDirectory, textFile);
            passedCount += passed;
            failedCount += failed;
            MoveLlmKitPackagedJsonFileIntoEnglishFolder(workingDirectory, fileOutputPath, textFile.Path);
        }


        Console.WriteLine($"Passed: {passedCount}");
        Console.WriteLine($"Failed: {failedCount}");
    }

    /// <summary>
    /// LlmKit's Prefab/DynamicStrings workflows always write their packaged output to
    /// Mod/{path}.yaml. This repo's own convention (see FileOutputWorkflowTests/ZipRelease) copies
    /// the whole Mod/English folder into the game's BepInEx/english folder, so the packaged file is
    /// moved there under its original name (no ".yaml" suffix) to preserve that existing contract.
    /// </summary>
    private static void MoveLlmKitPackagedFileIntoEnglishFolder(string workingDirectory, string fileOutputPath, string path)
    {
        var source = $"{workingDirectory}/Mod/{path}.yaml";
        if (File.Exists(source))
            File.Move(source, $"{fileOutputPath}/{path}", true);
    }

    /// <summary>
    /// <see cref="JsonGameDataWorkflow.PackageAsync"/> writes its packaged output straight to
    /// Mod/{path} with no ".yaml" suffix - unlike Prefab/DynamicStrings, its Mod-directory output is
    /// already the final game-consumable JSON, not an intermediate YAML shape - so no suffix needs
    /// stripping on the way into the English folder.
    /// </summary>
    private static void MoveLlmKitPackagedJsonFileIntoEnglishFolder(string workingDirectory, string fileOutputPath, string path)
    {
        var source = $"{workingDirectory}/Mod/{path}";
        if (File.Exists(source))
            File.Move(source, $"{fileOutputPath}/{path}", true);
    }

    public static void CopyDirectory(string sourceDir, string destDir)
    {
        // Get the subdirectories for the specified directory.
        var dir = new DirectoryInfo(sourceDir);

        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory does not exist or could not be found: {sourceDir}");

        // If the destination directory doesn't exist, create it.
        if (!Directory.Exists(destDir))
            Directory.CreateDirectory(destDir);

        // Get the files in the directory and copy them to the new location.
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            var tempPath = Path.Combine(destDir, file.Name);
            file.CopyTo(tempPath, true);
        }

        // Copy each subdirectory using recursion
        DirectoryInfo[] dirs = dir.GetDirectories();
        foreach (DirectoryInfo subdir in dirs)
        {
            if (subdir.Name == ".git" || subdir.Name == ".vs")
                continue;

            var tempPath = Path.Combine(destDir, subdir.Name);
            CopyDirectory(subdir.FullName, tempPath);
        }
    }
}