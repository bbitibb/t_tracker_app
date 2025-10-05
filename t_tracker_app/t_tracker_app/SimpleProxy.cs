using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace t_tracker_app;

public sealed class SimpleProxy : IDisposable
{
    private readonly TcpListener _listener;
    private readonly int _port;
    private bool _disposed;

    public SimpleProxy(int port)
    {
        _port = port;
        _listener = new TcpListener(IPAddress.Loopback, port);
    }

    public void Start(Func<string,string?,Task> onVisitAsync)
    {
        _listener.Start();
        _ = AcceptLoop(onVisitAsync);
    }

    private async Task AcceptLoop(Func<string,string?,Task> onVisitAsync)
    {
        try
        {
            while (!_disposed)
            {
                var client = await _listener.AcceptTcpClientAsync();
                _ = HandleClientAsync(client, onVisitAsync);
            }
        }
        catch (ObjectDisposedException) { }
    }

    private static bool IsLocal(string host)
        => host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
        || host.StartsWith("127.");

    private static async Task HandleClientAsync(TcpClient client, Func<string,string?,Task> onVisitAsync)
    {
        using var c = client;
        using var s = c.GetStream();
        using var reader = new StreamReader(s, Encoding.ASCII, false, 8192, leaveOpen:true);
        using var writer = new StreamWriter(s, new UTF8Encoding(false), 8192, leaveOpen:true) { NewLine="\r\n", AutoFlush=true };

        var reqLine = await reader.ReadLineAsync();
        if (string.IsNullOrEmpty(reqLine)) return;

        string? line;
        while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync())) { }

        var parts = reqLine.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return;

        var method = parts[0].ToUpperInvariant();
        var target = parts[1];

        if (method == "CONNECT")
        {
            var hostPort = target.Split(':');
            var host = hostPort[0];
            var port = hostPort.Length > 1 && int.TryParse(hostPort[1], out var p) ? p : 443;

            if (!IsLocal(host))
                await onVisitAsync(host, null);

            using var remote = new TcpClient();
            await remote.ConnectAsync(host, port);
            await writer.WriteAsync("HTTP/1.1 200 Connection Established\r\n\r\n");

            var rs = remote.GetStream();
            await Task.WhenAny(s.CopyToAsync(rs), rs.CopyToAsync(s));
        }
        else
        {
            if (!Uri.TryCreate(target, UriKind.Absolute, out var uri)) return;
            if (!IsLocal(uri.Host))
                await onVisitAsync(uri.Host, uri.ToString());

            using var remote = new TcpClient();
            await remote.ConnectAsync(uri.Host, uri.Port == -1 ? 80 : uri.Port);
            var rs = remote.GetStream();
            using var rw = new StreamWriter(rs, new UTF8Encoding(false), 8192, leaveOpen:true) { NewLine="\r\n", AutoFlush=true };

            await rw.WriteLineAsync($"{method} {uri.PathAndQuery} HTTP/1.1");
            await rw.WriteLineAsync($"Host: {uri.Host}");
            await rw.WriteLineAsync();

            await Task.WhenAny(s.CopyToAsync(rs), rs.CopyToAsync(s));
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _listener.Stop();
    }
}
