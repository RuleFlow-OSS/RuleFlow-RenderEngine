using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RuleFlowRenderEngine.server;


/// <summary>
/// <c>Message</c> is an ABC that supported polymorphic JSON serialization.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "Type")]
[JsonDerivedType(typeof(EventMessage), typeDiscriminator: "event")]
[JsonDerivedType(typeof(CommandMessage), typeDiscriminator: "command")]
public abstract class Message
{
    public static Message FromBytes(ReadOnlySpan<byte> data)
    {
        return JsonSerializer.Deserialize<Message>(data, Config.JsonSerializeOptions);
    }
    
    public byte[] ToBytes()
    {
        return JsonSerializer.SerializeToUtf8Bytes(this, Config.JsonSerializeOptions);
    }
}

/// <summary>
/// An <c>EventMessage</c> is for RuleFlow event data.
/// </summary>
public class EventMessage : Message
{
    public int Idx { get; set; } = 0;
    public int DistanceToCreation { get; set; } = 0;
    public List<int> Connected { get; set; } = [];
    public List<int> Data { get; set; } = [];
}

/// <summary>
/// A <c>CommandMessage</c> is for controller logic.
/// </summary>
public class CommandMessage : Message
{
    public string Op { get; set; } = "";
    public string Args { get; set; } = "";
    public string Kwargs { get; set; } = "";
}
