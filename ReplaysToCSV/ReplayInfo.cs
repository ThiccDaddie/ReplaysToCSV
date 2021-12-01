namespace ReplaysToCSV
{
    internal enum TierPosition
    {
        one_tier,
        two_tier_bottom,
        two_tier_top,
        three_tier_bottom,
        three_tier_middle,
        three_tier_top,
        invalid
    }

    internal class ReplayInfo
    {
        public string? PlayerVehicle { get; set; }
        public string? PlayerName { get; set; }
        public string? GameplayID { get; set; }
        public string? MapName { get; set; }
        public int? Tier { get; set; }
        public TierPosition? TierPosition { get; set; }        
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
