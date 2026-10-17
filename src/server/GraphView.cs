using Godot;
using System;
namespace  RuleFlowRenderEngine.server;

public partial class GraphView : Control
{
	int i { get; set; } = 0;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Server t = new Server();
		// instantiate server here...
		GetNode<Button>("Button").Pressed += OnButtonPressed;
	}

	void OnButtonPressed()
	{
		var l = GetNode<RichTextLabel>("label");
		l.AddText("\nTest " + i);
		i++;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
