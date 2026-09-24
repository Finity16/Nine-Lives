using Godot;
using System.Collections.Generic;

public partial class Player : CharacterBody2D
{
	public static Player Instance;

	[Export] public float Speed = 300f;
	[Export] public float DashSpeed = 900f;
	[Export] public float DashDuration = 0.4f;

	[Signal] public delegate void PlayerDiedEventHandler();
	[Signal] public delegate void DashChargesChangedEventHandler(int newCharges);
	[Signal] public delegate void LivesChangedEventHandler(int remaining, int total);
	[Signal] public delegate void HitEventHandler();

	private bool _isAlive = false;
	private Vector2 _lastDirection = Vector2.Right;
	private bool _isDashing = false;
	private float _dashTimer = 0f;
	private Vector2 _dashDirection;
	private float _sailorBoostTimer = 0f;

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
	public int TotalLivesThisRun = 0;
	public bool DashedThisRun = false;

	private List<(float time, Vector2 pos)> _positionHistory = new List<(float, Vector2)>();
	private float _historyClock = 0f;
	private const float MaxHistoryDuration = 6f;

	public bool HasLife(int id) => _activeLives.Contains(id);

	public override void _Ready()
	{
		Instance = this;
	}

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
			DashedThisRun = false;
			Rotation = 0f;

			_positionHistory.Clear();
			_historyClock = 0f;

			_activeLives = new List<int>(GameData.EquippedLives);
			if (_activeLives.Count == 0)
			{
				_activeLives.Add(0);
			}
			TotalLivesThisRun = _activeLives.Count;
			EmitSignal(SignalName.LivesChanged, _activeLives.Count, TotalLivesThisRun);
		}
	}

	public void TriggerSailorBoost()
	{
		if (HasLife(5))
		{
			_sailorBoostTimer = 0.5f;
		}
	}

	public bool IsDashKillActive() => _isDashing && HasLife(7);

	public Vector2 GetPositionAtDelay(float delay)
	{
		if (_positionHistory.Count == 0) return Position;

		float targetTime = _historyClock - delay;

		for (int i = 0; i < _positionHistory.Count; i++)
		{
			if (_positionHistory[i].time >= targetTime)
			{
				return _positionHistory[i].pos;
			}
		}

		return _positionHistory[0].pos;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!_isAlive) return;

		Vector2 viewportSize = GetViewportRect().Size;

		if (_isDashing)
		{
			_dashTimer -= (float)delta;
			Velocity = _dashDirection * DashSpeed;
			MoveAndSlide();

			Vector2 clamped = new Vector2(
				Mathf.Clamp(Position.X, 0, viewportSize.X),
				Mathf.Clamp(Position.Y, 0, viewportSize.Y)
			);

			if (clamped != Position)
			{
				_isDashing = false;
			}

			Position = clamped;

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

			if (_sailorBoostTimer > 0f)
			{
				_sailorBoostTimer -= (float)delta;
			}

			float speedBonus = 0.04f * GameData.SpeedLevel + (HasLife(2) ? 0.10f : 0f);
			float effectiveSpeed = Speed * (1f + speedBonus);

			if (_sailorBoostTimer > 0f)
			{
				effectiveSpeed *= 1.5f;
			}

			Velocity = direction * effectiveSpeed;
			MoveAndSlide();

			Position = new Vector2(
				Mathf.Clamp(Position.X, 0, viewportSize.X),
				Mathf.Clamp(Position.Y, 0, viewportSize.Y)
			);
		}

		_historyClock += (float)delta;
		_positionHistory.Add((_historyClock, Position));
		while (_positionHistory.Count > 0 && _historyClock - _positionHistory[0].time > MaxHistoryDuration)
		{
			_positionHistory.RemoveAt(0);
		}
	}

	private void StartDash()
	{
		_isDashing = true;
		_dashTimer = DashDuration + (HasLife(7) ? 2f : 0f);
		_dashDirection = _lastDirection;
		DashCharges -= 1;
		GameData.TotalDashes += 1;
		DashedThisRun = true;
	}

	private void PlayDodgeSpin()
	{
		var tween = CreateTween();
		tween.TweenProperty(this, "rotation", Rotation + Mathf.Tau, 0.3f);
		tween.TweenCallback(Callable.From(() => { Rotation = 0f; }));
	}

	public void Die()
	{
		if (!_isAlive || _isDashing) return;

		float dodgeChance = GameData.DodgeLevel * 0.02f + (HasLife(3) ? 0.05f : 0f);
		if (GD.Randf() < dodgeChance)
		{
			GameData.TotalDodges += 1;
			PlayDodgeSpin();
			return;
		}

		EmitSignal(SignalName.Hit);

		if (_activeLives.Count > 0)
		{
			_activeLives.RemoveAt(0);
			EmitSignal(SignalName.LivesChanged, _activeLives.Count, TotalLivesThisRun);

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
