using Godot;

public partial class Player : CharacterBody2D
{
	[Export] public float Speed = 300f;
	[Export] public float DashSpeed = 900f;
	[Export] public float DashDuration = 0.4f;

	[Signal] public delegate void PlayerDiedEventHandler();
	[Signal] public delegate void DashChargesChangedEventHandler(int newCharges);

	private bool _isAlive = true;
	private Vector2 _lastDirection = Vector2.Right;
	private bool _isDashing = false;
	private float _dashTimer = 0f;
	private Vector2 _dashDirection;

	private int _dashCharges = 0;
	public int DashCharges
	{
		get => _dashCharges;
		set
		{
			_dashCharges = value;
			EmitSignal(SignalName.DashChargesChanged, _dashCharges);
		}
	}

	public override void _Ready()
	{
		Position = GetViewportRect().Size / 2;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!_isAlive) return;

		if (_isDashing)
		{
			_dashTimer -= (float)delta;
			Velocity = _dashDirection * DashSpeed;
			MoveAndSlide();

			if (_dashTimer <= 0f)
			{
				_isDashing = false;
			}
		}
		else
		{
			Vector2 direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");

			if (direction != Vector2.Zero)
			{
				_lastDirection = direction.Normalized();
			}

			if (Input.IsActionJustPressed("dash") && DashCharges > 0)
			{
				StartDash();
			}

			Velocity = direction * Speed;
			MoveAndSlide();
		}

		Vector2 viewportSize = GetViewportRect().Size;
		Position = new Vector2(
			Mathf.Clamp(Position.X, 0, viewportSize.X),
			Mathf.Clamp(Position.Y, 0, viewportSize.Y)
		);
	}

	private void StartDash()
	{
		_isDashing = true;
		_dashTimer = DashDuration;
		_dashDirection = _lastDirection;
		DashCharges -= 1;
	}

	public void Die()
	{
		if (!_isAlive || _isDashing) return;

		_isAlive = false;
		Visible = false;
		EmitSignal(SignalName.PlayerDied);
	}
}
