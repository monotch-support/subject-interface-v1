using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.IO;

// ======== CONSTANTS ========
public static class Constants
{
    public static readonly byte[] HeaderPrefix = { 0xAA, 0xBB };
    public const string SecurityModeNone = "NONE";
    public const string SecurityModeTls = "TLSv1.2";
}

// ======== CONFIGURATION ========
public static class Config
{
    public static string StreamingApiBaseUrl { get; } = Environment.GetEnvironmentVariable("STREAMING_API_BASEURL") ?? "https://localhost/api";
    public static string StreamingApiTlcToken { get; } = Environment.GetEnvironmentVariable("STREAMING_API_TLC_TOKEN") ?? "your-tlc-auth-token";
    public static string StreamingApiBrokerToken { get; } = Environment.GetEnvironmentVariable("STREAMING_API_BROKER_TOKEN") ?? "your-broker-auth-token";
    public static string StreamingApiDomain { get; } = Environment.GetEnvironmentVariable("STREAMING_API_DOMAIN") ?? "dev_001";
    public static string StreamingApiSecurityMode { get; } = Environment.GetEnvironmentVariable("STREAMING_API_SECURITY_MODE") ?? "TLSv1.2";
    public static string StreamingApiIdentifier { get; } = Environment.GetEnvironmentVariable("STREAMING_API_IDENTIFIER") ?? "sub00001";
}

// ======== UTILITY FUNCTIONS ========
public static class Utilities
{
    public static void LogMessage(string threadName, string message)
    {
        Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{threadName}] {message}");
    }

    public static void LogJson(string threadName, string message, object data)
    {
        var json = JsonConvert.SerializeObject(data, Formatting.Indented);
        LogMessage(threadName, $"{message}: {json}");
    }

    public static string AsHexStream(byte[] data)
    {
        return "0x" + BitConverter.ToString(data).Replace("-", "");
    }

    public static long CurrentTimestamp()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}

// ======== REST API FUNCTIONS ========
public class SessionRequest
{
    [JsonProperty("domain")]
    public string Domain { get; set; } = "";

    [JsonProperty("type")]
    public string Type { get; set; } = "";

    [JsonProperty("protocol")]
    public string Protocol { get; set; } = "";

    [JsonProperty("details")]
    public SessionDetails Details { get; set; } = new();
}

public class SessionDetails
{
    [JsonProperty("securityMode")]
    public string SecurityMode { get; set; } = "";

    [JsonProperty("tlcIdentifiers")]
    public List<string> TlcIdentifiers { get; set; } = new();
}

public class SessionResponse
{
    [JsonProperty("token")]
    public string Token { get; set; } = "";

    [JsonProperty("details")]
    public SessionResponseDetails Details { get; set; } = new();
}

public class SessionResponseDetails
{
    [JsonProperty("listener")]
    public ListenerDetails Listener { get; set; } = new();
}

public class ListenerDetails
{
    [JsonProperty("host")]
    public string Host { get; set; } = "";

    [JsonProperty("port")]
    public int Port { get; set; }
}

public static class RestApi
{
    private static readonly HttpClient httpClient = new();

    public static async Task<(string host, int port, string token)> CreateSessionAsync(
        string sessionType, 
        string token, 
        string apiUrl, 
        string securityMode, 
        string identifier, 
        string threadName)
    {
        var url = $"{apiUrl}/v1/sessions";
        
        var requestData = new SessionRequest
        {
            Domain = Config.StreamingApiDomain,
            Type = sessionType,
            Protocol = "TCPStreaming_Multiplex",
            Details = new SessionDetails
            {
                SecurityMode = securityMode,
                TlcIdentifiers = new List<string> { identifier }
            }
        };

        var json = JsonConvert.SerializeObject(requestData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };
        request.Headers.Add("X-Authorization", token);

        var response = await httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Request failed with status code {response.StatusCode}");
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        var responseData = JsonConvert.DeserializeObject<object>(responseJson);
        Utilities.LogJson(threadName, "Session created successfully", responseData!);

        var sessionResponse = JsonConvert.DeserializeObject<SessionResponse>(responseJson);
        if (sessionResponse == null)
        {
            throw new InvalidOperationException("Failed to deserialize session response");
        }

        Utilities.LogMessage(threadName, $"Token: {sessionResponse.Token}");
        Utilities.LogMessage(threadName, $"Host: {sessionResponse.Details.Listener.Host}");
        Utilities.LogMessage(threadName, $"Port: {sessionResponse.Details.Listener.Port}");

        return (sessionResponse.Details.Listener.Host, sessionResponse.Details.Listener.Port, sessionResponse.Token);
    }
}

