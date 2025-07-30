using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace t_tracker_app;

public class WindowInfoFetcher
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    
    public (string WindowTitle, string ExeName) GetActiveWindowInfo()
    {
        var hwnd = GetForegroundWindow();
        var buffer = new StringBuilder(256);
        GetWindowText(hwnd, buffer, buffer.Capacity);
        string title = buffer.ToString();

        GetWindowThreadProcessId(hwnd, out uint pid);
        string exeName = "";

        try
        {
            using (var proc = Process.GetProcessById((int)pid))
            {
                exeName = proc.ProcessName;
            }
        }
        catch
        {
            exeName = "<unknown>";
        }

        return (title, exeName);
    }
}