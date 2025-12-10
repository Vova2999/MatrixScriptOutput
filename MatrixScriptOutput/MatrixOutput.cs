using System.Text;

namespace MatrixScriptOutput;

public static class MatrixOutput
{
    private class MatrixOutputData
    {
        public required string Line { get; init; }
        public required int Position { get; init; }
        public int Index { get; set; }
    }

    private readonly struct MatrixOutputSymbol
    {
        public readonly char Symbol;
        public readonly byte Color;

        public MatrixOutputSymbol(char symbol, byte color)
        {
            Symbol = symbol;
            Color = color;
        }
    }

    private static readonly TimeSpan OutputDelay = TimeSpan.FromMilliseconds(70);

    private static readonly byte[] SymbolColors =
        Enumerable.Repeat((byte) 15, 1)
            .Concat(Enumerable.Repeat((byte) 46, 12))
            .Concat(Enumerable.Repeat((byte) 40, 6))
            .Concat(Enumerable.Repeat((byte) 34, 3))
            .Concat(Enumerable.Repeat((byte) 28, 3))
            .Concat(Enumerable.Repeat((byte) 22, 3))
            .ToArray();

    private static readonly Random Random = new();
    private static readonly object LockObject = new();
    private static readonly List<MatrixOutputData> Data = [];

    private static readonly MatrixOutputSymbol[][] Symbols =
        Enumerable.Range(0, Console.LargestWindowWidth)
            .Select(_ => Enumerable.Range(0, Console.LargestWindowHeight)
                .Select(_ => new MatrixOutputSymbol(' ', 97))
                .ToArray())
            .ToArray();

    static MatrixOutput()
    {
        RunOutputWorker();
    }

    private static void RunOutputWorker()
    {
        // ReSharper disable FunctionNeverReturns
        Task.Run(async () =>
        {
            while (true)
            {
                OutputWorker();
                await Task.Delay(OutputDelay);
            }
        });
        // ReSharper restore FunctionNeverReturns
    }

    private static void OutputWorker()
    {
        try
        {
            var changedPositions = new List<(int Width, int Height)>();

            lock (LockObject)
            {
                Data.RemoveAll(data => data.Index >= data.Line.Length + SymbolColors.Length);

                foreach (var data in Data)
                {
                    var width = data.Position;
                    foreach (var symbolColorIndex in Enumerable.Range(0, SymbolColors.Length))
                    {
                        var height = data.Index - symbolColorIndex;

                        if (height < 0 || height >= data.Line.Length)
                            continue;

                        if (Symbols[width][height].Symbol == data.Line[height] && (Symbols[width][height].Color == SymbolColors[symbolColorIndex] || data.Line[height] == ' '))
                            continue;

                        Symbols[width][height] = new MatrixOutputSymbol(data.Line[height], SymbolColors[symbolColorIndex]);
                        changedPositions.Add((width, height));
                    }

                    var clearSymbolHeight = data.Index - SymbolColors.Length;
                    if (clearSymbolHeight >= 0)
                    {
                        Symbols[width][clearSymbolHeight] = new MatrixOutputSymbol(' ', 0);
                        changedPositions.Add((width, clearSymbolHeight));
                    }

                    data.Index++;
                }
            }

            Console.CursorVisible = false;
            var stringBuilder = new StringBuilder(changedPositions.Count * 14);
            foreach (var (width, height) in changedPositions)
                stringBuilder.Append($"\u001b[{height + 1};{width + 1}H\u001b[38;5;{Symbols[width][height].Color}m{Symbols[width][height].Symbol}");

            Console.Write(stringBuilder.ToString());
        }
        catch
        {
            // ignored
        }
    }

    public static void WriteLine(string? line)
    {
        if (string.IsNullOrEmpty(line))
            return;

        var position = Random.Next(0, Console.WindowWidth);
        var data = new MatrixOutputData { Line = line, Position = position };

        lock (LockObject)
            Data.Add(data);
    }

    public static void WaitForExit()
    {
        while (true)
        {
            lock (LockObject)
            {
                if (!Data.Any())
                    break;
            }

            Task.Delay(OutputDelay * 1.1).Wait();
        }
    }
}