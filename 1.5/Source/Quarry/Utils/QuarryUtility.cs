using System;
using System.Collections.Generic;
using System.Linq;

using RimWorld;
using Verse;

namespace Quarry {

	public static class QuarryUtility {

		public static Predicate<TerrainDef> IsValidQuarryTerrain = (
			(TerrainDef t) => (t == TerrainDefOf.Gravel ||
			t.defName.EndsWith("_Rough") ||
			t.defName.EndsWith("_RoughHewn") ||
			t.defName.EndsWith("_Smooth"))
		);

		public static void BuildStoneDict()
		{
			QuarrySettings.quarryableStone = new Dictionary<string, QuarryRockType>();
			List<string> processed = new List<string>();
			List<TerrainDef> terrainDefs = DefDatabase<TerrainDef>.AllDefsListForReading;
			List<ThingDef> rockDefs = DefDatabase<ThingDef>.AllDefsListForReading.FindAll(x => x.building?.mineableThing != null /*&& x.building.mineableThing.defName.Contains("Chunk")*/);
			List<ThingDef> chunkDefs = DefDatabase<ThingDef>.AllDefsListForReading.FindAll(x => x.defName.Contains("Chunk") && rockDefs.Any(y => y.building.mineableThing == x));
			List<ThingDef> blockDefs = DefDatabase<ThingDef>.AllDefsListForReading.FindAll(x => x.defName.Contains("Blocks"));
			/*
			Log.Message($"Quarry: Checking\n" +
				$"{blockDefs.Count} Blocks {((List<string>)blockDefs.Select(r => r.LabelCap)).ToCommaList()}" +
				$"\n\n{chunkDefs.Count} Chunks {((List<string>)chunkDefs.Select(r => r.LabelCap)).ToCommaList()}" +
				$"\n\n{rockDefs.Count} Rocks {((List<string>)rockDefs.Select(r => r.LabelCap)).ToCommaList()}" +
				$"\n\n{terrainDefs.Count} TerrainDefs {((List<string>)terrainDefs.Select(r => r.LabelCap)).ToCommaList()}");
			*/
			List<QuarryableStoneDef> modDefs = DefDatabase<QuarryableStoneDef>.AllDefsListForReading;
			//		Log.Message($"Quarry: Checking {rockDefs.Count} rockDefs");
			foreach (var rockDef in rockDefs)
			{
				if (rockDef.building.naturalTerrain == null || QuarrySettings.quarryableStone.Values.Any(x => x.rockDef == rockDef)) continue;
				ThingDef chunkDef = rockDef.building.mineableThing;
				TerrainDef terrainNatural = rockDef.building.naturalTerrain;
				TerrainDef terrainLeave = rockDef.building.leaveTerrain;
				TerrainDef terrainSmoothed = terrainNatural?.smoothedTerrain ?? null;
				ThingDef blockDef = blockDefs.Find(x => x.label.Contains(rockDef.label)) ?? null;
				QuarrySettings.quarryableStone.Add(rockDef.LabelCap, new QuarryRockType(rockDef, chunkDef, blockDef));
				//	Log.Message($"Quarry:: RockDef New: {rockDef.LabelCap}:: rockDef: {rockDef}, chunkDef: {chunkDef}, blockDef: {blockDef}");
				if (!processed.Contains(rockDef.LabelCap)) processed.Add(rockDef.LabelCap);
			}
			//	Log.Message($"Quarry: Checking {modDefs.Count} QuarryableStoneDefs");
			foreach (var modDef in modDefs)
			{
				string rockType = QuarryUtility.RockType(modDef.terrainTag);
				ThingDef rockDef = modDef.rockDef ?? rockDefs.Find(x => x.defName.Contains(rockType));
				//	Log.Message($"Quarry: Checking modDef: {modDef} ");
				ThingDef chunkDef = modDef.chunkDef ?? chunkDefs.Find(x => x.defName.Contains(rockType));
				ThingDef blockDef = modDef.blockDef ?? blockDefs.Find(x => x.defName.Contains(rockType));
				if (chunkDef == null && rockDef != null)
				{
					chunkDef = rockDef.building.mineableThing;
				}
				if (chunkDef != null)
				{
					if (!modDef.terrainDefs.NullOrEmpty() && QuarrySettings.quarryableStone.First(x => x.Value.chunkDef == chunkDef).Value is QuarryRockType quarryRock)
					{
						quarryRock.terrainDefs.AddRange(modDef.terrainDefs);
						//	Log.Message($"Quarry:: QuarryableStoneDef Updated: {rockType}:: rockDef: {rockDef}, chunkDef: {chunkDef}, blockDef: {blockDef}");
					}
					else
					{
						QuarrySettings.quarryableStone.Add(rockDef.LabelCap, new QuarryRockType(rockDef, chunkDef, blockDef));
						//	Log.Message($"Quarry:: QuarryableStoneDef New: {rockType}:: rockDef: {rockDef}, chunkDef: {chunkDef}, blockDef: {blockDef}");
					}
					if (!processed.Contains(rockDef.LabelCap)) processed.Add(rockDef.LabelCap);
				}
			}
			//	Log.Message($"Quarry: Checking {terrainDefs.Count} terrainDefs");
			foreach (var terrainDef in terrainDefs)
			{
				if (QuarrySettings.quarryableStone.Values.Any(x => x.terrainDefs.Contains(terrainDef))) continue;
				string rockType = QuarryUtility.RockType(terrainDef.defName);
				ThingDef rockDef = rockDefs.Find(x => x.defName.Contains(rockType));
				if (rockDef == null || rockType == "Sand")
				{
					continue;
				}
				//	Log.Message($"Quarry: Checking terrainDef: {item} ");
				ThingDef chunkDef = chunkDefs.Find(x => x.defName.Contains(rockType));
				ThingDef blockDef = blockDefs.Find(x => x.defName.Contains(rockType));
				if (chunkDef == null && rockDef != null && ((rockDef.building.naturalTerrain != null && rockDef.building.naturalTerrain == terrainDef) || (rockDef.building.leaveTerrain != null && rockDef.building.leaveTerrain == terrainDef)))
				{
					chunkDef = rockDef.building.mineableThing;

				}
				if (chunkDef != null)
				{
					if (QuarrySettings.quarryableStone.First(x => x.Value.chunkDef == chunkDef).Value is QuarryRockType quarryRock)
					{
						quarryRock.terrainDefs.Add(terrainDef);
						//	Log.Message($"Quarry:: TerrainDef Updated: {terrainDef}:: rockDef: {rockDef}, chunkDef: {chunkDef}, blockDef: {blockDef}");
					}
					else
					{
						QuarrySettings.quarryableStone.Add(rockDef.LabelCap, new QuarryRockType(rockDef, chunkDef, blockDef));
						//	Log.Message($"Quarry:: TerrainDef New: {terrainDef}:: rockDef: {rockDef}, chunkDef: {chunkDef}, blockDef: {blockDef}");
					}
					if (!processed.Contains(rockDef.LabelCap)) processed.Add(rockDef.LabelCap);
				}

			}
			Log.Message($"Quarry:: Total {processed.Count} quarryable terrain found, processed: {processed.ToCommaList()}" + (modDefs.Count > 0 ? $"\n{modDefs.Count} QuarryableStoneDef's found" : ""));
		}

