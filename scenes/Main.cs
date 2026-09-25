using Godot;
using System.Collections.Generic;

public partial class Main : Node2D
{
	[Export] public PackedScene ProjectileScene;
	[Export] public PackedScene CoinScene;

	private RandomNumberGenerator _rng = new RandomNumberGenerator();
	private Player _player;

	private enum GameState { Menu, Playing, GameOver, Upgrades, Lives }
	private GameState _state = GameState.Menu;

	private double _survivalTime = 0.0;
	private int _score = 0;

	private const float BaseSpawnWait = 1.0f;
	private const float MinSpawnWait = 0.45f;
	private const float SpawnRampRate = 0.0275f;

	private const float BaseProjectileSpeed = 250f;
	private const float MaxProjectileSpeed = 420f;
	private const float SpeedRampRate = 8.5f;

	private const float BigSlowIntroTime = 10f;
	private const float SplitterIntroTime = 20f;
	private const float GoliathIntroTime = 30f;

	private double _lastDashChargeTime = 0.0;
	private float DashChargeInterval => Mathf.Max(5f, 15f - GameData.DashLevel - (_player.HasLife(4) ? 5f : 0f));

	private List<Coin> _activeCoins = new List<Coin>();
	private bool[] _wasUnlockedAtRunStart = new bool[9];

	private bool _shadow50Spawned = false;
	private bool _shadow75Spawned = false;
	private bool _shadow100Spawned = false;

	private static readonly int[] SpeedCosts = { 10, 20, 35, 55, 80 };
	private static readonly int[] DodgeCosts = { 15, 30, 50, 75, 110 };
	private static readonly int[] DashCosts = { 20, 40, 65, 95, 140 };

	public override void _Ready()
	{
		var spawnTimer = GetNode<Timer>("SpawnTimer");
		spawnTimer.Timeout += OnSpawnTimerTimeout;
		spawnTimer.Stop();

		var coinTimer = GetNode<Timer>("CoinSpawnTimer");
		coinTimer.WaitTime = 2.0f;
		coinTimer.Timeout += OnCoinTimerTimeout;
		coinTimer.Stop();

		_player = GetNode<Player>("Player");
		_player.PlayerDied += OnPlayerDied;
		_player.DashChargesChanged += OnDashChargesChanged;
		_player.LivesChanged += OnLivesChanged;
		_player.Hit += OnPlayerHit;

		GetNode<Button>("UI/UpgradesButton").Pressed += ShowUpgrades;
		GetNode<Button>("UI/UpgradesCenterContainer/UpgradesPanel/BackButton").Pressed += ShowMenu;
		GetNode<Button>("UI/UpgradesCenterContainer/UpgradesPanel/SpeedBuyButton").Pressed += BuySpeed;
		GetNode<Button>("UI/UpgradesCenterContainer/UpgradesPanel/DodgeBuyButton").Pressed += BuyDodge;
		GetNode<Button>("UI/UpgradesCenterContainer/UpgradesPanel/DashBuyButton").Pressed += BuyDash;

		GetNode<Button>("UI/LivesButton").Pressed += ShowLives;
		GetNode<Button>("UI/LivesCenterContainer/LivesPanel/LivesBackButton").Pressed += ShowMenu;

		for (int i = 0; i < 9; i++)
		{
			int capturedId = i;
			GetNode<Button>($"UI/LivesCenterContainer/LivesPanel/LivesGrid/Life{i}Button").Pressed += () => OnLifeButtonPressed(capturedId);
		}

		GetNode<Label>("UI/CoinLabel").Text = $"Coins: {GameData.Coins}";

		ShowMenu();
	}

