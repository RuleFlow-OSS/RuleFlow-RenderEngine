using Godot;
using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using RuleFlowRenderEngine.server;

namespace RuleFlowRenderEngine;


/// <summary>
/// Provides threading utilities for the app side.
/// The utils provided should be used primarily for communication between Godot's Main Thread and Other Threads.
/// Example uses when be for callbacks subscribed to Server events.
/// </summary>
public static class ThreadUtils
{
    /// <summary>
    /// Wraps an Action so that when it is invoked, it executes safely on Godot's Main Thread.
    /// </summary>
    public static Action Cft(Action action)
    {
        // Return a new action that handles the internal Godot deferral
        return () => Callable.From(action).CallDeferred();
    }

    /// <summary>
    /// Wraps a generic Action with one argument for deferred execution.
    /// </summary>
    public static Action<T> Cft<T>(Action<T> action)
    {
        return (arg) => Callable.From(() => action(arg)).CallDeferred();
    }
}


/// <summary>
/// Handles the business logic...
/// </summary>
public class Controller
{
    // ==== Events ====
    public event Action<CommandMessage> OnPingPongReceived;
    public event Action<CommandMessage> OnCommandMessageReceived;
    public event Action<EventMessage> OnEventMessageReceived;
    
    
    // ==== Object properties ====
    private Server Server { get; } = new();
    private ChannelWriter<Message> ChannelWriter { get; }
    /// when true, the server switches back into its listening state after a connection is dropped.
    private volatile bool _keepServerAlive;

    public Controller()
    {
        ChannelWriter = Server.MessageChannels.GetOutgoingWriter();
        Server.OnConnected += () => Console.WriteLine("Server: Client Connected.");
        Server.OnDisconnected += () => Console.WriteLine("Server: Client Disconnected.");
        Server.OnStopped += OnServerStopped;
        
        // subscribed for logging purposes
        OnPingPongReceived += (msg) => Console.WriteLine($"Ping/Pong: {msg.Op} - {msg.Args}");
        OnEventMessageReceived += (msg) => Console.WriteLine($"Event: {msg.Time}");  // TODO test line
    }
    
    public void StartServer()
    {
        Console.WriteLine("Server: Waiting for connection...");
        _keepServerAlive = true;
        Server.Start();
        _ = MessageHandler(); // fire and forget
    }
    
    public void StopServer()
    {
        _keepServerAlive = false;
        Server.Stop();
    }

    private void OnServerStopped()
    {
        Console.WriteLine("Server: Stopped.\n");
        if (!_keepServerAlive) return;
        Console.WriteLine("Server: Returned to waiting state...");
        Server.Start();
    }
    
    /// <summary>
    /// Main loop for processing all incoming messages. Always running.
    /// </summary>
    private async Task MessageHandler()
    {
        var channelReader = Server.MessageChannels.GetIncomingReader();
        await foreach (var message in channelReader.ReadAllAsync())
        {
            switch (message)
            {
                case EventMessage em:
                    // TODO: connect to internal Causal Network
                    OnEventMessageReceived?.Invoke(em);
                    break;
                case CommandMessage cm:
                    switch (cm.Op)
                    {
                        case "ping":
                            ping_client(op: "pong");
                            OnPingPongReceived?.Invoke(cm);
                            break;
                        case "pong":
                            OnPingPongReceived?.Invoke(cm);
                            break;
                        default:
                            OnCommandMessageReceived?.Invoke(cm);
                            break;
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Use this to send a message to the client.
    /// </summary>
    /// <param name="msg">A Message to send.</param>
    /// <returns>A boolean indicating if successful.</returns>
    public bool SendMessage(Message msg)
    {
        return ChannelWriter.TryWrite(msg);
    }

    /// <summary>
    /// Ping the client.
    /// </summary>
    public bool ping_client(string op = "ping", string args = "Server Says Hi!")
    {
        return op is "ping" or "pong" ? 
            SendMessage(new CommandMessage() { Op = op, Args = args }) : 
            throw new ArgumentException("Must be 'ping' or 'pong'", nameof(op));
    }
}
