using Godot;

public enum ProjectileType
{
	Normal,
	BigSlow,
	Splitter,
	Goliath
}

public partial class Projectile : Area2D
{
	[Export] public float Speed = 250f;
	[Export] public ProjectileType Type = ProjectileType.Normal;
	public Vector2 Direction = Vector2.Right;
	public PackedScene ProjectileScene;

	private float _splitDelay = 1.5f;
	private float _elapsedTime = 0f;
	private bool _hasSplit = false;

	public override void _Ready()
	{
		var notifier = GetNode<VisibleOnScreenNotifier2D>("VisibleOnScreenNotifier2D");
		notifier.ScreenExited += OnScreenExited;
		BodyEntered += OnBodyEntered;
	}

	public override void _PhysicsProcess(double delta)
	{
		float effectiveSpeed = Speed;

		if (Player.Instance != null && Player.Instance.HasLife(6))
		{
			float distance = Position.DistanceTo(Player.Instance.Position);
			if (distance < 60f)
			{
				effectiveSpeed = Speed * 0.75f;
			}
		}

		Position += Direction * effectiveSpeed * (float)delta;

		if (Type == ProjectileType.Splitter && !_hasSplit)
		{
			_elapsedTime += (float)delta;
			if (_elapsedTime >= _splitDelay)
			{
				Split();
			}
		}
	}

	private void Split()
	{
		_hasSplit = true;

		Vector2[] diagonalDirections = new Vector2[]
		{
			new Vector2(1, 1).Normalized(),
			new Vector2(1, -1).Normalized(),
			new Vector2(-1, 1).Normalized(),
			new Vector2(-1, -1).Normalized()
		};

		foreach (var dir in diagonalDirections)
		{
			var fragment = ProjectileScene.Instantiate<Projectile>();
			fragment.Position = Position;
			fragment.Direction = dir;
			fragment.Speed = Speed * 1.4f;
			fragment.Type = ProjectileType.Normal;
			fragment.ProjectileScene = ProjectileScene;
			fragment.Scale = new Vector2(0.6f, 0.6f);
			GetParent().AddChild(fragment);
		}

		QueueFree();
	}

	private void OnScreenExited()
	{
		QueueFree();
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is Player player)
		{
			if (player.IsDashKillActive())
			{
				QueueFree();
				return;
			}
			player.Die();
		}
	}
}
