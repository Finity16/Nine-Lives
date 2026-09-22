using Godot;

public partial class Projectile : Area2D
{
	[Export] public float Speed = 250f;
	public Vector2 Direction = Vector2.Right;

	public override void _Ready()
	{
	 
		var notifier = GetNode<VisibleOnScreenNotifier2D>("VisibleOnScreenNotifier2D");
		notifier.ScreenExited += OnScreenExited;

		BodyEntered += OnBodyEntered;
	}

	public override void _PhysicsProcess(double delta)
	{
		Position += Direction * Speed * (float)delta;
	}

	private void OnScreenExited()
	{
		QueueFree();
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is Player player)
		{
			player.Die();
		}
	}
}
