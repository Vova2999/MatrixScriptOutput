using System.Diagnostics;

namespace MatrixScriptOutput;

public static class Program
{
    private const int ConsoleWidth = 50;
    private static readonly int ConsoleHeight = Console.LargestWindowHeight * 4 / 5;

    public static void Main(string[] args)
    {
        ConsoleCustomize.EnableUtf8Support();
        ConsoleCustomize.DeleteMinimize();
        ConsoleCustomize.DeleteMaximize();
        ConsoleCustomize.DeleteResize();

        Console.SetWindowSize(ConsoleWidth, ConsoleHeight);
        Console.SetBufferSize(ConsoleWidth, ConsoleHeight);

        if (!args.Any())
        {
            Console.WriteLine("Not specified path to script");
            Console.ReadKey();
            return;
        }

        var scriptPath = args.First();
        if (!File.Exists(scriptPath))
        {
            Console.WriteLine("Not found script by path");
            Console.ReadKey();
            return;
        }

        RunBatchScript(scriptPath);
        MatrixOutput.WaitForExit();
    }

    private static void RunBatchScript(string scriptPath)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c \"{scriptPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(scriptPath) ?? Environment.CurrentDirectory
        };

        using var process = new Process();
        process.StartInfo = processInfo;
        process.OutputDataReceived += FormatAndPrint;
        process.ErrorDataReceived += FormatAndPrint;

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
    }

    private static void FormatAndPrint(object sender, DataReceivedEventArgs dataReceivedEventArgs)
    {
        MatrixOutput.WriteLine(dataReceivedEventArgs.Data);
    }
}