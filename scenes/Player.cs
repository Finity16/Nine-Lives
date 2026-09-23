using Godot;
using System.Collections.Generic;

public partial class Player : CharacterBody2D
{
	[Export] public float Speed = 300f;
	[Export] public float DashSpeed = 900f;
	[Export] public float DashDuration = 0.4f;

	[Signal] public delegate void PlayerDiedEventHandler();
	[Signal] public delegate void DashChargesChangedEventHandler(int newCharges);
	[Signal] public delegate void LivesChangedEventHandler(int remaining, int total);

	private bool _isAlive = false;
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

	private List<int> _activeLives = new List<int>();
	private int _totalLivesThisRun = 0;

	public bool HasLife(int id) => _activeLives.Contains(id);

	public void SetActive(bool active)
	{
		_isAlive = active;
		Visible = active;
		if (active)
		{
			Position = GetViewportRect().Size / 2;
			Velocity = Vector2.Zero;
			_isDashing = false;
			DashCharges = 0;

			_activeLives = new List<int>(GameData.EquippedLives);
			if (_activeLives.Count == 0)
			{
				_activeLives.Add(0);
			}
			_totalLivesThisRun = _activeLives.Count;
			EmitSignal(SignalName.LivesChanged, _activeLives.Count, _totalLivesThisRun);
		}
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

			float speedBonus = 0.04f * GameData.SpeedLevel + (HasLife(2) ? 0.20f : 0f);
			float effectiveSpeed = Speed * (1f + speedBonus);
			Velocity = direction * effectiveSpeed;
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
		GameData.TotalDashes += 1;
	}

	public void Die()
	{
		if (!_isAlive || _isDashing) return;

		float dodgeChance = GameData.DodgeLevel * 0.02f + (HasLife(3) ? 0.10f : 0f);
		if (GD.Randf() < dodgeChance)
		{
			GameData.TotalDodges += 1;
			return;
		}

		if (_activeLives.Count > 0)
		{
			_activeLives.RemoveAt(0);
			EmitSignal(SignalName.LivesChanged, _activeLives.Count, _totalLivesThisRun);

			if (_activeLives.Count > 0)
			{
				return;
			}
		}

		_isAlive = false;
		Visible = false;
		EmitSignal(SignalName.PlayerDied);
	}
}
