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
            PrintChangedPositions(CalculateChangedPositions());
        }
        catch
        {
            // ignored
        }
    }

    private static List<(int Width, int Height)> CalculateChangedPositions()
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
                    var lineHeight = data.Index - symbolColorIndex;
                    var symbolHeight = lineHeight % Console.WindowHeight;

                    if (lineHeight < 0 || lineHeight >= data.Line.Length)
                        continue;

                    if (Symbols[width][symbolHeight].Symbol == data.Line[lineHeight] && (Symbols[width][symbolHeight].Color == SymbolColors[symbolColorIndex] || data.Line[lineHeight] == ' '))
                        continue;

                    Symbols[width][symbolHeight] = new MatrixOutputSymbol(data.Line[lineHeight], SymbolColors[symbolColorIndex]);
                    changedPositions.Add((width, symbolHeight));
                }

                var clearSymbolHeight = (data.Index - SymbolColors.Length) % Console.WindowHeight;
                if (clearSymbolHeight >= 0)
                {
                    Symbols[width][clearSymbolHeight] = new MatrixOutputSymbol(' ', 0);
                    changedPositions.Add((width, clearSymbolHeight));
                }

                data.Index++;
            }
        }

        return changedPositions;
    }

    private static void PrintChangedPositions(List<(int Width, int Height)> changedPositions)
    {
        var stringBuilder = new StringBuilder(changedPositions.Count * 14);
        foreach (var (width, height) in changedPositions)
            stringBuilder.Append($"\u001b[{height + 1};{width + 1}H\u001b[38;5;{Symbols[width][height].Color}m{Symbols[width][height].Symbol}");

        Console.CursorVisible = false;
        Console.Write(stringBuilder.ToString());
    }

    public static void WriteLine(string? line)
    {
        if (string.IsNullOrEmpty(line))
            return;

        (int Position, int Index)[] dataCopy;

        lock (LockObject)
            dataCopy = Data.Select(data => (data.Position, data.Index)).ToArray();

        var position = GetNewPosition(dataCopy);

        lock (LockObject)
            Data.Add(new MatrixOutputData { Line = line, Position = position });
    }

    private static int GetNewPosition((int Position, int Index)[] dataCopy)
    {
        var allPositions = Enumerable.Range(0, Console.WindowWidth).Select(position => (int?) position).ToHashSet();
        var positionWithoutNeighbors = allPositions.Except(dataCopy.SelectMany(data => Enumerable.Range(-1, 3).Select(index => (int?) index + data.Position))).OrderBy(_ => Random.Next()).FirstOrDefault();
        if (positionWithoutNeighbors.HasValue)
            return positionWithoutNeighbors.Value;

        var positionWithNeighbors = allPositions.Except(dataCopy.Select(data => (int?) data.Position)).OrderBy(_ => Random.Next()).FirstOrDefault();
        if (positionWithNeighbors.HasValue)
            return positionWithNeighbors.Value;

        var middleHeight = Console.WindowHeight * 2 / 3;
        var positionToMiddleDeviation = dataCopy.GroupBy(data => data.Position).ToDictionary(group => group.Key, group => group.Max(data => Math.Abs(data.Index % Console.WindowHeight - middleHeight)));
        var minMiddleDeviation = positionToMiddleDeviation.Values.Min();
        var positionWithWorkedData = positionToMiddleDeviation.Where(group => group.Value == minMiddleDeviation).OrderBy(_ => Random.Next()).Select(group => group.Key).First();
        return positionWithWorkedData;
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