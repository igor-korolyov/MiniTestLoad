using System.Collections.Concurrent;

namespace MiniTestLoad;

internal class Display
{
    private readonly object _lock = new();

    private int _spinnerFrame = 0;

    private string _titleRow = string.Empty;

    private string _bottomRow = string.Empty;

    private string[] _threadRows = [];

    private readonly ConcurrentQueue<string> _logRows = [];

    private int _threadCount;

    private static readonly Display _instance = new();

    private static readonly char[] _spinnerFrames = ['/', '-', '\\', '|'];

    public static Display Instance => _instance;

    public int ThreadCount
    {
        get => _threadCount;
        set
        {
            lock (_lock)
            {
                _threadCount = value;
                _threadRows = Enumerable.Repeat(string.Empty, value).ToArray();
                Refresh();
            }
        }
    }

    public string TitleRow
    {
        get => _titleRow;
        set
        {
            lock (_lock)
            {
                _titleRow = value;
                Refresh();
            }
        }
    }

    public string BottomRow
    {
        get => _bottomRow;
        set
        {
            lock (_lock)
            {
                _bottomRow = value;
                Refresh();
            }
        }
    }

    public void SetThreadRow(int threadNumber, string message)
    {
        lock (_lock)
        {
            _threadRows[threadNumber] = message;
            Refresh();
        }
    }

    public void AddLogRow(string message)
    {
        _logRows.Enqueue(message);
        while (_logRows.Count > 1000)
        {
            _logRows.TryDequeue(out _);
        }
        lock (_lock)
        {
            Refresh();
        }
    }

    private void Refresh()
    {
        var oldBackgroundColor = Console.BackgroundColor;
        var oldForegroundColor = Console.ForegroundColor;

        Console.SetCursorPosition(0, 0);

        if (++_spinnerFrame >= _spinnerFrames.Length)
        {
            _spinnerFrame = 0;
        }

        Console.BackgroundColor = ConsoleColor.DarkGreen;
        WriteFullLine(_spinnerFrames[_spinnerFrame] + _titleRow);

        Console.BackgroundColor = ConsoleColor.DarkGray;
        foreach (var row in _threadRows)
        {
            WriteFullLine(row);
        }

        Console.BackgroundColor = ConsoleColor.Black;
        foreach (var row in _logRows.TakeLast(VisibleLogRowsCount))
        {
            WriteFullLine(row);
        }

        for (var i = Console.CursorTop; i < Console.WindowHeight - 1; i++)
        {
            WriteFullLine(string.Empty);
        }

        Console.BackgroundColor = ConsoleColor.DarkBlue;
        WriteBottomLine(_bottomRow);

        Console.BackgroundColor = oldBackgroundColor;
        Console.ForegroundColor = oldForegroundColor;
    }

    private static void WriteFullLine(string message) =>
        Console.WriteLine(message[..Math.Min(Console.WindowWidth - 1, message.Length)]
            .PadRight(Console.WindowWidth - 1));
    private static void WriteBottomLine(string message) =>
        Console.Write(message[..Math.Min(Console.WindowWidth - 1, message.Length)]
            .PadRight(Console.WindowWidth - 1));

    private int VisibleLogRowsCount => Console.WindowHeight - _threadCount - 2;
}