// ======== TCP STREAMING FUNCTIONS ========
public abstract class Connection : IDisposable
{
    public abstract int Read(byte[] buffer, int offset, int count);
    public abstract void Write(byte[] buffer, int offset, int count);
    public abstract void SetNonBlocking(bool nonBlocking);
    public abstract void Close();
    public abstract void Dispose();
}

public class PlainConnection : Connection
{
    private readonly TcpClient tcpClient;
    private readonly NetworkStream stream;

    public PlainConnection(TcpClient tcpClient)
    {
        this.tcpClient = tcpClient;
        this.stream = tcpClient.GetStream();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return stream.Read(buffer, offset, count);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        stream.Write(buffer, offset, count);
    }

    public override void SetNonBlocking(bool nonBlocking)
    {
        tcpClient.Client.Blocking = !nonBlocking;
    }

    public override void Close()
    {
        stream.Close();
        tcpClient.Close();
    }

    public override void Dispose()
    {
        stream.Dispose();
        tcpClient.Dispose();
    }
}

public class TlsConnection : Connection
{
    private readonly TcpClient tcpClient;
    private readonly SslStream sslStream;

    public TlsConnection(TcpClient tcpClient, string targetHost)
    {
        this.tcpClient = tcpClient;
        this.sslStream = new SslStream(tcpClient.GetStream(), false, ValidateServerCertificate);
        this.sslStream.AuthenticateAsClient(targetHost);
    }

    private static bool ValidateServerCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
    {
        // In production, implement proper certificate validation
        return true;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return sslStream.Read(buffer, offset, count);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        sslStream.Write(buffer, offset, count);
    }

    public override void SetNonBlocking(bool nonBlocking)
    {
        tcpClient.Client.Blocking = !nonBlocking;
    }

    public override void Close()
    {
        sslStream.Close();
        tcpClient.Close();
    }

    public override void Dispose()
    {
        sslStream.Dispose();
        tcpClient.Dispose();
    }
}

public static class TcpStreaming
{
    public static Connection Connect(string host, int port, bool useTls, string threadName)
    {
        var tcpClient = new TcpClient(host, port);
        
        if (useTls)
        {
            var tlsConnection = new TlsConnection(tcpClient, host);
            Utilities.LogMessage(threadName, $"Connected to {host}:{port} (TLS: {useTls})");
            Utilities.LogMessage(threadName, "TLS handshake successful");
            return tlsConnection;
        }
        else
        {
            Utilities.LogMessage(threadName, $"Connected to {host}:{port} (TLS: {useTls})");
            return new PlainConnection(tcpClient);
        }
    }

    public static void Handshake(Connection conn, string threadName)
    {
        var protocolVersionByte = new byte[] { 0x01 };
        conn.Write(protocolVersionByte, 0, protocolVersionByte.Length);

        var recv = new byte[1];
        conn.Read(recv, 0, recv.Length);

        Utilities.LogMessage(threadName, $"Received protocol version {Utilities.AsHexStream(recv)}");

        if (recv[0] != 1)
        {
            throw new InvalidOperationException("Unsupported protocol version received");
        }
    }

    public static void WriteDatagram(Connection conn, byte[] datagram, string threadName)
    {
        var header = new List<byte>();
        header.AddRange(Constants.HeaderPrefix);
        header.AddRange(BitConverter.GetBytes((ushort)datagram.Length).Reverse()); // Big-endian

        var frame = new List<byte>();
        frame.AddRange(header);
        frame.AddRange(datagram);

        var frameArray = frame.ToArray();
        Utilities.LogMessage(threadName, $"Writing frame {Utilities.AsHexStream(frameArray)}");

        conn.Write(frameArray, 0, frameArray.Length);
    }

