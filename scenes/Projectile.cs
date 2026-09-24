using Godot;

public enum ProjectileType
{
	Normal,
	BigSlow,
	Splitter,
	Goliath,
	Shadow
}

public partial class Projectile : Area2D
{
	[Export] public float Speed = 250f;
	[Export] public ProjectileType Type = ProjectileType.Normal;
	public Vector2 Direction = Vector2.Right;
	public PackedScene ProjectileScene;
	public float ShadowDelay = 1f;

	private float _splitDelay = 1.5f;
	private float _elapsedTime = 0f;
	private bool _hasSplit = false;

	private void SetSpriteForType()
	{
		var sprite = GetNode<Sprite2D>("Sprite2D");

		switch (Type)
		{
			case ProjectileType.Normal:
				sprite.Texture = GD.Load<Texture2D>("res://sprites/projectile.png");
				break;
			case ProjectileType.BigSlow:
				sprite.Texture = GD.Load<Texture2D>("res://sprites/bigprojectile.png");
				break;
			case ProjectileType.Splitter:
				sprite.Texture = GD.Load<Texture2D>("res://sprites/splitter.png");
				break;
			case ProjectileType.Goliath:
				sprite.Texture = GD.Load<Texture2D>("res://sprites/goliath.png");
				break;
			case ProjectileType.Shadow:
				sprite.Texture = GD.Load<Texture2D>("res://sprites/projectile.png");
				break;
		}
	}

	private float GetDesiredRadius()
	{
	switch (Type)
	{
		case ProjectileType.Normal: return 10f;
		case ProjectileType.BigSlow: return 25f;
		case ProjectileType.Splitter: return 20f;
		case ProjectileType.Goliath: return 40f;
		case ProjectileType.Shadow: return 10f;
		default: return 10f;
	}
	}

	private void ApplySizing()
	{
	var collision = GetNode<CollisionShape2D>("CollisionShape2D");
	var sprite = GetNode<Sprite2D>("Sprite2D");

	float radius = GetDesiredRadius();

	if (collision.Shape is CircleShape2D circle)
	{
		circle.Radius = radius;
	}

	if (sprite.Texture != null)
	{
		float diameter = radius * 2f;
		float textureWidth = sprite.Texture.GetWidth();
		float spriteScale = diameter / textureWidth;
		sprite.Scale = new Vector2(spriteScale, spriteScale);
	}
	}

	public override void _Ready()
	{
		var notifier = GetNode<VisibleOnScreenNotifier2D>("VisibleOnScreenNotifier2D");
		notifier.ScreenExited += OnScreenExited;
		BodyEntered += OnBodyEntered;
		SetSpriteForType();
		ApplySizing();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Type == ProjectileType.Shadow)
		{
			if (Player.Instance != null)
			{
				Position = Player.Instance.GetPositionAtDelay(ShadowDelay);
			}
			return;
		}

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
			GetParent().AddChild(fragment);
		}

		QueueFree();
	}

	private void OnScreenExited()
	{
		if (Type == ProjectileType.Shadow) return;
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
