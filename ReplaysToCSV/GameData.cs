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

		public static Dictionary<string, string> GetMapDictionary()
		{
			// get the maps from the maplist and put them in a dictionary to have easy access
			Dictionary<string, string> mapDict = new();
			var maps = JObject.Parse(File.ReadAllText(@"mapList.json"));
			foreach (var map in maps.Children())
			{
				var mapDetails = map.First;
				if (mapDetails is not null)
				{
					// tier is... the tier 
					string? name = mapDetails["name_i18n"]?.ToString();
					string? arena_id = mapDetails["arena_id"]?.ToString();
					if (name is not null && arena_id is not null)
					{
						mapDict.Add(arena_id, name);
					}
				}
			}

			return mapDict;
		}
	}
}
