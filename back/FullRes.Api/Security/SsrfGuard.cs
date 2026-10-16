using System.Net;
using System.Net.Sockets;

namespace FullRes.Api.Security;

public static class SsrfGuard
{
    private static readonly HashSet<ushort> AllowedPorts = [ 80, 443 ];

    public static async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var uri = context.InitialRequestMessage.RequestUri!;
        ValidateUri(uri);

        IPAddress[] addresses;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(uri.Host, cancellationToken);
        }
        catch (Exception ex) when (ex is SocketException or ArgumentException)
        {
            throw new HttpRequestException($"Could not resolve host '{uri.Host}'.", ex);
        }

        var allowed = addresses.Where(IsPublicAddress).ToArray();
        if (allowed.Length == 0)
            throw new HttpRequestException($"Host '{uri.Host}' resolves only to non-public addresses.");

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(allowed, uri.Port, cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    public static void ValidateUri(Uri uri)
    {
        if (uri.Scheme is not ("http" or "https"))
            throw new HttpRequestException($"Scheme '{uri.Scheme}' is not allowed.");
        if (uri.Port is not (80 or 443) && !uri.IsDefaultPort)
            throw new HttpRequestException($"Port {uri.Port} is not allowed.");

        var host = uri.Host;
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".local", StringComparison.OrdinalIgnoreCase) ||
            host.Equals("metadata.google.internal", StringComparison.OrdinalIgnoreCase))
            throw new HttpRequestException($"Host '{host}' is not allowed.");

        if (IPAddress.TryParse(host, out var literal) && !IsPublicAddress(literal))
            throw new HttpRequestException("IP literals that are not public are not allowed.");
    }

    public static bool IsPublicAddress(IPAddress address)
    {
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast || address.IsIPv6UniqueLocal)
                return false;
            if (address.IsIPv4MappedToIPv6)
                return IsPublicAddress(address.MapToIPv4());
            if (address.Equals(IPAddress.IPv6Loopback) || address.Equals(IPAddress.IPv6Any))
                return false;
            return true;
        }

        if (address.AddressFamily != AddressFamily.InterNetwork)
            return false;

        var bytes = address.GetAddressBytes();
        var a = bytes[0];
        var b = bytes[1];

        if (a == 0 || a == 10 || a == 127)
            return false;
        if (a == 100 && b is >= 64 and <= 127)
            return false;
        if (a == 169 && b == 254)
            return false;
        if (a == 172 && b is >= 16 and <= 31)
            return false;
        if (a == 192 && b == 168)
            return false;
        if (a == 192 && b == 0)
            return false;
        if (a == 198 && b is 18 or 19)
            return false;
        if (a == 198 && b == 51)
            return false;
        if (a == 203 && b == 0)
            return false;
        if (a >= 224)
            return false;

        return true;
    }

    public static bool IsPortAllowed(int port) => AllowedPorts.Contains((ushort)port);
}
