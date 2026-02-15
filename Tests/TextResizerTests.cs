using SharedAssembly.TextResizer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Translate.Utility;

namespace Translate.Tests;

public class TextResizerTests
{
    const string workingDirectory = "../../../../Files";

    [Fact] // Can only be run when VS is running in admin
    public void CreateSymlinks()
    {
        // DumpedData
        var inputFolder = $"{workingDirectory}/Raw/Dumped";
        inputFolder = Path.GetFullPath(inputFolder);
        var outputFolder = $"{TranslationWorkflowTests.GameFolder}/BepInEx/dumpeddata";
        SymLinkFolder(inputFolder, outputFolder);

        // Resizers
        inputFolder = $"{workingDirectory}/Resizers";
        inputFolder = Path.GetFullPath(inputFolder);
        outputFolder = $"{TranslationWorkflowTests.GameFolder}/BepInEx/resizers";
        SymLinkFolder(inputFolder, outputFolder);
    }

    private static void SymLinkFolder(string inputFolder, string outputFolder)
    {
        if (Directory.Exists(outputFolder))
        {
            Console.WriteLine("Output folder already exists. Deleting it...");
            Directory.Delete(outputFolder, true);
        }

        // Run mklink command to create a symbolic link
        string command = $"/C mklink /D \"{outputFolder}\" \"{inputFolder}\"";
        ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", command)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            Verb = "runas" // Run as administrator
        };

        Process process = new Process { StartInfo = psi };
        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        // Display output or error
        if (!string.IsNullOrEmpty(output))
            Console.WriteLine("Success: " + output);
        if (!string.IsNullOrEmpty(error))
            throw new Exception("Error: " + error);
    }   

    [Fact]
    public void ReserializeResizerTest()
    {
        var serializer = Yaml.CreateSerializer();
        var deserializer = Yaml.CreateDeserializer();
        var folder = $"{workingDirectory}/Resizers";

        foreach (var file in Directory.EnumerateFiles(folder))
        {
            var newResizers = deserializer.Deserialize<List<TextResizerContract>>(File.ReadAllText(file));
            var content = serializer.Serialize(newResizers);
            File.WriteAllText(file, content);
        }
    }
}
