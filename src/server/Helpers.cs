using System;
using System.Buffers.Binary;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using Sqids;

namespace RuleFlowRenderEngine.server;


/// <summary>
/// A static class that provides configuration options.
/// IMPORTANT: This class must be kept up to date with the client side configuration.
/// </summary>
public static class Config
{
    public const int Port = 31415;
    public const int MaxMessageLength = 10485760; // 10 * 1024 * 1024 = 10485760 (that is 10MB)
    public const int MaxMessageChannelSize = 2048;
    public static readonly SqidsOptions SqidsCfg = new SqidsOptions()
    {
        Alphabet = "123456789ABCDEFGHIJKLMNPQRSTUVWXYZ"
    };

    public static readonly JsonSerializerOptions JsonSerializeOptions = new()
    {
        AllowTrailingCommas = true,
        IncludeFields = true,
        PropertyNameCaseInsensitive = true
    };
}


public static class NetworkHelpers
{
    /// <summary>
    /// Get the LocalHost (aka loop back) IP address.
    /// </summary>
    public static IPAddress LocalHost { get; }  = IPAddress.Loopback; // this the localhost IP 127.0.0.1

    /// <summary>
    /// Gets the local (LAN) IP address. Returns null if unsuccessful.
    /// </summary>
    /// <returns>IPAddress</returns>
    public static IPAddress GetLocalIpAddress()
    {
        // Establish a dummy socket. 8.8.8.8 does not receive any data.
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Connect("8.8.8.8", Config.Port);
            if (socket.LocalEndPoint is IPEndPoint endPoint)
            {
                return endPoint.Address;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"[NetworkHelper] Failed to find LAN IP address: {e}");
        }
        return null;
    }

    private static HttpClient _httpClient;
    /// <summary>
    /// Gets the global (WAN) IP address. Returns null if unsuccessful.
    /// </summary>
    /// <returns><c>Task&lt;IPAddress&gt;</c></returns>
    public static IPAddress GetGlobalIpAddress()
    {
        _httpClient ??= new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(4)
        };
        
        // Define primary and secondary mirrors
        string[] ipProviders =
        [
            "https://icanhazip.com",
            "https://api64.ipify.org/"
        ];

        foreach (var url in ipProviders)
        {
            try
            {
                var ipText = _httpClient.GetStringAsync(url).Result;
                if (!string.IsNullOrWhiteSpace(ipText))
                {
                    return IPAddress.Parse(ipText.Trim());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[NetworkHelper] Failed to find WAN IP address: {e}");
            }
        }
        return null;
    }
}

public static class IpAddressSqidsEncoding
{
    public static SqidsEncoder<uint> Sqids { get; } = new(Config.SqidsCfg);

    public static string Encode(IPAddress ip)
    {
        var n = BinaryPrimitives.ReadUInt32BigEndian(ip.GetAddressBytes());
        return Sqids.Encode(n);
    }
    
    public static IPAddress Decode(string sqidsCode)
    {
        var n = Sqids.Decode((ReadOnlySpan<char>)sqidsCode)[0];
        byte[] bytes = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian((Span<byte>)bytes, n);
        return new IPAddress(bytes);
    }
}