    public static void WriteToken(Connection conn, string token, string threadName)
    {
        Utilities.LogMessage(threadName, $"Writing token {token}");
        var datagram = new List<byte> { 0x01 };
        datagram.AddRange(Encoding.UTF8.GetBytes(token));
        WriteDatagram(conn, datagram.ToArray(), threadName);
    }

    public static void WriteKeepAlive(Connection conn, string threadName)
    {
        Utilities.LogMessage(threadName, "Writing keep alive");
        var datagram = new byte[] { 0x00 };
        WriteDatagram(conn, datagram, threadName);
    }

    public static void WriteTimestampResponse(Connection conn, long timestampT0, long timestampT1, string threadName)
    {
        var timestampT2 = Utilities.CurrentTimestamp();
        Utilities.LogMessage(threadName, $"Writing timestamp response (t0: {timestampT0}, t1: {timestampT1}, t2: {timestampT2})");

        var datagram = new List<byte> { 0x07 };
        datagram.AddRange(BitConverter.GetBytes(timestampT0).Reverse()); // Big-endian
        datagram.AddRange(BitConverter.GetBytes(timestampT1).Reverse());
        datagram.AddRange(BitConverter.GetBytes(timestampT2).Reverse());

        WriteDatagram(conn, datagram.ToArray(), threadName);
    }

    public static void WritePayloadWithIdentifier(Connection conn, string identifier, byte payloadType, byte[] payload, string threadName)
    {
        Utilities.LogMessage(threadName, 
            $"Writing payload with identifier (identifier: {identifier}, payload_type: {Utilities.AsHexStream(new[] { payloadType })}): {Utilities.AsHexStream(payload)}");

        var datagram = new List<byte> { 0x05 };
        datagram.AddRange(Encoding.UTF8.GetBytes(identifier));
        datagram.Add(payloadType);
        datagram.AddRange(BitConverter.GetBytes(Utilities.CurrentTimestamp()).Reverse()); // Big-endian
        datagram.AddRange(payload);

        WriteDatagram(conn, datagram.ToArray(), threadName);
    }

    public static byte[]? ReadDatagram(Connection conn, string threadName)
    {
        // Set non-blocking mode for header read
        conn.SetNonBlocking(true);
        
        var header = new byte[4];
        int headerBytesRead = 0;
        
        try
        {
            headerBytesRead = conn.Read(header, 0, 4);
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.WouldBlock)
        {
            // Socket would block - no data available
            return null;
        }
        catch (IOException ex) when (ex.InnerException is SocketException socketEx && 
                                     socketEx.SocketErrorCode == SocketError.WouldBlock)
        {
            // SSL stream wraps socket exception in IOException
            return null;
        }
        
        if (headerBytesRead == 0)
        {
            throw new InvalidOperationException("Socket disconnected");
        }
        
        if (headerBytesRead != 4)
        {
            throw new InvalidOperationException("Received partial header (implementation does not support partial receives yet)");
        }

        Utilities.LogMessage(threadName, $"Received header {Utilities.AsHexStream(header)}");

        if (!header.Take(2).SequenceEqual(Constants.HeaderPrefix))
        {
            throw new InvalidOperationException($"Framing error: header prefix {Utilities.AsHexStream(header.Take(2).ToArray())} != {Utilities.AsHexStream(Constants.HeaderPrefix)}");
        }

        var size = (ushort)((header[2] << 8) | header[3]); // Big-endian
        Utilities.LogMessage(threadName, $"Trying to read {size} bytes datagram");

        // Set blocking mode for datagram body read (to ensure complete read)
        conn.SetNonBlocking(false);
        
        var datagram = new byte[size];
        var datagramBytesRead = 0;
        while (datagramBytesRead < size)
        {
            var bytesRead = conn.Read(datagram, datagramBytesRead, size - datagramBytesRead);
            if (bytesRead == 0)
                throw new InvalidOperationException("Socket disconnected");
            datagramBytesRead += bytesRead;
        }

        Utilities.LogMessage(threadName, $"Received datagram {Utilities.AsHexStream(datagram)}");

        return datagram;
    }

