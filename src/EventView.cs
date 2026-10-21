/*
Note: currently we use a ForceDirectedGraph plugin... later we will transition to a custom network visualizer
using GPU compute shaders and/or optimized Graph libraries.
*/

using System.Linq;
using System.Net;
using Godot;
using Godot.Collections;
using RuleFlowRenderEngine.server;

namespace RuleFlowRenderEngine;

public partial class EventView : Control
{
	// Preload the force directed node and spring classes
	public static readonly GDScript FdgNodeClass = GD.Load<GDScript>("res://addons/force_directed_graph/fdg_node.gd"); // extends Node2D
	public static readonly GDScript FdgSpringClass = GD.Load<GDScript>("res://addons/force_directed_graph/fdg_spring.gd"); // extends Line2D

	// Causal Network node structure
	public Dictionary<int, Node2D> EventNodes = new();
	
	// Access fields
	public Node2D ForceDirectedGraph; // really of type ForceDirectedGraph, but we'll just duck type it.
	public ItemList MsgLog;
	
	public override void _Ready()
	{
		// Grab nodes
		ForceDirectedGraph = GetNode<Node2D>("%ForceDirectedGraph");
		MsgLog = GetNode<ItemList>("%MsgLog");
		
		// Connect events
		GetNode<Button>("%ClearLogBtn").Pressed += () => MsgLog.Clear();
		GetNode<Button>("%PingClientBtn").Pressed += () => Main.Controller.PingClient();
		GetNode<Button>("%ClearNodesBtn").Pressed += ClearEventNodes;
		GetNode<CheckBox>("%ForcesCheckBox").Toggled += (b) => ForceDirectedGraph.Set("is_active", b);
		GetNode<Button>("%DisconnectBtn").Pressed += () => Main.Controller.Server.Stop(); // we stop it at the server so that we drop the connection, but controller restarts listening...
		Main.Controller.OnPingPongReceived += msg =>
		{
			GetNode("/root/EasyNotify").Call("add_notification", new Dictionary()
				{
					{ "title", $"Client: {msg.Op}" },
					{ "message", msg.Args }
				}
			);
			MsgLog.AddItem($"{msg.Op.Capitalize()}(\"{msg.Args}\")");
		};
		Main.Controller.OnCommandMessageReceived += HandleCommandMessage;
		Main.Controller.OnEventMessageReceived += HandleEventMessage;
	}
	
	/// <summary>
	/// Called whenever this scene is entered.
	/// </summary>
	public void ViewStarted()
	{
		var clientEndpoint = (IPEndPoint)Main.Controller.Server.ClientSocket.Client.RemoteEndPoint;
		GetNode<RichTextLabel>("%ConnectedLabel").Text = clientEndpoint != null ? 
			$"[color=green]Client {clientEndpoint.Address} on port {clientEndpoint.Port}[/color]" : 
			"[color=green]Client Connected[/color]";
	}
	
	public void HandleCommandMessage(CommandMessage msg)
	{
		MsgLog.AddItem($"\nCmdMsg(Op: {msg.Op}, Args: {msg.Args})");
		switch (msg.Op)
		{
			case "clear":
				ClearEventNodes();
				break;
			case "regress":
				var idx = EventNodes.Keys.Max();
				var n = msg.Args.ToInt();
				for (var i = idx; i > idx - n; i--)
					RemoveEventNode(i);
				break;
		}
	}

	private Node2D _lastSpawnedNode = null;
	private readonly Vector2 _growthDirection = new Vector2(80f, 0f); // Grows left-to-right
	private readonly LabelSettings _nodeLabelSettings = new LabelSettings
	{
		FontColor = Colors.Black,
		OutlineColor = Colors.Cyan,
		OutlineSize = 2,
		FontSize = 24
	};
	private readonly Vector2 _nodeLabelMinSize = new Vector2(32, 32);
	
	public void HandleEventMessage(EventMessage msg)
	{
		Node2D n = CreateEventNode(msg.Idx, $"Causal Distance: {msg.DistanceToCreation}");

		foreach (var i in msg.Connected.Distinct())
		{
			if (!EventNodes.ContainsKey(i))
				CreateEventNode(i, "Edge Node");
			var e = (Line2D)FdgSpringClass.New();
			e.Set("node_start", EventNodes[i]);
			e.Set("node_end", n);
			n.AddChild(e);
		}
		
		// output to logger
		MsgLog.AddItem($"\nEventMsg(Idx: {msg.Idx}, " +
		               $"CausalDistance: {msg.DistanceToCreation}, " +
		               $"Connected: [{string.Join(", ", msg.Connected)}])");
	}

	public Node2D CreateEventNode(int idx, string tooltip)
	{
		// Configure new node
		var n = (Node2D)FdgNodeClass.New();
		EventNodes[idx] = n;

		// --- Directional Spawn Position ---
		if (_lastSpawnedNode != null && IsInstanceValid(_lastSpawnedNode))
		{
			var jitterY = (float)GD.RandRange(-30.0, 30.0);
			var jitterX = (float)GD.RandRange(-10.0, 10.0);
			n.Position = _lastSpawnedNode.Position + _growthDirection + new Vector2(jitterX, jitterY);
		}
		else
		{
			n.Position = Vector2.Zero; // First node starts at origin
		}
		_lastSpawnedNode = n;
		
		// ----------------------------------
		var label = new Label
		{
			Text = idx.ToString(),
			MouseFilter = MouseFilterEnum.Stop,
			TooltipText = tooltip,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			CustomMinimumSize = _nodeLabelMinSize,
			ZIndex = 1,
			LabelSettings = _nodeLabelSettings
		};

		label.Resized += () => label.Position = -label.Size / 2f;

		var isDragging = false;
		var dragOffset = Vector2.Zero;
		label.GuiInput += (@event) =>
		{
			if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
			{
				isDragging = mb.Pressed;
				dragOffset = n.GlobalPosition - n.GetGlobalMousePosition();
				n.Set("pinned_down", isDragging);
				label.AcceptEvent();
			}
			else if (@event is InputEventMouseMotion && isDragging)
			{
				n.GlobalPosition = n.GetGlobalMousePosition() + dragOffset;
				n.Set("velocity", Vector2.Zero);
				label.AcceptEvent();
			}
		};

		n.AddChild(label);
		ForceDirectedGraph.AddChild(n);
		return n;
	}
	
	public bool RemoveEventNode(int idx)
	{
		if (!EventNodes.TryGetValue(idx, out var n))
			return false;
		foreach (var spring in n.GetChildren())
		{
			n.RemoveChild(spring);
			spring.QueueFree();
		}
		ForceDirectedGraph.RemoveChild(n);
		n.QueueFree();
		EventNodes.Remove(idx);
		_lastSpawnedNode = EventNodes[EventNodes.Keys.Max()];
		return true;
	}
	
	public void ClearEventNodes()
	{
		foreach (var node in EventNodes.Values)  // we don't want to delete all children of ForceDirectedGraph (plugin uses child some nodes...)
		{
			foreach (var spring in node.GetChildren())
			{
				node.RemoveChild(spring);
				spring.QueueFree();
			}
			ForceDirectedGraph.RemoveChild(node);
			node.QueueFree();
		}
		EventNodes.Clear();
	}
}
