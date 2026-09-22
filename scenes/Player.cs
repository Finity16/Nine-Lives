using Godot;

public partial class Player : CharacterBody2D
{
	[Export] public float Speed = 300f;
	[Signal] public delegate void PlayerDiedEventHandler();

	private bool _isAlive = true;

	public override void _PhysicsProcess(double delta)
	{
		if (!_isAlive) return;

		Vector2 direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		Velocity = direction * Speed;
		MoveAndSlide();

		Vector2 viewportSize = GetViewportRect().Size;
		Position = new Vector2(
			Mathf.Clamp(Position.X, 0, viewportSize.X),
			Mathf.Clamp(Position.Y, 0, viewportSize.Y)
		);
	}

	public void Die()
	{
		if (!_isAlive) return;

		_isAlive = false;
		Visible = false;
		EmitSignal(SignalName.PlayerDied);
	}
}