    public static void HandleKeepAlive(Connection conn, string threadName)
    {
        Utilities.LogMessage(threadName, "Keep alive received");
    }

    public static void HandleBye(Connection conn, byte[] datagram, string threadName)
    {
        Utilities.LogMessage(threadName, "Bye received");
        var reason = Encoding.UTF8.GetString(datagram, 1, datagram.Length - 1);
        Utilities.LogMessage(threadName, $"Bye reason: {reason}");
    }

    public static void HandlePayloadWithIdentifier(Connection conn, byte[] datagram, Action<string, byte, long, byte[]> payloadReceivedCallback, string threadName)
    {
        Utilities.LogMessage(threadName, "Payload with identifier received");
        var identifier = Encoding.UTF8.GetString(datagram, 1, 8);
        var payloadType = datagram[9];
        var originTimestamp = BitConverter.ToInt64(datagram.Skip(10).Take(8).Reverse().ToArray(), 0);
        var payload = datagram.Skip(18).ToArray();

        Utilities.LogMessage(threadName, 
            $"Payload received (identifier: {identifier}, payload_type: {Utilities.AsHexStream(new[] { payloadType })}, origin_timestamp: {originTimestamp}): {Utilities.AsHexStream(payload)}");

        payloadReceivedCallback(identifier, payloadType, originTimestamp, payload);
    }

    public static void HandleTimestampRequest(Connection conn, byte[] datagram, string threadName)
    {
        Utilities.LogMessage(threadName, "Timestamp request received");
        var timestampT0 = BitConverter.ToInt64(datagram.Skip(1).Take(8).Reverse().ToArray(), 0);
        var timestampT1 = Utilities.CurrentTimestamp();
        Utilities.LogMessage(threadName, $"Timestamp request delta: {timestampT1 - timestampT0}ms");
        WriteTimestampResponse(conn, timestampT0, timestampT1, threadName);
    }

    public static void HandleDatagram(Connection conn, byte[] datagram, Action<string, byte, long, byte[]> payloadReceivedCallback, string threadName)
    {
        if (datagram.Length == 0) return;

        switch (datagram[0])
        {
            case 0x00:
                HandleKeepAlive(conn, threadName);
                break;
            case 0x02:
                HandleBye(conn, datagram, threadName);
                break;
            case 0x05:
                HandlePayloadWithIdentifier(conn, datagram, payloadReceivedCallback, threadName);
                break;
            case 0x06:
                HandleTimestampRequest(conn, datagram, threadName);
                break;
            default:
                Utilities.LogMessage(threadName, $"Unknown/unimplemented datagram type {Utilities.AsHexStream(new[] { datagram[0] })} received");
                break;
        }
    }

    public static void RunStreamingClient(
        string host, 
        int port, 
        string sessionToken, 
        bool useTls, 
        Action<string, byte, long, byte[]> payloadReceivedCallback, 
        Action<Connection> loopCallback, 
        string threadName,
        CancellationToken cancellationToken)
    {
        using var conn = Connect(host, port, useTls, threadName);
        
        Handshake(conn, threadName);
        WriteToken(conn, sessionToken, threadName);

        while (!cancellationToken.IsCancellationRequested)
        {
            var datagram = ReadDatagram(conn, threadName);
            if (datagram != null)
            {
                HandleDatagram(conn, datagram, payloadReceivedCallback, threadName);
            }
            else
            {
                Thread.Sleep(10);
            }

            loopCallback(conn);
        }

        Utilities.LogMessage(threadName, "Shutdown signal received, terminating");
    }
}

