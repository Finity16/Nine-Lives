using System.Collections.Generic;

public static class GameData
{
	public static int HighScore = 0;
	public static int Coins = 0;
	public static int SpeedLevel = 0;
	public static int DodgeLevel = 0;
	public static int DashLevel = 0;

	public static int TotalCoinsCollected = 0;
	public static int TotalDodges = 0;
	public static int TotalDashes = 0;

	public static List<int> EquippedLives = new List<int> { 0 };

	public static bool IsLifeUnlocked(int id)
	{
		switch (id)
		{
			case 0: return true;
			case 1: return TotalCoinsCollected >= 500;
			case 2: return HighScore >= 25;
			case 3: return TotalDodges >= 5;
			case 4: return TotalDashes >= 25;
			default: return false;
		}
	}

	public static string GetLifeName(int id)
	{
		switch (id)
		{
			case 0: return "Life of the Peasant";
			case 1: return "Life of the Merchant";
			case 2: return "Life of the Nomad";
			case 3: return "Life of the Bandit";
			case 4: return "Life of the Warrior";
			default: return "???";
		}
	}

	public static string GetUnlockCondition(int id)
	{
		switch (id)
		{
			case 0: return "Unlocked by default";
			case 1: return "Collect 500 coins total";
			case 2: return "Reach 25 points in a run";
			case 3: return "Dodge an attack 5 times";
			case 4: return "Dash 25 times";
			default: return "Coming in a future update";
		}
	}

	public static string GetPassiveDescription(int id)
	{
		switch (id)
		{
			case 0: return "No passive bonus";
			case 1: return "Coin values are doubled";
			case 2: return "+20% movement speed";
			case 3: return "+10% dodge chance";
			case 4: return "-5s dash charge time";
			default: return "";
		}
	}
}
