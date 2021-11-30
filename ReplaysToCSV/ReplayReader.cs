// <copyright file="ReplayReader.cs" company="Josh">
// Copyright (c) Josh. All rights reserved.
// </copyright>

using CsvHelper.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
//using Newtonsoft.Json;

namespace ReplaysToCSV
{
    public class ReplayReader
    {
        private static readonly CsvConfiguration config = new(CultureInfo.InvariantCulture)
        {
            NewLine = Environment.NewLine,
        };

        private readonly Dictionary<string, (string name, int tier)> tankDict;

        internal ReplayReader()
        {
            var tanks = JObject.Parse(File.ReadAllText(@"tanklist.json"));
            tankDict = new();
            foreach (var tank in tanks.Children())
            {
                var tankDetails = tank.First;
                if (tankDetails is not null)
                {
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
                    int dataBlockSzOffset = 8;

                    byte[] blockSizeInBytes = new byte[4];
                    SourceStream.Seek(dataBlockSzOffset, SeekOrigin.Begin);
                    SourceStream.Read(blockSizeInBytes, 0, 4);
                    int blockSize = BitConverter.ToInt32(blockSizeInBytes, 0);

                    byte[] block = new byte[blockSize];
                    SourceStream.Read(block, 0, blockSize);
                    data = Encoding.Default.GetString(block);
                }

                ReplayInfoWithVehicles? replayInfoWithVehicles;
                replayInfoWithVehicles = JsonConvert.DeserializeObject<ReplayInfoWithVehicles>(data, new JsonSerializerSettings
                {
                    DateFormatString = "dd.MM.yyyy HH:mm:ss",
                });

                if (replayInfoWithVehicles is not null && replayInfoWithVehicles.Vehicles is not null)
                {
                    (int minTier, int maxTier) = GetTierSpread(replayInfoWithVehicles.Vehicles);

                    string? playerVehicleTag = replayInfoWithVehicles.PlayerVehicle;
                    if (playerVehicleTag is not null)
                    {
                        playerVehicleTag = playerVehicleTag.Substring(playerVehicleTag.IndexOf("-") + 1);
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

        internal async Task<ReplayInfo?> ReadReplayFileAsync(string path)
        {
            try
            {
                string data;
                using (FileStream SourceStream = File.Open(path, FileMode.Open, FileAccess.Read))
                {
                    int dataBlockSzOffset = 8;

                    byte[] blockSizeInBytes = new byte[4];
                    SourceStream.Seek(dataBlockSzOffset, SeekOrigin.Begin);
                    await SourceStream.ReadAsync(blockSizeInBytes, 0, 4);
                    int blockSize = BitConverter.ToInt32(blockSizeInBytes, 0);

                    byte[] block = new byte[blockSize];
                    await SourceStream.ReadAsync(block, 0, blockSize);
                    data = Encoding.Default.GetString(block);
                }

                ReplayInfoWithVehicles? replayInfoWithVehicles;
                replayInfoWithVehicles = JsonConvert.DeserializeObject<ReplayInfoWithVehicles>(data, new JsonSerializerSettings
                {
                    DateFormatString = "dd.MM.yyyy HH:mm:ss",
                });

                if (replayInfoWithVehicles is not null && replayInfoWithVehicles.Vehicles is not null)
                {
                    (int minTier, int maxTier) = GetTierSpread(replayInfoWithVehicles.Vehicles);

                    string? playerVehicleTag = replayInfoWithVehicles.PlayerVehicle;
                    if (playerVehicleTag is not null)
                    {
                        playerVehicleTag = playerVehicleTag.Substring(playerVehicleTag.IndexOf("-") + 1);
                        int playerTier = tankDict[playerVehicleTag].tier;
                        replayInfoWithVehicles.Tier = playerTier;
                        replayInfoWithVehicles.PlayerVehicle = tankDict[playerVehicleTag].name;
                        replayInfoWithVehicles.TierPosition = GetTierPosition(playerTier, minTier, maxTier);
                        return replayInfoWithVehicles;
                    }
                }
            }
            catch {
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

        private (int minTier, int maxTier) GetTierSpread(Dictionary<string, Tank> battleVehicles)
        {
            IEnumerable<int> tiers = battleVehicles
                .Select(battleVehicle =>
                {
                    string? vehicleType = battleVehicle.Value.VehicleType;
                    if (vehicleType == null) { return 0; }
                    string vehicleTag = vehicleType.Split(":")[1];
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
