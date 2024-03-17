using System.Collections.Generic;
using Verse;

namespace Quarry
{
    public class QuarryRockType : IExposable
	{
		public QuarryRockType(ThingDef rockDef, ThingDef chunkDef = null, ThingDef blockDef = null)
		{
			this.rockDef = rockDef;
			this.chunkDef = chunkDef;
			this.blockDef = blockDef;
            if (rockDef != null)
			{
				if (this.chunkDef == null) this.chunkDef = this.rockDef.building.mineableThing;
                if (this.blockDef == null && this.chunkDef != null && !this.chunkDef.butcherProducts.NullOrEmpty() && this.chunkDef.butcherProducts.Any(x=> x.thingDef != null && x.thingDef.defName.ToLower().Contains("block")))
                {
					this.blockDef = this.chunkDef.butcherProducts.Find(x => x.thingDef != null && x.thingDef.defName.ToLower().Contains("block")).thingDef;
				}
                if (this.rockDef.building?.naturalTerrain != null)
				{
					terrainDefs.Add(this.rockDef.building.naturalTerrain);
				}
                if (this.rockDef.building?.leaveTerrain != null)
				{
					terrainDefs.Add(this.rockDef.building.leaveTerrain);
				}
                if (this.rockDef.building?.naturalTerrain?.smoothedTerrain != null)
				{
					terrainDefs.Add(this.rockDef.building.naturalTerrain.smoothedTerrain);
				}
			}
		}
		public ThingDef rockDef;
		public ThingDef chunkDef;
		public ThingDef blockDef;
		public List<TerrainDef> terrainDefs = new List<TerrainDef>();

        public void ExposeData()
        {
            throw new System.NotImplementedException();
        }
    }
}
