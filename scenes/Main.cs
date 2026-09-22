using Godot;

public partial class Main : Node2D
{
	[Export] public PackedScene ProjectileScene;

	private RandomNumberGenerator _rng = new RandomNumberGenerator();
	private bool _gameOver = false;

	public override void _Ready()
	{
		var spawnTimer = GetNode<Timer>("SpawnTimer");
		spawnTimer.Timeout += OnSpawnTimerTimeout;

		var player = GetNode<Player>("Player");
		player.PlayerDied += OnPlayerDied;
	}

	public override void _Process(double delta)
	{
		if (_gameOver && Input.IsKeyPressed(Key.R))
		{
			GetTree().ReloadCurrentScene();
		}
	}

	private void OnPlayerDied()
	{
		_gameOver = true;
		GetNode<Timer>("SpawnTimer").Stop();
		GetNode<Label>("UI/GameOverLabel").Visible = true;
	}

	private void OnSpawnTimerTimeout()
	{
		if (_gameOver || ProjectileScene == null) return;

		var projectile = ProjectileScene.Instantiate<Projectile>();

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

		projectile.Position = spawnPos;
		projectile.Direction = direction;
		AddChild(projectile);
	}
}
