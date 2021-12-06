using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplaysToCSV
{
	internal static class GameData
	{
		public static Dictionary<string, (string name, int tier)> GetTankDictionary()
		{
			// get the tanks from the tanklist and put them in a dictionary to have easy access
			Dictionary<string, (string name, int tier)> tankDict = new();
			var tanks = JObject.Parse(File.ReadAllText(@"tanklist.json"));
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

			return tankDict;
		}
	}
}
