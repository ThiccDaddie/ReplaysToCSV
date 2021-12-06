namespace ReplaysToCSV
{
    internal enum BattleResult
	{
        victory,
        defeat,
        draw,
        undefined
	}

    internal enum TierPosition
    {
        one_tier,
        two_tier_bottom,
        two_tier_top,
        three_tier_bottom,
        three_tier_middle,
        three_tier_top,
        undefined
    }

    internal class ReplayInfo
    {
        public string? PlayerVehicle { get; set; }
        public string? PlayerName { get; set; }
        public string? GameplayID { get; set; }
        public string? MapName { get; set; }
        public int? Tier { get; set; } = -1;
        public TierPosition? TierPosition { get; set; } = ReplaysToCSV.TierPosition.undefined;
        public BattleResult? BattleResult { get; set; } = ReplaysToCSV.BattleResult.undefined;
        public int? TeamSurvived { get; set; }
        public int? EnemySurvived { get; set; }
	}

    internal class ReplayInfoWithVehicles : ReplayInfo
    {
        public Dictionary<string, Tank>? Vehicles { get; set; }
    }

    internal class Tank
    {
        public string? Name { get; set; }
        public string? VehicleType { get; set; }
    }
}
