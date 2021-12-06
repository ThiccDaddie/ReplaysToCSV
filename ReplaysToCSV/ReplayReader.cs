// <copyright file="ReplayReader.cs" company="Josh">
// Copyright (c) Josh. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace ReplaysToCSV
{
	internal static class ReplayReader
	{

		internal static ReplayInfo? ReadReplayFile(
			string path,
			Dictionary<string, (string name, int tier)> tankDict,
			Dictionary<string, string> mapDict)
		{
			try
			{
				//string data;
				List<string> data = new();
				using FileStream SourceStream = File.Open(path, FileMode.Open, FileAccess.Read);

				// The number of data blocks
				int blockCount;

				{
					byte[] blockCountInBytes = new byte[4];
					SourceStream.Seek(4, SeekOrigin.Begin);
					SourceStream.Read(blockCountInBytes, 0, 4);
					blockCount = BitConverter.ToInt32(blockCountInBytes, 0);
				}

				for (int i = 0; i < blockCount; i++)
				{
					// The block size; how many bytes to read for the current block
					int blockSize;
					{
						byte[] blockSizeInBytes = new byte[4];
						SourceStream.Read(blockSizeInBytes, 0, 4);
						blockSize = BitConverter.ToInt32(blockSizeInBytes, 0);
					}

					// The current block of data
					byte[] block = new byte[blockSize];
					SourceStream.Read(block, 0, blockSize);

					// decode the data
					data.Add(Encoding.Default.GetString(block));
				}

				if (!data.Any())
				{
					return null;
				}

				ReplayInfoWithVehicles? replayInfoWithVehicles;
				// deserialize the data into a POCO
				replayInfoWithVehicles = JsonConvert.DeserializeObject<ReplayInfoWithVehicles>(data.First());

				if (replayInfoWithVehicles is not null)
				{
					if (replayInfoWithVehicles.MapName is not null && mapDict.TryGetValue(replayInfoWithVehicles.MapName, out string? mapName))
					{						
						replayInfoWithVehicles.MapName = mapName;
					}
					if (replayInfoWithVehicles.Vehicles is not null)
					{
						// get the min and max tier in the replay
						(int minTier, int maxTier) = GetTierSpread(replayInfoWithVehicles.Vehicles, tankDict);

						string? playerVehicleTag = replayInfoWithVehicles.PlayerVehicle;

						if (playerVehicleTag is not null)
						{
							playerVehicleTag = playerVehicleTag[(playerVehicleTag.IndexOf("-") + 1)..];
							// get the player tier so that we can compare it using GetTierPosition
							int playerTier = tankDict[playerVehicleTag].tier;
							replayInfoWithVehicles.Tier = playerTier;
							replayInfoWithVehicles.PlayerVehicle = tankDict[playerVehicleTag].name;
							replayInfoWithVehicles.TierPosition = GetTierPosition(playerTier, minTier, maxTier);
						}
					}
					if (data.Count > 1)
					{
						try
						{
							JArray afterBattleInfo = JArray.Parse(data[1]);
							{
								var winningTeam = (int?)afterBattleInfo[0]?["common"]?["winnerTeam"];
								if (winningTeam is not null)
								{
									int winningTeamInt = winningTeam.Value;
									replayInfoWithVehicles.BattleResult = winningTeamInt switch
									{
										0 => BattleResult.draw,
										1 => BattleResult.victory,
										2 => BattleResult.defeat,
										_ => BattleResult.undefined
									};
								}
								int? duration = (int?)afterBattleInfo[0]?["common"]?["duration"];
								replayInfoWithVehicles.DurationInSeconds = duration;
							}
							{
								int teamSurvived = 0;
								int enemySurvived = 0;
								foreach (var property in afterBattleInfo[1].Children())
								{
									var value = property.First();
									if (value is not null)
									{
										int? team = (int?)value["team"];
										bool? isAlive = (bool?)value["isAlive"];
										if (team.HasValue && isAlive.HasValue)
										{
											if (team.Value == 1 && isAlive.Value)
											{
												teamSurvived++;
											}
											else if (team.Value == 2 && isAlive.Value)
											{
												enemySurvived++;
											}
										}
									}
								}
								replayInfoWithVehicles.TeamSurvived = teamSurvived;
								replayInfoWithVehicles.EnemySurvived = enemySurvived;
							}
						}
						catch
						{

						}
					}
					return replayInfoWithVehicles;
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
					return TierPosition.undefined;
			}
		}

		private static (int minTier, int maxTier) GetTierSpread(Dictionary<string, Tank> players, Dictionary<string, (string name, int tier)> tankDict)
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
