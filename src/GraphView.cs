/*
Note: currently we use a ForceDirectedGraph plugin... later we will transition to a custom network visualizer
user GPU compute shaders and/or optimized Graph libraries.
*/
using System.Net;
using Godot;
using Godot.Collections;
using RuleFlowRenderEngine.server;

namespace RuleFlowRenderEngine;

public partial class GraphView : Control
{
	public RichTextLabel ConnectedLabel;
	public Button PingClientBtn;
	public Button DisconnectBtn;
	public Node2D ForceDirectedGraph; // really of type ForceDirectedGraph, but we'll just duck type it.
	
	public override void _Ready()
	{
		// Grab nodes
		ConnectedLabel = GetNode<RichTextLabel>("%ConnectedLabel");
		PingClientBtn = GetNode<Button>("%PingClientBtn");
		DisconnectBtn = GetNode<Button>("%DisconnectBtn");
		ForceDirectedGraph = GetNode<Node2D>("%ForceDirectedGraph");
		
		// Connect events
		PingClientBtn.Pressed += () => Main.Controller.PingClient();
		DisconnectBtn.Pressed += () => Main.Controller.Server.Stop(); // we stop it at the server so that we drop the connection, but controller restarts listening...
		Main.Controller.OnPingPongReceived += (msg) => GetNode("/root/EasyNotify")
			.Call("add_notification", new Dictionary()
				{
					{ "title", $"Client: {msg.Op}" },
					{ "message", msg.Args }
				}
			);
		Main.Controller.OnCommandMessageReceived += HandleCommandMessage;
		Main.Controller.OnEventMessageReceived += HandleEventMessage;
	}
	
	/// <summary>
	/// Called whenever this scene is entered.
	/// </summary>
	public void ViewStarted()
	{
		var clientEndpoint = (IPEndPoint)Main.Controller.Server.ClientSocket.Client.RemoteEndPoint;
		ConnectedLabel.Text = clientEndpoint != null ? 
			$"[color=green]Client {clientEndpoint.Address} on port {clientEndpoint.Port}[/color]" : 
			"[color=green]Client Connected[/color]";
	}
	
	public void HandleCommandMessage(CommandMessage msg)
	{
		// Handle command messages.
	}
	
	public void HandleEventMessage(EventMessage msg)
	{
		// Handle adding nodes to the ForceDirectedGraph.
	}
}
