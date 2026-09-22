using Godot;
using System.Collections.Generic;

public partial class Main : Node2D
{
	[Export] public PackedScene ProjectileScene;

	private RandomNumberGenerator _rng = new RandomNumberGenerator();
	private bool _gameOver = false;
	private Player _player;

	private double _survivalTime = 0.0;
	private int _score = 0;

	private const float BaseSpawnWait = 1.0f;
	private const float MinSpawnWait = 0.45f;
	private const float SpawnRampRate = 0.0275f;

	private const float BaseProjectileSpeed = 200f;
	private const float MaxProjectileSpeed = 500f;
	private const float SpeedRampRate = 8.5f;

	private const float BigSlowIntroTime = 10f;
	private const float SplitterIntroTime = 20f;
	private const float GoliathIntroTime = 30f;

	private const float DashChargeInterval = 15f;
	private double _lastDashChargeTime = 0.0;

	public override void _Ready()
	{
		var spawnTimer = GetNode<Timer>("SpawnTimer");
		spawnTimer.Timeout += OnSpawnTimerTimeout;

		_player = GetNode<Player>("Player");
		_player.PlayerDied += OnPlayerDied;
		_player.DashChargesChanged += OnDashChargesChanged;
	}

	public override void _Process(double delta)
	{
		if (_gameOver)
		{
			if (Input.IsKeyPressed(Key.R))
				GetTree().ReloadCurrentScene();
			return;
		}

		_survivalTime += delta;

		int newScore = (int)_survivalTime;
		if (newScore != _score)
		{
			_score = newScore;
			GetNode<Label>("UI/ScoreLabel").Text = $"Score: {_score}";
		}

		if (_survivalTime - _lastDashChargeTime >= DashChargeInterval)
		{
			_lastDashChargeTime += DashChargeInterval;
			_player.DashCharges += 1;
		}

		float chargeProgress = (float)((_survivalTime - _lastDashChargeTime) / DashChargeInterval) * 100f;
		GetNode<ProgressBar>("UI/DashBar").Value = chargeProgress;

		var spawnTimer = GetNode<Timer>("SpawnTimer");
		float newWaitTime = Mathf.Max(MinSpawnWait, BaseSpawnWait - (float)_survivalTime * SpawnRampRate);
		spawnTimer.WaitTime = newWaitTime;
	}

	private void OnDashChargesChanged(int newCharges)
	{
		GetNode<Label>("UI/DashLabel").Text = $"Dashes: {newCharges}";
	}

	private float GetCurrentProjectileSpeed()
	{
		return Mathf.Min(MaxProjectileSpeed, BaseProjectileSpeed + (float)_survivalTime * SpeedRampRate);
	}

private ProjectileType ChooseProjectileType()
{
	var options = new List<ProjectileType> { ProjectileType.Normal, ProjectileType.Normal, ProjectileType.Normal };

	if (_survivalTime >= BigSlowIntroTime)
		options.Add(ProjectileType.BigSlow);

	if (_survivalTime >= SplitterIntroTime)
		options.Add(ProjectileType.Splitter);

	if (_survivalTime >= GoliathIntroTime)
		options.Add(ProjectileType.Goliath);

	return options[_rng.RandiRange(0, options.Count - 1)];
}

	private void OnPlayerDied()
	{
		_gameOver = true;
		GetNode<Timer>("SpawnTimer").Stop();
		GetNode<Label>("UI/GameOverLabel").Visible = true;
		GetNode<Label>("UI/GameOverLabel").Text = $"Game Over - Survived {_score}s - Press R to Restart";
	}

	private void OnSpawnTimerTimeout()
	{
		if (_gameOver || ProjectileScene == null) return;

		var projectile = ProjectileScene.Instantiate<Projectile>();
		var type = ChooseProjectileType();

		Vector2 viewportSize = GetViewportRect().Size;
		Vector2 spawnPos;
		Vector2 direction;

		int edge = _rng.RandiRange(0, 3);
		switch (edge)
		{
			case 0:
				spawnPos = new Vector2(_rng.RandfRange(0, viewportSize.X), -20);
				direction = Vector2.Down;
				break;
			case 1:
				spawnPos = new Vector2(_rng.RandfRange(0, viewportSize.X), viewportSize.Y + 20);
				direction = Vector2.Up;
				break;
			case 2:
				spawnPos = new Vector2(-20, _rng.RandfRange(0, viewportSize.Y));
				direction = Vector2.Right;
				break;
			default:
				spawnPos = new Vector2(viewportSize.X + 20, _rng.RandfRange(0, viewportSize.Y));
				direction = Vector2.Left;
				break;
		}

		float baseSpeed = GetCurrentProjectileSpeed();
		float speedMultiplier = 1f;
		float scale = 1f;

		if (type == ProjectileType.BigSlow)
		{
			speedMultiplier = 0.4f;
			scale = 2.5f;
		}
		else if (type == ProjectileType.Splitter)
		{
			speedMultiplier = 0.7f;
			scale = 2f;
		}
				else if (type == ProjectileType.Goliath)
		{
			speedMultiplier = 0.15f;
			scale = 4f;
		}
				Color tint = Colors.White;
		if (type == ProjectileType.Normal) tint = Colors.Red;
		else if (type == ProjectileType.BigSlow) tint = Colors.Orange;
		else if (type == ProjectileType.Splitter) tint = Colors.Purple;
		else if (type == ProjectileType.Goliath) tint = Colors.DarkRed;

		projectile.Modulate = tint;

		projectile.Position = spawnPos;
		projectile.Direction = direction;
		projectile.Speed = baseSpeed * speedMultiplier;
		projectile.Type = type;
		projectile.ProjectileScene = ProjectileScene;
		projectile.Scale = new Vector2(scale, scale);

		AddChild(projectile);
	}
}