	public override void _Process(double delta)
	{
		if (OS.IsDebugBuild() && Input.IsActionJustPressed("cheat_coins"))
		{
			GameData.Coins += 100;
			GameData.TotalCoinsCollected += 100;
			GetNode<Label>("UI/CoinLabel").Text = $"Coins: {GameData.Coins}";
			if (_state == GameState.Upgrades) RefreshUpgradesUI();
			if (_state == GameState.Lives) RefreshLivesUI();
		}

		if (OS.IsDebugBuild() && Input.IsActionJustPressed("cheat_unlock_lives"))
		{
			GameData.ForceUnlockAll = true;
			if (_state == GameState.Lives) RefreshLivesUI();
		}

		if (_state == GameState.Menu)
		{
			if (Input.IsActionJustPressed("ui_accept"))
			{
				StartGame();
			}
			return;
		}

		if (_state == GameState.Upgrades || _state == GameState.Lives)
		{
			return;
		}

		if (_state == GameState.GameOver)
		{
			if (Input.IsActionJustPressed("restart"))
			{
				StartGame();
			}
			else if (Input.IsActionJustPressed("ui_accept"))
			{
				ShowMenu();
			}
			return;
		}

		_survivalTime += delta;

		int newScore = (int)_survivalTime;
		if (newScore != _score)
		{
			_score = newScore;
			GetNode<Label>("UI/ScoreLabel").Text = $"Score: {_score}";
		}

		if (_score >= 50 && !_shadow50Spawned)
		{
			SpawnShadow(1.5f);
			_shadow50Spawned = true;
		}
		if (_score >= 75 && !_shadow75Spawned)
		{
			SpawnShadow(3f);
			_shadow75Spawned = true;
		}
		if (_score >= 100 && !_shadow100Spawned)
		{
			SpawnShadow(5f);
			_shadow100Spawned = true;
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

	private void OnPlayerHit()
	{
		var flash = GetNode<ColorRect>("UI/HitFlash");
		flash.Color = new Color(1f, 0f, 0f, 0.4f);
		var tween = CreateTween();
		tween.TweenProperty(flash, "color:a", 0f, 0.4f);
	}

	private void ClearProjectiles()
	{
		foreach (Node child in GetChildren())
		{
			if (child is Projectile)
			{
				child.QueueFree();
			}
		}
	}

	private void ClearCoins()
	{
		foreach (var coin in _activeCoins)
		{
			if (GodotObject.IsInstanceValid(coin))
			{
				coin.QueueFree();
			}
		}
		_activeCoins.Clear();
	}

	private void HideAllPanels()
	{
		GetNode<Label>("UI/ScoreLabel").Visible = false;
		GetNode<ProgressBar>("UI/DashBar").Visible = false;
		GetNode<Label>("UI/DashLabel").Visible = false;
		GetNode<Label>("UI/LivesIndicatorLabel").Visible = false;
		GetNode<Control>("UI/UpgradesCenterContainer").Visible = false;
		GetNode<Control>("UI/LivesCenterContainer").Visible = false;
		GetNode<Button>("UI/UpgradesButton").Visible = false;
		GetNode<Button>("UI/LivesButton").Visible = false;
		GetNode<Label>("UI/GameOverLabel").Visible = false;
	}

	private void ShowMenu()
	{
		_state = GameState.Menu;

		GetNode<Timer>("SpawnTimer").Stop();
		GetNode<Timer>("CoinSpawnTimer").Stop();
		ClearProjectiles();
		ClearCoins();

		HideAllPanels();
		GetNode<Button>("UI/UpgradesButton").Visible = true;
		GetNode<Button>("UI/LivesButton").Visible = true;

		var gameOverLabel = GetNode<Label>("UI/GameOverLabel");
		gameOverLabel.Visible = true;
		gameOverLabel.Text = $"NINE LIVES\nHigh Score: {GameData.HighScore}s\n\nPress SPACE to Start";

		GetNode<Label>("UI/CoinLabel").Text = $"Coins: {GameData.Coins}";

		_player.SetActive(false);
	}

	private void StartGame()
	{
		_state = GameState.Playing;

		HideAllPanels();
		GetNode<Label>("UI/ScoreLabel").Visible = true;
		GetNode<ProgressBar>("UI/DashBar").Visible = true;
		GetNode<Label>("UI/DashLabel").Visible = true;
		GetNode<Label>("UI/LivesIndicatorLabel").Visible = true;

		for (int i = 0; i < 9; i++)
		{
			_wasUnlockedAtRunStart[i] = GameData.IsLifeUnlocked(i);
		}

		_survivalTime = 0.0;
		_score = 0;
		_lastDashChargeTime = 0.0;
		_shadow50Spawned = false;
		_shadow75Spawned = false;
		_shadow100Spawned = false;

		GetNode<Label>("UI/ScoreLabel").Text = "Score: 0";
		GetNode<Label>("UI/DashLabel").Text = "Dashes: 0";
		GetNode<ProgressBar>("UI/DashBar").Value = 0;

		ClearProjectiles();
		ClearCoins();

		_player.SetActive(true);

		var spawnTimer = GetNode<Timer>("SpawnTimer");
		spawnTimer.WaitTime = BaseSpawnWait;
		spawnTimer.Start();

		GetNode<Timer>("CoinSpawnTimer").Start();
	}

	private void ShowUpgrades()
	{
		_state = GameState.Upgrades;
		HideAllPanels();
		GetNode<Control>("UI/UpgradesCenterContainer").Visible = true;
		RefreshUpgradesUI();
	}

	private void ShowLives()
	{
		_state = GameState.Lives;
		HideAllPanels();
		GetNode<Control>("UI/LivesCenterContainer").Visible = true;
		RefreshLivesUI();
	}

	private void OnLifeButtonPressed(int id)
	{
		if (!GameData.IsLifeUnlocked(id)) return;

		if (GameData.EquippedLives.Contains(id))
		{
			GameData.EquippedLives.Remove(id);
		}
		else
		{
			if (GameData.EquippedLives.Count >= 3) return;
			GameData.EquippedLives.Add(id);
		}

		RefreshLivesUI();
	}

	private void RefreshLivesUI()
	{
		for (int i = 0; i < 9; i++)
		{
			var button = GetNode<Button>($"UI/LivesCenterContainer/LivesPanel/LivesGrid/Life{i}Button");
			bool unlocked = GameData.IsLifeUnlocked(i);
			bool equipped = GameData.EquippedLives.Contains(i);

			if (!unlocked)
			{
				button.Text = $"???\n{GameData.GetUnlockCondition(i)}";
				button.Disabled = true;
			}
			else
			{
				string prefix = equipped ? "[EQUIPPED] " : "";
				button.Text = $"{prefix}{GameData.GetLifeName(i)}\n{GameData.GetPassiveDescription(i)}";
				button.Disabled = false;
			}
		}

		var orderLabel = GetNode<Label>("UI/LivesCenterContainer/LivesPanel/EquippedOrderLabel");
		if (GameData.EquippedLives.Count == 0)
		{
			orderLabel.Text = "No lives equipped!";
		}
		else
		{
			var names = new List<string>();
			foreach (var id in GameData.EquippedLives)
			{
				names.Add(GameData.GetLifeName(id));
			}
			orderLabel.Text = "Order (lost first -> last): " + string.Join(" -> ", names);
		}
	}

	private void RefreshUpgradesUI()
	{
		GetNode<Label>("UI/CoinLabel").Text = $"Coins: {GameData.Coins}";

		var speedLabel = GetNode<Label>("UI/UpgradesCenterContainer/UpgradesPanel/SpeedLabel");
		var speedButton = GetNode<Button>("UI/UpgradesCenterContainer/UpgradesPanel/SpeedBuyButton");
		if (GameData.SpeedLevel >= 5)
		{
			speedLabel.Text = $"Speed: +{GameData.SpeedLevel * 4}% (MAX)";
			speedButton.Text = "MAXED";
			speedButton.Disabled = true;
		}
		else
		{
			int cost = SpeedCosts[GameData.SpeedLevel];
			speedLabel.Text = $"Speed: +{GameData.SpeedLevel * 4}%  ->  +{(GameData.SpeedLevel + 1) * 4}%";
			speedButton.Text = $"Buy ({cost}c)";
			speedButton.Disabled = GameData.Coins < cost;
		}

		var dodgeLabel = GetNode<Label>("UI/UpgradesCenterContainer/UpgradesPanel/DodgeLabel");
		var dodgeButton = GetNode<Button>("UI/UpgradesCenterContainer/UpgradesPanel/DodgeBuyButton");
		if (GameData.DodgeLevel >= 5)
		{
			dodgeLabel.Text = $"Dodge Chance: +{GameData.DodgeLevel * 2}% (MAX)";
			dodgeButton.Text = "MAXED";
			dodgeButton.Disabled = true;
		}
		else
		{
			int cost = DodgeCosts[GameData.DodgeLevel];
			dodgeLabel.Text = $"Dodge Chance: +{GameData.DodgeLevel * 2}%  ->  +{(GameData.DodgeLevel + 1) * 2}%";
			dodgeButton.Text = $"Buy ({cost}c)";
			dodgeButton.Disabled = GameData.Coins < cost;
		}

		var dashLabel = GetNode<Label>("UI/UpgradesCenterContainer/UpgradesPanel/DashUpgradeLabel");
		var dashButton = GetNode<Button>("UI/UpgradesCenterContainer/UpgradesPanel/DashBuyButton");
		if (GameData.DashLevel >= 5)
		{
			dashLabel.Text = $"Dash Charge Speed: {15 - GameData.DashLevel}s (MAX)";
			dashButton.Text = "MAXED";
			dashButton.Disabled = true;
		}
		else
		{
			int cost = DashCosts[GameData.DashLevel];
			dashLabel.Text = $"Dash Charge Speed: {15 - GameData.DashLevel}s  ->  {15 - (GameData.DashLevel + 1)}s";
			dashButton.Text = $"Buy ({cost}c)";
			dashButton.Disabled = GameData.Coins < cost;
		}
	}

	private void BuySpeed()
	{
		if (GameData.SpeedLevel >= 5) return;
		int cost = SpeedCosts[GameData.SpeedLevel];
		if (GameData.Coins < cost) return;
		GameData.Coins -= cost;
		GameData.SpeedLevel += 1;
		RefreshUpgradesUI();
	}

	private void BuyDodge()
	{
		if (GameData.DodgeLevel >= 5) return;
		int cost = DodgeCosts[GameData.DodgeLevel];
		if (GameData.Coins < cost) return;
		GameData.Coins -= cost;
		GameData.DodgeLevel += 1;
		RefreshUpgradesUI();
	}

	private void BuyDash()
	{
		if (GameData.DashLevel >= 5) return;
		int cost = DashCosts[GameData.DashLevel];
		if (GameData.Coins < cost) return;
		GameData.Coins -= cost;
		GameData.DashLevel += 1;
		RefreshUpgradesUI();
	}

	private void OnDashChargesChanged(int newCharges)
	{
		GetNode<Label>("UI/DashLabel").Text = $"Dashes: {newCharges}";
	}

	private void OnLivesChanged(int remaining, int total)
	{
		GetNode<Label>("UI/LivesIndicatorLabel").Text = $"Lives: {remaining}/{total}";
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
		_state = GameState.GameOver;

		GetNode<Timer>("SpawnTimer").Stop();
		GetNode<Timer>("CoinSpawnTimer").Stop();

		if (_score > GameData.HighScore)
		{
			GameData.HighScore = _score;
		}

		if (GameData.HighScore >= 50 && _score >= 45 && !_player.DashedThisRun)
		{
			GameData.MonkUnlocked = true;
		}

		if (GameData.HighScore >= 50 && _score >= 45 && _player.TotalLivesThisRun == 1)
		{
			GameData.MonarchUnlocked = true;
		}

		var newlyUnlocked = new List<string>();
		for (int i = 0; i < 9; i++)
		{
			if (!_wasUnlockedAtRunStart[i] && GameData.IsLifeUnlocked(i))
			{
				newlyUnlocked.Add(GameData.GetLifeName(i));
			}
		}

		string unlockMessage = newlyUnlocked.Count > 0
			? "\n\nUnlocked: " + string.Join(", ", newlyUnlocked) + "!"
			: "";

		var gameOverLabel = GetNode<Label>("UI/GameOverLabel");
		gameOverLabel.Visible = true;
		gameOverLabel.Text = $"Game Over - Survived {_score}s\nHigh Score: {GameData.HighScore}s{unlockMessage}\n\nPress R to restart, SPACE for Menu";
	}

	private void OnSpawnTimerTimeout()
	{
		if (_state != GameState.Playing || ProjectileScene == null) return;

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

		if (type == ProjectileType.BigSlow)
		{
			speedMultiplier = 0.4f;
		}
		else if (type == ProjectileType.Splitter)
		{
			speedMultiplier = 0.7f;
		}
		else if (type == ProjectileType.Goliath)
		{
			speedMultiplier = 0.15f;
		}

		projectile.Type = type;
		projectile.Position = spawnPos;
		projectile.Direction = direction;
		projectile.Speed = baseSpeed * speedMultiplier;
		projectile.ProjectileScene = ProjectileScene;

		AddChild(projectile);
	}

	private void SpawnShadow(float delay)
	{
		if (ProjectileScene == null) return;

		var shadow = ProjectileScene.Instantiate<Projectile>();
		shadow.Type = ProjectileType.Shadow;
		shadow.ShadowDelay = delay;
		shadow.Position = _player.GetPositionAtDelay(delay);

		AddChild(shadow);
	}

	private void OnCoinTimerTimeout()
	{
		if (_state != GameState.Playing || CoinScene == null) return;
		if (_activeCoins.Count >= 3) return;

		var coin = CoinScene.Instantiate<Coin>();

		Vector2 viewportSize = GetViewportRect().Size;
		float margin = 40f;
		coin.Position = new Vector2(
			_rng.RandfRange(margin, viewportSize.X - margin),
			_rng.RandfRange(margin, viewportSize.Y - margin)
		);

		coin.Collected += () => OnCoinCollected(coin, GetCoinValue());
		AddChild(coin);
		_activeCoins.Add(coin);
	}

	private void OnCoinCollected(Coin coin, int value)
	{
		_activeCoins.Remove(coin);

		int finalValue = _player.HasLife(1) ? value * 2 : value;

		GameData.Coins += finalValue;
		GameData.TotalCoinsCollected += finalValue;
		GetNode<Label>("UI/CoinLabel").Text = $"Coins: {GameData.Coins}";
		_player.TriggerSailorBoost();
	}

	private int GetCoinValue()
	{
		int value = 1 + (int)(_survivalTime / 20.0);
		return Mathf.Min(value, 5);
	}
}
