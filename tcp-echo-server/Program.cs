using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

var port = args.Length > 0 && int.TryParse(args[0], out var p) ? p : 7001;
var ip = IPAddress.Loopback;

var listener = new TcpListener(ip, port);
listener.Start();
Console.WriteLine($"Echo server listening on {ip}:{port}");

while (true)
{
    var client = await listener.AcceptTcpClientAsync();
    _ = Task.Run(async () =>
    {
        using var c = client;
        var remote = c.Client.RemoteEndPoint?.ToString() ?? "?";
        Console.WriteLine($"Accepted {remote}");
        var stream = c.GetStream();
        // Send a simple banner so clients can see which backend handled the connection
        try
        {
            var banner = $"echo-backend {ip}:{port}\r\n";
            var bannerBytes = Encoding.ASCII.GetBytes(banner);
            await stream.WriteAsync(bannerBytes, 0, bannerBytes.Length);
        }
        catch { /* ignore banner write errors */ }
        var buffer = new byte[8192];
        int n;
        try
        {
            while ((n = await stream.ReadAsync(buffer)) > 0)
            {
                await stream.WriteAsync(buffer.AsMemory(0, n));
            }
        }
        catch (IOException ioEx)
        {
            if (ioEx.InnerException is SocketException se &&
                (se.SocketErrorCode == SocketError.ConnectionReset || se.SocketErrorCode == SocketError.ConnectionAborted))
            {
                // Common on quick client disconnects (health checks, Test-NetConnection, telnet exit)
            }
            else
            {
                Console.WriteLine($"Client error: {ioEx.Message}");
            }
        }
        catch (SocketException se)
        {
            if (se.SocketErrorCode == SocketError.ConnectionReset || se.SocketErrorCode == SocketError.ConnectionAborted)
            {
                // Common on quick client disconnects
            }
            else
            {
                Console.WriteLine($"Client error: {se.Message}");
            }
        }
        catch (ObjectDisposedException)
        {
            // Stream disposed due to client closing
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Client error: {ex.Message}");
        }
    });
}
