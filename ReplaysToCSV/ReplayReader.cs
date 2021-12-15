// <copyright file="ReplayReader.cs" company="Josh">
// Copyright (c) Josh. All rights reserved.
// </copyright>

using System.Text.Json;
using System.Text.Json.Nodes;

namespace ReplaysToCSV
{
	internal static class ReplayReader
	{
		internal static async Task<ReplayInfo?> ReadReplayFile2(
			string path,
			Dictionary<string, (string name, int tier)> tankDict,
			CancellationToken cancellationToken = default)
		{
			try
			{
				cancellationToken.ThrowIfCancellationRequested();
				using FileStream sourceStream = File.Open(path, FileMode.Open, FileAccess.Read);

				// The number of data blocks
				int blockCount;
				{
					byte[] blockCountInBytes = new byte[4];
					sourceStream.Seek(4, SeekOrigin.Begin);
					await sourceStream.ReadAsync(blockCountInBytes.AsMemory(0, 4), cancellationToken);
					blockCount = BitConverter.ToInt32(blockCountInBytes, 0);
				}

				ReplayInfo? replayInfo = null;

				for (int blockIndex = 0; blockIndex < blockCount; blockIndex++)
				{
					// The block size; how many bytes to read for the current block
					int blockSize;
					{
						byte[] blockSizeInBytes = new byte[4];
						await sourceStream.ReadAsync(blockSizeInBytes.AsMemory(0, 4), cancellationToken);
						blockSize = BitConverter.ToInt32(blockSizeInBytes, 0);
					}

					SubStream sub = new(sourceStream, 0, blockSize);
					if (blockIndex == 0)
					{
						replayInfo = await JsonSerializer
							.DeserializeAsync<ReplayInfo>(sub, SourceGenerationContext.Default.ReplayInfo, cancellationToken);
						if (replayInfo is null)
						{
							return null;
						}
						if (replayInfo.vehicles is not null)
						{
							// get the min and max tier in the replay
							(int minTier, int maxTier) = GetTierSpreadNew(replayInfo.vehicles, tankDict);

							string? playerVehicleTag = replayInfo.playerVehicle;

							if (playerVehicleTag is not null)
							{
								playerVehicleTag = playerVehicleTag[(playerVehicleTag.IndexOf("-") + 1)..];
								// get the player tier so that we can compare it using GetTierPosition
								int playerTier = tankDict[playerVehicleTag].tier;
								if (tankDict.TryGetValue(playerVehicleTag, out (string name, int tier) vehicle))
								{
									replayInfo = replayInfo with
									{
										tier = vehicle.tier,
										playerVehicle = vehicle.name,
										tierPosition = GetTierPosition(playerTier, minTier, maxTier)
									};
								}
							}
						}
					}
					else if (blockIndex == 1 && replayInfo is not null)
					{
						JsonArray? postBattleInfo = JsonNode.Parse(sub)?.AsArray();
						if (postBattleInfo is not null)
						{
							var winningTeam = (int?)postBattleInfo[0]?["common"]?["winnerTeam"];
							if (winningTeam is not null)
							{
								int winningTeamInt = winningTeam.Value;
								replayInfo = replayInfo with
								{
									battleResult = winningTeamInt switch
									{
										0 => BattleResult.draw,
										1 => BattleResult.victory,
										2 => BattleResult.defeat,
										_ => BattleResult.undefined
									}
								};
							}
							int? duration = (int?)postBattleInfo[0]?["common"]?["duration"];
							replayInfo = replayInfo with
							{
								durationInSeconds = duration
							};

							int teamSurvived = 0;
							int enemySurvived = 0;
							var playerResults = postBattleInfo[1];
							if (playerResults is not null)
							{
								foreach (var property in playerResults.AsObject())
								{
									var value = property.Value;
									if (value is not null)
									{
										int? team = (int?)value["team"];
										string? isAliveString = value["isAlive"]?.ToString();
										bool? isAliveNullable = null;
										if (bool.TryParse(isAliveString, out bool isAlive))
										{
											isAliveNullable = isAlive;
										}
										if (team.HasValue && isAliveNullable.HasValue)
										{
											if (team.Value == 1 && isAliveNullable.Value)
											{
												teamSurvived++;
											}
											else if (team.Value == 2 && isAliveNullable.Value)
											{
												enemySurvived++;
											}
										}
									}
								}
								replayInfo = replayInfo with
								{
									teamSurvived = teamSurvived,
									enemySurvived = enemySurvived
								};
							}
						}
					}
				}

				return replayInfo;
			}
			catch (OperationCanceledException)
			{
				return null;
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

		private static (int minTier, int maxTier) GetTierSpreadNew(Dictionary<string, Vehicle> vehicle, Dictionary<string, (string name, int tier)> tankDict)
		{
			IEnumerable<int> tiers = vehicle
				.Select(vehicle =>
				{
					string? vehicleType = vehicle.Value.vehicleType;
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
