using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace t_tracker_app;

public class WindowInfoFetcher
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    public virtual (string Title, string ExeName) GetActiveWindowInfo()
    {
        var hWnd = GetForegroundWindow();
        if (hWnd == IntPtr.Zero)
            return ("", "<unknown>");

        int len = 0;
        try
        {
            len = GetWindowTextLength(hWnd);
        }
        catch { }

        int capacity = Math.Clamp(len + 1, 1, 4096);
        var sb = new StringBuilder(capacity);

        string title;
        try
        {
            _ = GetWindowText(hWnd, sb, sb.Capacity);
            title = sb.ToString();
        }
        catch
        {
            title = "";
        }

        string exeName = "<unknown>";
        try
        {
            _ = GetWindowThreadProcessId(hWnd, out uint pid);
            using var proc = Process.GetProcessById((int)pid);
            exeName = proc.ProcessName;
        }
        catch
        {

        }

        if (!string.IsNullOrEmpty(title))
            title = title.Trim();

        return (title, exeName);
    }
}
