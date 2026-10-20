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
    /// Fired immediately when a client connects and streams are ready.
    public event Action OnConnected;
    
    /// Fired when an active client session ends and cleanup is complete.
    public event Action OnDisconnected;
    
    /// Fired whenever the FSM completely exits (whether from WaitingState or ConnectedState).
    public event Action OnStopped;
    
    /// True when the FSM is active.
    public bool Awake { get; private set; }
    
    /// Tells us if a client has been connected.
    public bool ClientConnected { get; private set; }
    
    /// This is what handles incoming and outgoing message logic.
    public readonly MessageChannels MessageChannels = new();
    
    // This section is internal functionality
    public TcpListener ServerSocket { get; } = new(IPAddress.Any, Config.Port);
    public TcpClient ClientSocket { get; set; }
    public CancellationTokenSource Cts { get; set; }
    public CancellationToken Ct { get; set; }
    
    /// Enter the FSM in a new thread
    public void Start()
    {
        if (Awake) return;
        Awake = true;
        Cts = new CancellationTokenSource();
        Ct = Cts.Token;
        Task.Run(WaitingState);  // we don't use Ct in the Run due do wanting WaitingState to properly handle cancellation.
    }

    /// Cancel the current session
    public void Stop()
    {
        if (!Awake) return;
        Cts?.Cancel();
    }
    
    // ======== FSM States (primary transition code is defined within state) ========
    private async Task WaitingState()
    {
        ServerSocket.Start();
        try
        {
            // We wait here until there is a client in the queue.
            ClientSocket = await ServerSocket.AcceptTcpClientAsync(Ct);
        }
        catch (Exception e) when (e is SocketException or OperationCanceledException)
        {
            ServerSocket.Stop();
            Cts.Dispose();
            Awake = false;
            OnStopped?.Invoke();
            return;
        }
        ServerSocket.Stop();
        
        // Transition into the connected state
        try
        {
            ClientConnected = true;
            OnConnected?.Invoke();
            await using var stream = ClientSocket.GetStream();
            var outgoingData = ProcessOutgoingChannel(stream);
            var incomingData = ConnectedState(stream);
            await Task.WhenAny(incomingData, outgoingData);
            await Cts.CancelAsync(); // we have this for robustness... if something happens to one of the tasks, cancel all.
            await Task.WhenAll(incomingData, outgoingData);
        }
        finally // all exceptions should be handled by transitions
        {
            // Exit FSM
            ClientSocket.Close();
            Cts.Dispose();
            MessageChannels.Drain();
            ClientConnected = false;
            Awake = false;
            OnDisconnected?.Invoke();  // note: important that this is after fields are set in case the callback invokes start again...
            OnStopped?.Invoke();
        }
    }
    
    private async Task ConnectedState(NetworkStream stream)
    {
        ChannelWriter<Message> channelWriter = MessageChannels.GetIncomingWriter();
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
                    break;
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
        catch (EndOfStreamException){}  // EOF indicates that the TCP connected has been closed.
        catch (OperationCanceledException){}
        catch (Exception e)
        {
            Console.WriteLine($"Unexpected Error in <connected_state.receive_data()>: {e}.");
        }
    }
    
    private async Task ProcessOutgoingChannel(NetworkStream stream)
    {
        try
        {
            ChannelReader<Message> channelReader = MessageChannels.GetOutgoingReader();
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