// ======== PRODUCER ========
public static class Producer
{
    public static async Task RunAsync(CancellationToken cancellationToken)
    {
        const string threadName = "producer";

        try
        {
            // Step 1: Create a session using the REST API
            var (host, port, token) = await RestApi.CreateSessionAsync(
                "TLC",
                Config.StreamingApiTlcToken,
                Config.StreamingApiBaseUrl,
                Config.StreamingApiSecurityMode,
                Config.StreamingApiIdentifier,
                threadName);

            // Step 2: Connect to the TCP Streaming Node
            var lastWrite = Utilities.CurrentTimestamp();
            var identifier = Config.StreamingApiIdentifier;

            void WriteCallback(Connection conn)
            {
                var now = Utilities.CurrentTimestamp();
                // Write a random payload every second
                if (now - lastWrite > 1000)
                {
                    lastWrite = now;
                    var payload = new byte[100];
                    RandomNumberGenerator.Fill(payload);
                    TcpStreaming.WritePayloadWithIdentifier(conn, identifier, 0x02, payload, threadName);
                }
            }

            void ReadPayloadCallback(string identifier, byte payloadType, long originTimestamp, byte[] payload)
            {
                Utilities.LogMessage(threadName,
                    $"Producer received payload from {identifier}: type=0x{payloadType:X2}, timestamp={originTimestamp}, size={payload.Length}");
            }

            var useTls = Config.StreamingApiSecurityMode == Constants.SecurityModeTls;
            TcpStreaming.RunStreamingClient(host, port, token, useTls, ReadPayloadCallback, WriteCallback, threadName, cancellationToken);
        }
        catch (Exception ex)
        {
            Utilities.LogMessage(threadName, $"Producer error: {ex.Message}");
        }
    }
}

// ======== CONSUMER ========
public static class Consumer
{
    public static async Task RunAsync(CancellationToken cancellationToken)
    {
        const string threadName = "consumer";

        try
        {
            // Step 1: Create a session using the REST API
            var (host, port, token) = await RestApi.CreateSessionAsync(
                "Broker",
                Config.StreamingApiBrokerToken,
                Config.StreamingApiBaseUrl,
                Config.StreamingApiSecurityMode,
                Config.StreamingApiIdentifier,
                threadName);

            // Step 2: Connect to the TCP Streaming Node
            var lastWrite = Utilities.CurrentTimestamp();

            void WriteCallback(Connection conn)
            {
                var now = Utilities.CurrentTimestamp();
                // Write a keepalive every 5 seconds
                if (now - lastWrite > 5000)
                {
                    lastWrite = now;
                    TcpStreaming.WriteKeepAlive(conn, threadName);
                }
            }

            void ReadPayloadCallback(string identifier, byte payloadType, long originTimestamp, byte[] payload)
            {
                var latency = Utilities.CurrentTimestamp() - originTimestamp;
                Utilities.LogMessage(threadName,
                    $"Consumer received payload from {identifier}: type=0x{payloadType:X2}, timestamp={originTimestamp}, latency={latency}ms, size={payload.Length}");
            }

            var useTls = Config.StreamingApiSecurityMode == Constants.SecurityModeTls;
            TcpStreaming.RunStreamingClient(host, port, token, useTls, ReadPayloadCallback, WriteCallback, threadName, cancellationToken);
        }
        catch (Exception ex)
        {
            Utilities.LogMessage(threadName, $"Consumer error: {ex.Message}");
        }
    }
}

// ======== STARTUP AND RUN LOOP ========
public static class Program
{
    public static void DumpConfig()
    {
        Console.WriteLine($"STREAMING_API_BASEURL: '{Config.StreamingApiBaseUrl}'");
        Console.WriteLine($"STREAMING_API_TLC_TOKEN: '{Config.StreamingApiTlcToken}'");
        Console.WriteLine($"STREAMING_API_BROKER_TOKEN: '{Config.StreamingApiBrokerToken}'");
        Console.WriteLine($"STREAMING_API_DOMAIN: '{Config.StreamingApiDomain}'");
        Console.WriteLine($"STREAMING_API_SECURITY_MODE: '{Config.StreamingApiSecurityMode}'");
    }

    public static async Task Main(string[] args)
    {
        DumpConfig();

        var cancellationTokenSource = new CancellationTokenSource();

        // Set up CTRL-C signal handler
        Console.CancelKeyPress += (sender, e) =>
        {
            Console.WriteLine("Received CTRL-C signal, shutting down...");
            e.Cancel = true;
            cancellationTokenSource.Cancel();
        };

        var producerTask = Task.Run(() => Producer.RunAsync(cancellationTokenSource.Token));
        var consumerTask = Task.Run(() => Consumer.RunAsync(cancellationTokenSource.Token));

        await Task.WhenAll(producerTask, consumerTask);

        Console.WriteLine("Application terminated gracefully");
    }
}