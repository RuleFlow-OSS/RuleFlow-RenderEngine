/*
Types of command messages:
(Server <> Client) - ping/pong
(Server <- Client) - clear()
(Server <- Client) - regress(n)
TODO: implement a formal Enum class for command message types to clearly define what is allowed.
*/
using Godot;
using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using RuleFlowRenderEngine.server;

namespace RuleFlowRenderEngine;


/// <summary>
/// Handles the main logic of running the server and triggering events.
/// </summary>
public class Controller
{
    // ==== Events ====
    public event Action<CommandMessage> OnPingPongReceived;
    public event Action<CommandMessage> OnCommandMessageReceived;
    public event Action<EventMessage> OnEventMessageReceived;
    
    
    // ==== Object properties ====
    public Server Server { get; } = new();
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
    }
    
    /// <summary>
    /// Wraps an Action so that when it is invoked, it executes safely on Godot's Main Thread.
    /// </summary>
    public static Action Cft(Action action)
    {
        // Return a new action that handles the internal Godot deferral
        return () => Callable.From(action).CallDeferred();
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
                    OnEventMessageReceived?.Invoke(em);
                    break;
                case CommandMessage cm:
                    switch (cm.Op)
                    {
                        case "ping":
                            PingClient(op: "pong", args: "Server Replies Hi!");
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
    public bool PingClient(string op = "ping", string args = "Server Says Hi!")
    {
        return op is "ping" or "pong" ? 
            SendMessage(new CommandMessage() { Op = op, Args = args }) : 
            throw new ArgumentException("Must be 'ping' or 'pong'", nameof(op));
    }
}
