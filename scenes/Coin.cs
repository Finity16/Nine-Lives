using Godot;

public partial class Coin : Area2D
{
	[Signal] public delegate void CollectedEventHandler();
	private bool _collected = false;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is Player && !_collected)
		{
			_collected = true;
			EmitSignal(SignalName.Collected);
			QueueFree();
		}
	}
}
