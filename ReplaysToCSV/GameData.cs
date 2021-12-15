using System.Text.Json.Nodes;

namespace ReplaysToCSV
{
	internal static class GameData
	{
		public static Dictionary<string, (string name, int tier)> GetTankDictionary()
		{
			// get the tanks from the tanklist and put them in a dictionary to have easy access
			Dictionary<string, (string name, int tier)> tankDict = new();
			var tanks = JsonNode.Parse(File.ReadAllText(@"tanklist.json"))?.AsObject();
			if (tanks is not null)
			{
				foreach (var tank in tanks)
				{
					var tankDetails = tank.Value;
					if (tankDetails is not null)
					{
						// tag is the code name
						// name is the readable name
						// tier is... the tier 
						string? tag = tankDetails["tag"]?.ToString();
						string? name = tankDetails["name"]?.ToString();
						int? tier = int.Parse(tankDetails["tier"]?.ToString());
						if (tag is not null && name is not null && tier is not null)
						{
							tankDict.Add(tag, (name, tier.Value));
						}
					}
				}
			}

			return tankDict;
		}
	}
}
