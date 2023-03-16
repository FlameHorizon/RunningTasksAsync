using System.Runtime.InteropServices;
using System.Text;

namespace LearningAboutThreads;

public static class Program
{
    [DllImport("user32.dll")]
    private static extern int EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(nint hWnd, StringBuilder strText, int maxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool SendMessage(nint hWnd, uint msg, int wParam, int lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(nint hWnd);
    
    // ReSharper disable once InconsistentNaming
    private const uint WM_CLOSE = 0x0010;

    private delegate bool EnumWindowsProc(nint hWnd, nint lParam);

    private static nint _felherWindowHandle;

    private static bool EnumWindowsCallback(nint hWnd, nint lParam)
    {
        var windowTitleBuilder = new StringBuilder(1024);
        int windowTitleLength = GetWindowTextLength(hWnd);
        if (GetWindowText(hWnd, windowTitleBuilder, windowTitleLength + 1) > 0)
        {
            var windowTitle = windowTitleBuilder.ToString();
            if (!windowTitle.Contains("Felher")) return true; // continue enumerating windows
            
            // store the handle to the "Felher" window
            _felherWindowHandle = hWnd;
            return false; // stop enumerating windows
        }

        return true; // continue enumerating windows
    }

    // ReSharper disable once UnusedParameter.Local
    private static async Task Main(string[] args)
    {
        var transactionCodes = new List<string> { "code1", "code2", "code3" };
        foreach (string code in transactionCodes)
        {
            Task task = Task.Run(async () =>
            {
                try
                {
                    // run the transaction code here
                    await Task.Delay(10000); // example code delay of 10 seconds

                    // periodically check if the "Felher" window still exists
                    while (IsWindow(_felherWindowHandle)) await Task.Delay(1000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Transaction {code} failed with error: {ex.Message}");
                }
            });

            if (await Task.WhenAny(task, Task.Delay(60000)) == task)
            {
                // task completed within the 60 second timeout
                Console.WriteLine($"Transaction {code} completed successfully");
            }
            else
            {
                // task timed out after 60 seconds
                Console.WriteLine($"Transaction {code} timed out and was cancelled");
                task.Dispose(); // dispose the task to cancel its execution

                // find and close the "Felher" window if it still exists
                _ = EnumWindows(EnumWindowsCallback, nint.Zero);
                if (_felherWindowHandle != nint.Zero && IsWindow(_felherWindowHandle))
                    SendMessage(_felherWindowHandle, WM_CLOSE, 0, 0);
            }
        }
    }
}