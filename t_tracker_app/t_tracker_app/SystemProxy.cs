using Microsoft.Win32;
using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace t_tracker_app;

public static class SystemProxy
{
    private const string K  = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";
    private const string BK = @"Software\t_tracker\ProxyBackup";

    public static void EnableLocalProxy(int port)
    {
        using (var k = Registry.CurrentUser.OpenSubKey(K, writable: false)!)
        using (var bk = Registry.CurrentUser.CreateSubKey(BK)!)
        {
            bk.SetValue("ProxyEnable",   k.GetValue("ProxyEnable", 0),   RegistryValueKind.DWord);
            bk.SetValue("ProxyServer",   k.GetValue("ProxyServer", ""),  RegistryValueKind.String);
            bk.SetValue("ProxyOverride", k.GetValue("ProxyOverride",""),  RegistryValueKind.String);
        }

        using (var k = Registry.CurrentUser.OpenSubKey(K, writable: true)!)
        {
            k.SetValue("ProxyEnable", 1, RegistryValueKind.DWord);
            k.SetValue("ProxyServer", $"127.0.0.1:{port}", RegistryValueKind.String);
            k.SetValue("ProxyOverride", "localhost;127.0.0.1;<local>", RegistryValueKind.String);
        }
        NotifyWinInet();
    }
    public static void RestoreIfOwned(int port)
    {
        string curServer;
        using (var k = Registry.CurrentUser.OpenSubKey(K, writable: false)!)
            curServer = (k.GetValue("ProxyServer") as string) ?? string.Empty;

        if (!curServer.Equals($"127.0.0.1:{port}", StringComparison.OrdinalIgnoreCase))
            return;

        using var bk = Registry.CurrentUser.OpenSubKey(BK, writable: false);
        if (bk is not null)
        {
            using var k = Registry.CurrentUser.OpenSubKey(K, writable: true)!;
            k.SetValue("ProxyEnable",   (int)(bk.GetValue("ProxyEnable", 0) ?? 0),  RegistryValueKind.DWord);
            k.SetValue("ProxyServer",   (string)(bk.GetValue("ProxyServer", "") ?? ""), RegistryValueKind.String);
            k.SetValue("ProxyOverride", (string)(bk.GetValue("ProxyOverride","") ?? ""), RegistryValueKind.String);
        }
        else
        {
            using var k = Registry.CurrentUser.OpenSubKey(K, writable: true)!;
            k.SetValue("ProxyEnable", 0, RegistryValueKind.DWord);
        }
        NotifyWinInet();
    }
    public static void HealIfDead(int port)
    {
        string curServer;
        using (var k = Registry.CurrentUser.OpenSubKey(K, writable: false)!)
            curServer = (k.GetValue("ProxyServer") as string) ?? string.Empty;

        if (!curServer.Equals($"127.0.0.1:{port}", StringComparison.OrdinalIgnoreCase))
            return;

        var alive = false;
        try { using var c = new TcpClient(); c.Connect("127.0.0.1", port); alive = true; } catch { }
        if (!alive) RestoreIfOwned(port);
    }
    public static void DisableProxy()
    {
        using var k = Registry.CurrentUser.OpenSubKey(K, writable: true)!;
        k.SetValue("ProxyEnable", 0, RegistryValueKind.DWord);
        NotifyWinInet();
    }
    static void NotifyWinInet()
    {
        InternetSetOption(IntPtr.Zero, 39, IntPtr.Zero, 0);
        InternetSetOption(IntPtr.Zero, 37, IntPtr.Zero, 0);
    }

    [DllImport("wininet.dll", SetLastError = true)]
    private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
}