		public static string RockType(string input)
		{
			string rockType = input;
			if (rockType.EndsWith("_Rough"))
			{
				rockType = rockType.Replace("_Rough", "");
			}
			else
			if (rockType.EndsWith("_RoughHewn"))
			{
				rockType = rockType.Replace("_RoughHewn", "");
			}
			else
			if (rockType.EndsWith("_Smooth"))
			{
				rockType = rockType.Replace("_Smooth", "");
			}
			if (rockType.StartsWith("AB_"))
			{
				rockType = rockType.Replace("AB_", "");
			}
			else
			if (rockType.StartsWith("GU_"))
			{
				rockType = rockType.Replace("GU_", "");
			}
			if (rockType.Contains(" "))
			{
				rockType = rockType.Replace(" ", "");
			}
			return rockType;
		}

		public static bool IsValidQuarryRock(TerrainDef str, out QuarryRockType rockType, out string key) {
			rockType = null;
			key = string.Empty;
			if (QuarrySettings.database.NullOrEmpty() && QuarrySettings.quarryableStone.NullOrEmpty()) {
				Log.Error("Quarry:: Trying to validate rock types before the database has been built.");
				return false;
			}
			rockType = QuarrySettings.quarryableStone.Values.FirstOrFallback(x => x.terrainDefs.Contains(str));
			if (rockType != null)
			{
				key = QuarrySettings.quarryableStone.FirstOrFallback(x => x.Value.terrainDefs.Contains(str)).Key;
				Log.Message($"Quarry:: search for {str}: {rockType} found using key {key}");
				return rockType.terrainDefs.Contains(str);

			}
			/*
            else
			{
				Log.Message($"Quarry:: search for {str}: failed, using old system");
			}
			// If there isn't a known chunk or blocks for this, it probably isn't a rock type and wouldn't work for spawning anyways
			// This allows Cupro's Stones to work, and any other mod that uses standard naming conventions for stones
			if (QuarrySettings.database.Find(t => t.defName == "Chunk" + str) != null &&
					QuarrySettings.database.Find(t => t.defName == "Blocks" + str) != null) {
				return true;
			}
			*/
			return false;
		}
		
		public static IEnumerable<ThingDef> PossibleThingDefs() {
			return from d in DefDatabase<ThingDef>.AllDefs
						 where (d.category == ThingCategory.Item && d.scatterableOnMapGen && !d.destroyOnDrop && !d.MadeFromStuff && (d.GetCompProperties<CompProperties_Rottable>() == null || QuarrySettings.allowRottable))
						 select d;
		}
	}
}
