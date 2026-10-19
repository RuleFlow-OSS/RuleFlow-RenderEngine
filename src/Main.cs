/*
Types of command messages:
(Server <> Client) - ping/pong
(Server -> Client) - regress(n)
(Server -> Client) - evolve(n)
(Server <- Client) - highlight_cells(n, ...)
(Client -> Server) - highlight_nodes(n, ...)
*/
using Godot;
using  RuleFlowRenderEngine.server;

namespace RuleFlowRenderEngine;

public partial class Main : Control
{
	private Controller Controller { get; } = new();  // logic implementation...
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GetNode<Button>("Button").Pressed += OnButtonPressed;
		Controller.StartServer();
	}
	
	void OnButtonPressed()
	{
		var l = GetNode<RichTextLabel>("label");
		var loopback = NetworkHelpers.LocalHost;
		var lan = NetworkHelpers.GetLocalIpAddress();
		var wan = NetworkHelpers.GetGlobalIpAddress();
		l.AddText($"\n{loopback}");
		l.AddText($"\n{IpAddressSqidsEncoding.Encode(loopback)}");
		l.AddText($"\n{lan}");
		l.AddText($"\n{IpAddressSqidsEncoding.Encode(lan)}");
		l.AddText($"\n{wan}");
		l.AddText($"\n{IpAddressSqidsEncoding.Encode(wan)}");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) { }
}
