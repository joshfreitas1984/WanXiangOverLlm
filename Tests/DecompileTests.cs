using System.Diagnostics;


namespace Translate.Tests;

/// <summary>
/// "0. Decompile Game Assemblies" - decompiles the game's Mono assemblies to Files/Raw/Decompiled (gitignored) so
/// the game code can be searched with Grep instead of being re-decompiled each session. Skips an assembly whose output
/// is newer than the game DLL, so it is cheap to re-run after a game update. Needs `dotnet tool install -g ilspycmd`.
/// </summary>
public class DecompileTests
{
    static readonly string[] Assemblies = ["Assembly-CSharp", "Assembly-CSharp-firstpass"];

    [Fact(DisplayName = "0. Decompile Game Assemblies")]
    public void DecompileGameAssemblies()
    {
        var dataFolder = Directory.GetDirectories(TranslationWorkflowTests.GameFolder, "*_Data").Single();
        var outputRoot = Path.GetFullPath($"{TranslationWorkflowTests.WorkingDirectory}/Raw/Decompiled");

        foreach (var assembly in Assemblies)
        {
            var dll = Path.Combine(dataFolder, "Managed", $"{assembly}.dll");
            var output = Path.Combine(outputRoot, assembly);

            if (!File.Exists(dll))
                continue;

            if (Directory.Exists(output) && Directory.GetFiles(output, "*.cs", SearchOption.AllDirectories) is { Length: > 0 } files
                && files.Max(File.GetLastWriteTimeUtc) > File.GetLastWriteTimeUtc(dll))
                continue;

            if (Directory.Exists(output))
                Directory.Delete(output, true);

            Directory.CreateDirectory(output);

            // -p writes one .cs file per type (and a csproj), which is what makes it greppable.
            var info = new ProcessStartInfo("ilspycmd", $"\"{dll}\" -p -o \"{output}\"") { UseShellExecute = false };
            using var process = Process.Start(info)!;
            process.WaitForExit();
            Assert.Equal(0, process.ExitCode);
        }
    }
}
