using System.Runtime.InteropServices;
using System.Text;

namespace MatrixScriptOutput;

public static class ConsoleCustomize
{
    private const int StdOutputHandle = -11;
    private const uint EnableVirtualTerminalProcessing = 0x0004;

    private const int MfByCommand = 0x00000000;
    private const int ScClose = 0xF060;
    private const int ScMinimize = 0xF020;
    private const int ScMaximize = 0xF030;
    private const int ScSize = 0xF000;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    [DllImport("user32.dll")]
    private static extern int DeleteMenu(IntPtr hMenu, int nPosition, int wFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

    [DllImport("kernel32.dll", ExactSpelling = true)]
    private static extern IntPtr GetConsoleWindow();

    private static readonly IntPtr ConsoleWindow;
    private static readonly IntPtr SystemMenu;

    static ConsoleCustomize()
    {
        ConsoleWindow = GetConsoleWindow();
        SystemMenu = GetSystemMenu(ConsoleWindow, false);
    }

    public static void EnableUtf8Support()
    {
        Console.OutputEncoding = Encoding.UTF8;

        var stdout = GetStdHandle(StdOutputHandle);

        GetConsoleMode(stdout, out var mode);
        SetConsoleMode(stdout, mode | EnableVirtualTerminalProcessing);
    }

    public static void DeleteClose()
    {
        if (ConsoleWindow != IntPtr.Zero)
            DeleteMenu(SystemMenu, ScClose, MfByCommand);
    }

    public static void DeleteMinimize()
    {
        if (ConsoleWindow != IntPtr.Zero)
            DeleteMenu(SystemMenu, ScMinimize, MfByCommand);
    }

    public static void DeleteMaximize()
    {
        if (ConsoleWindow != IntPtr.Zero)
            DeleteMenu(SystemMenu, ScMaximize, MfByCommand);
    }

    public static void DeleteResize()
    {
        if (ConsoleWindow != IntPtr.Zero)
            DeleteMenu(SystemMenu, ScSize, MfByCommand);
    }
}