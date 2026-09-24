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
	public static bool MonkUnlocked = false;
	public static bool MonarchUnlocked = false;
	public static bool ForceUnlockAll = false;

	public static bool IsLifeUnlocked(int id)
	{
		if (ForceUnlockAll) return true;
		switch (id)
		{
			case 0: return true;
			case 1: return TotalCoinsCollected >= 100;
			case 2: return HighScore >= 25;
			case 3: return TotalDodges >= 1;
			case 4: return TotalDashes >= 10;
			case 5: return HighScore >= 50;
			case 6: return MonkUnlocked;
			case 7: return MonarchUnlocked;
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
			case 5: return "Life of the Sailor";
			case 6: return HighScore >= 50 ? "Life of the Monk" : "???";
			case 7: return HighScore >= 50 ? "Life of the Monarch" : "???";
			default: return "???";
		}
	}

	public static string GetUnlockCondition(int id)
	{
		switch (id)
		{
			case 0: return "Unlocked by default";
			case 1: return "Collect 100 coins total";
			case 2: return "Reach 25 points in a run";
			case 3: return "Dodge an attack once";
			case 4: return "Dash 10 times";
			case 5: return "Reach 50 points in a run";
			case 6: return HighScore >= 50 ? "Reach 35 points without dashing" : "???";
			case 7: return HighScore >= 50 ? "Reach 30 points with only 1 life equipped" : "???";
			default: return "Coming in a future update";
		}
	}

	public static string GetPassiveDescription(int id)
	{
		switch (id)
		{
			case 0: return "No passive bonus";
			case 1: return "Coin values are doubled";
			case 2: return "+10% movement speed";
			case 3: return "+5% dodge chance";
			case 4: return "-5s dash charge time";
			case 5: return "Coin pickups grant a brief speed burst";
			case 6: return HighScore >= 50 ? "Slowing aura" : "???";
			case 7: return HighScore >= 50 ? "Lethal dash" : "???";
			default: return "";
		}
	}
}
