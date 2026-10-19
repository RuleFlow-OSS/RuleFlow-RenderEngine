using System.Threading.Channels;

namespace RuleFlowRenderEngine.server;

public class MessageChannels
{
    /// Incoming messages are stored in the order they are received.
    public Channel<Message> IncomingChannel { get; } = Channel.CreateBounded<Message>(Config.MaxMessageChannelSize);

    /// Outgoing messages are sent in the order they are enqueued.
    public Channel<Message> OutgoingChannel { get; } = Channel.CreateBounded<Message>(Config.MaxMessageChannelSize);
    
    /// Gets the Incoming Reader
    public ChannelReader<Message> GetIncomingReader() => IncomingChannel.Reader;
    /// Gets the Incoming Writer
    public ChannelWriter<Message> GetIncomingWriter() => IncomingChannel.Writer;
    /// Gets the Outgoing Reader
    public ChannelReader<Message> GetOutgoingReader() => OutgoingChannel.Reader;
    /// Gets the Outgoing Writer
    public ChannelWriter<Message> GetOutgoingWriter() => OutgoingChannel.Writer;
    
    /// <summary>
    /// Use this to clear any remaining messages.
    /// </summary>
    public void Drain()
    {
        while (IncomingChannel.Reader.TryRead(out _)) { }
        while (OutgoingChannel.Reader.TryRead(out _)) { }
    }
}
