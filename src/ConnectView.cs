using Godot;
using RuleFlowRenderEngine.server;

namespace RuleFlowRenderEngine;

public partial class ConnectView : Control
{
	// Access Fields
	public RichTextLabel ConnectionCode;
	public ProgressBar WaitingIndicator;
	public OptionButton IpSelection;
	public Button StartStopServerBtn;
	
	public override void _Ready()
	{
		// Grab nodes
		ConnectionCode = GetNode<RichTextLabel>("%ConnectionCode");
		WaitingIndicator = GetNode<ProgressBar>("%WaitingIndicator");
		IpSelection = GetNode<OptionButton>("%IpSelection");
		StartStopServerBtn = GetNode<Button>("%StartStopServerBtn");
		
		// Connect events
		StartStopServerBtn.Pressed += OnStartStopServerBtnPressed;
		IpSelection.ItemSelected += OnIpAddressSelected;
		
		// set default state
		OnIpAddressSelected(0);
	}

	private void OnIpAddressSelected(long op)
	{
		try
		{
			ConnectionCode.Text = op switch
			{
				1 => $"Connection Code: [color=lightblue]{
					IpAddressSqidsEncoding.Encode(NetworkHelpers.GetLocalIpAddress())
				}[/color]",
				2 => $"Connection Code: [color=lightblue]{
					IpAddressSqidsEncoding.Encode(NetworkHelpers.GetGlobalIpAddress())
				}[/color]",
				_ => $"Connection Code: [color=lightblue]{
					IpAddressSqidsEncoding.Encode(NetworkHelpers.LocalHost)
				}[/color]"
			};
		}
		catch
		{
			ConnectionCode.Text = "Please select a working network level...";
		}
	}
	
	private void OnStartStopServerBtnPressed()
	{
		if (Main.Controller.Server.Awake)
		{
			Main.Controller.StopServer();
			StartStopServerBtn.Text = "Start Server";
			WaitingIndicator.Visible = false;
			return;
		}
		Main.Controller.StartServer();
		StartStopServerBtn.Text = "🛑 Stop Server";
		WaitingIndicator.Visible = true;
	}
}
