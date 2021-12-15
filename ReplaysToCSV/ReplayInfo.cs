namespace ReplaysToCSV
{
    public enum BattleResult
	{
        victory,
        defeat,
        draw,
        undefined
	}

    public enum TierPosition
    {
        one_tier,
        two_tier_bottom,
        two_tier_top,
        three_tier_bottom,
        three_tier_middle,
        three_tier_top,
        undefined
    }

	public record ReplayInfo
	{
		public string? playerVehicle { get; set; }

		public string? clientVersionFromXml { get; set; }

		public string? clientVersionFromExe { get; set; }

		public Dictionary<string, Vehicle>? vehicles { get; set; }

		public string? regionCode { get; set; }

		public int? playerID { get; set; }

		public string? serverName { get; set; }

		public string? mapDisplayName { get; set; }

		public string? dateTime { get; set; }

		public string? mapName { get; set; }

		public string? gameplayID { get; set; }

		public int? battleType { get; set; }

		public bool? hasMods { get; set; }

		public string? playerName { get; set; }

		public int? tier { get; set; } = -1;

		public TierPosition? tierPosition { get; set; } = ReplaysToCSV.TierPosition.undefined;

		public int? durationInSeconds { get; set; }

		public BattleResult? battleResult { get; set; } = ReplaysToCSV.BattleResult.undefined;

		public int? teamSurvived { get; set; }

		public int? enemySurvived { get; set; }
	}

	public record Vehicle
	{
		public string? vehicleType { get; set; }
	}
}
