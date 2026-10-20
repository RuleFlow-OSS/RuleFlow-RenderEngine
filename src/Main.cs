/*
Note: due to using a controller node design, running some scenes individually may cause errors. Consider using the
autoloaded singleton pattern at some point to make this more versatile.
 */

using System;
using Godot;

namespace RuleFlowRenderEngine;

public partial class Main : Control
{
	// Controllers
	public static Main Instance { get; private set; }  // make this follow a singleton pattern...
	public static Controller Controller { get; } = new();  // make a singleton...
	
	// Access Fields
	public ConnectView ConnectView;
	public GraphView GraphView;
	
	public override void _Ready()
	{
		// Can place Controller.StartServer() here if we need to test without relying on UI...
		Instance = this;  // give global access...
		
		// Grab nodes
		ConnectView = GetNode<ConnectView>("ConnectView");
		GraphView = GetNode<GraphView>("GraphView");
		
		// Set defaults
		OS.LowProcessorUsageMode = true;
		
		// wire up events
		Controller.Server.OnConnected += Controller.Cft(() => SwitchViews("graph"));
		Controller.Server.OnDisconnected += Controller.Cft(() => SwitchViews("connect"));
	}

	public void SwitchViews(string viewName)
	{
		switch (viewName)
		{
			case "connect":
				ConnectView.Visible = true;
				GraphView.Visible = false;
				break;
			case "graph":
				ConnectView.Visible = false;
				GraphView.Visible = true;
				GraphView.ViewStarted();
				break;
		}
	}
}
