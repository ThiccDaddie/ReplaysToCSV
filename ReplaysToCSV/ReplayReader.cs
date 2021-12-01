// <copyright file="ReplayReader.cs" company="Josh">
// Copyright (c) Josh. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace ReplaysToCSV
{
    public class ReplayReader
    {
        private readonly Dictionary<string, (string name, int tier)> tankDict;

        internal ReplayReader()
        {
            // get the tanks from the tanklist and put them in a dictionary to have easy access
            var tanks = JObject.Parse(File.ReadAllText(@"tanklist.json"));
            tankDict = new();
            foreach (var tank in tanks.Children())
            {
                var tankDetails = tank.First;
                if (tankDetails is not null)
                {
                    // tag is the code name
                    // name is the readable name
                    // tier is... the tier 
                    string? tag = tankDetails["tag"]?.ToString();
                    string? name = tankDetails["name"]?.ToString();
                    int? tier = tankDetails["tier"]?.ToObject<int>();
                    if (tag is not null && name is not null && tier is not null)
                    {
                        tankDict.Add(tag, (name, tier.Value));
                    }
                }
            }
        }

        internal ReplayInfo? ReadReplayFile(string path)
        {
            try
            {
                string data;
                using (FileStream SourceStream = File.Open(path, FileMode.Open, FileAccess.Read))
                {
                    // The offset in bytes at which you can find the "block size"
                    int dataBlockSzOffset = 8;
                    // The block size in bytes
                    byte[] blockSizeInBytes = new byte[4];
                    // The block size
                    int blockSize;
                    // The block of data
                    byte[] block;

                    // skip ahead until the block size
                    SourceStream.Seek(dataBlockSzOffset, SeekOrigin.Begin);
                    // get the blocksize
                    SourceStream.Read(blockSizeInBytes, 0, 4);
                    // convert the blocksize to int
                    blockSize = BitConverter.ToInt32(blockSizeInBytes, 0);

                    block = new byte[blockSize];
                    // get the block of data
                    SourceStream.Read(block, 0, blockSize);
                    // decode the data
                    data = Encoding.Default.GetString(block);
                }

                ReplayInfoWithVehicles? replayInfoWithVehicles;
                // deserialize the data into a POCO
                replayInfoWithVehicles = JsonConvert.DeserializeObject<ReplayInfoWithVehicles>(data);

                if (replayInfoWithVehicles is not null && replayInfoWithVehicles.Vehicles is not null)
                {
                    // get the min and max tier in the replay
                    (int minTier, int maxTier) = GetTierSpread(replayInfoWithVehicles.Vehicles);

                    string? playerVehicleTag = replayInfoWithVehicles.PlayerVehicle;

                    if (playerVehicleTag is not null)
                    {
                        playerVehicleTag = playerVehicleTag[(playerVehicleTag.IndexOf("-") + 1)..];
                        // get the player tier so that we can compare it using GetTierPosition
                        int playerTier = tankDict[playerVehicleTag].tier;
                        replayInfoWithVehicles.Tier = playerTier;
                        replayInfoWithVehicles.PlayerVehicle = tankDict[playerVehicleTag].name;
                        replayInfoWithVehicles.TierPosition = GetTierPosition(playerTier, minTier, maxTier);
                        return replayInfoWithVehicles;
                    }
                }
            }
            catch
            {
            }
            return null;
        }

        private static TierPosition GetTierPosition(int tier, int minTier, int maxTier)
        {
            int tierLambda = maxTier - minTier;
            switch (tierLambda)
            {
                case (0):
                    return TierPosition.one_tier;
                case (1):
                    if (tier == maxTier) return TierPosition.two_tier_top;
                    return TierPosition.two_tier_bottom;
                case (2):
                    if (tier == maxTier) return TierPosition.three_tier_top;
                    if (tier == minTier) return TierPosition.three_tier_bottom;
                    return TierPosition.three_tier_middle;
                default:
                    return TierPosition.invalid;
            }
        }

        private (int minTier, int maxTier) GetTierSpread(Dictionary<string, Tank> players)
        {
            IEnumerable<int> tiers = players
                .Select(player =>
                {
                    string? vehicleType = player.Value.VehicleType;
                    if (vehicleType == null) { return 0; }
                    string vehicleTag = vehicleType[(vehicleType.IndexOf(":") + 1)..];
                    if (tankDict.ContainsKey(vehicleTag))
                    {
                        return tankDict[vehicleTag].tier;
                    }
                    return 0;
                })
                .Distinct()
                .Where(tier => tier != 0);

            return (tiers.Min(), tiers.Max());
        }
    }
}
