using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace RuleFlowRenderEngine.server;

public class Server
{
    // TODO: We need to have a method for sending data (it sends in its own thread)
    // TODO: all received data should be processed using the Request handler and pushed to the FIFO ConcurrentQueue.
    const int Port = 48000;
    public bool Awake { get; private set; } = false;
    public bool ClientConnected { get; private set; } = false;  // tells us if a client has been connected.
    private TcpListener socket = null;
    private TcpClient clientSocket = null;
    private CancellationTokenSource cts = null;
    private CancellationToken ct;
    
    public Server()
    {
        socket = new TcpListener(IPAddress.Any, Port);
    }

    public void Start()  // should enter the states into a new thread
    {
        cts = new CancellationTokenSource();
        ct = cts.Token;
        Task.Run(WaitingState, ct);
    } 

    public void Stop()
    {
        cts.Cancel();
        Awake = false;
    }
    
    // ======== FSM States (some transition code is defined within state) ========
    void WaitingState()
    {
        Awake = true;
        socket.Start();
        try
        {
            // we wait here until there is a pending connection.
            clientSocket = socket.AcceptTcpClient();
        }
        catch (SocketException)
        {
            Awake = false;
            return;
        }
        
        // transition into the connected state
        ClientConnected = true;
        ConnectedState();
        socket.Stop();
    }

    void ConnectedState()
    {
        
    }
}


// public static class TcpClientExt
// {
//     // this is HTTP 1.0 specific for requests only...
//     public static async Task<string> ReadRequestString(this TcpClient client)
//     {
//         var stream = client.GetStream();
//         var buffer = new byte[4096];
//         var sb = new StringBuilder();
//
//         while (true)
//         {
//             var len = await stream.ReadAsync(buffer, 0, 4096);
//             if (len == 0) // the connection was closed somehow
//                 break;
//             var chunk = Encoding.ASCII.GetString(buffer, 0, len);
//             sb.Append(chunk);
//                 
//             // we reached the end of the HTTP request or null char
//             if (chunk.Contains("\r\n") || chunk.Contains('\0'))
//                 break;
//         }
//             
//         return sb.ToString();
//     }
//         
//     public static async Task WriteResponseString(this TcpClient client, string content)
//     {
//         var stream = client.GetStream();
//         byte[] buffer = Encoding.ASCII.GetBytes(content);
//         await stream.WriteAsync(buffer);
//         await stream.FlushAsync();
//     }
// }
