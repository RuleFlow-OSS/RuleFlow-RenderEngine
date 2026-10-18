using System;
using System.Buffers.Binary;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace RuleFlowRenderEngine.server;

public class Server
{
    /// True when the FSM is active.
    public bool Awake { get; private set; } = false;  
    
    /// Tells us if a client has been connected.
    public bool ClientConnected { get; private set; } = false;
    
    /// This is what handles incoming and outgoing message logic.
    private readonly MessageChannels _messageChannels = new();
    public MessageChannels GetMessageHandler() => _messageChannels;
    
    // This section is internal functionality
    public readonly TcpListener ServerSocket = new(IPAddress.Any, Config.Port);
    public TcpClient ClientSocket;
    public CancellationTokenSource Cts;
    public CancellationToken Ct;
    
    /// Enter the FSM in a new thread
    public void Start()
    {
        if (Awake) return;
        Awake = true;
        Cts = new CancellationTokenSource();
        Ct = Cts.Token;
        Task.Run(WaitingState);
    } 

    /// Cancel the current session
    public void Stop()
    {
        if (!Awake) return;
        try { Cts?.Cancel(); }
        catch (ObjectDisposedException) { }
    }
    
    // ======== FSM States (primary transition code is defined within state) ========
    async Task WaitingState()
    {
        ServerSocket.Start();
        try
        {
            // We wait here until there is a client in the queue.
            ClientSocket = await ServerSocket.AcceptTcpClientAsync(Ct);
        }
        catch (Exception ex) when (ex is SocketException or OperationCanceledException)
        {
            ServerSocket.Stop();
            Cts.Dispose();
            Awake = false;
            return;
        }
        ServerSocket.Stop();
        
        // Transition into the connected state
        try
        {
            ClientConnected = true;
            using NetworkStream stream = ClientSocket.GetStream();
            Task outgoingData = ProcessOutgoingQueue(stream);
            Task incomingData = ConnectedState(stream);
            await Task.WhenAny(incomingData, outgoingData);
            await Cts.CancelAsync(); // we have this for robustness... if something happens to one of the processes, cancel all.
            await Task.WhenAll(incomingData, outgoingData);
        }
        finally
        {
            // Exit FSM
            ClientSocket.Close();
            Cts.Dispose();
            _messageChannels.Drain();
            ClientConnected = false;
            Awake = false;
        }
    }
    
    async Task ConnectedState(NetworkStream stream)
    {
        ChannelWriter<Message> channelWriter = _messageChannels.GetIncomingWriter();
        byte[] lengthBuffer = new byte[4];
        try
        {
            while (true)
            {
                await stream.ReadExactlyAsync(lengthBuffer, 0, 4, Ct); // read the length prefix
                int messageLength = BinaryPrimitives.ReadInt32BigEndian((ReadOnlySpan<byte>)lengthBuffer); // we assume correct bytes
                if (messageLength is <= 0 or > Config.MaxMessageLength)
                {
                    Console.WriteLine($"A message of length {messageLength} could not be parsed.");
                    continue;
                }
                byte[] messageBuffer = new byte[messageLength];
                await stream.ReadExactlyAsync(messageBuffer, 0, messageLength, Ct);
                try
                {
                    Message msg = Message.FromBytes((ReadOnlySpan<byte>)messageBuffer);
                    if (msg is not null)
                        await channelWriter.WriteAsync(msg, Ct);
                }
                catch (JsonException)
                {
                    Console.WriteLine($"A message of length {messageLength} could not be parsed.");
                }
            }
        } // all errors regarding the reading buffer must break the loop because we lose position.
        catch (EndOfStreamException){}
        catch (OperationCanceledException){}
        catch (Exception e)
        {
            Console.WriteLine($"Unexpected Error in <connected_state.receive_data()>: {e}.");
        }
    }
    
    async Task ProcessOutgoingQueue(NetworkStream stream)
    {
        try
        {
            ChannelReader<Message> channelReader = _messageChannels.GetOutgoingReader();
            await foreach (Message message in channelReader.ReadAllAsync(Ct))
            {
                var messageBytes = (ReadOnlyMemory<byte>)message.ToBytes();
                byte[] buffer = new byte[4 + messageBytes.Length];
                BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(0, 4), messageBytes.Length);
                messageBytes.CopyTo(buffer.AsMemory(4));
                await stream.WriteAsync(buffer, Ct);
            }
        } 
        catch (OperationCanceledException){}
        catch (Exception e)
        {
            Console.WriteLine($"Unexpected Error in <connected_state.send_data()>: {e}.");
        }
    }
}
