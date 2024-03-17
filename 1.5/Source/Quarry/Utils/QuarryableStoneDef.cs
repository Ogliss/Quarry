using System.Collections.Generic;
using Verse;

namespace Quarry
{
    public class QuarryableStoneDef : Def
    {
        public string terrainTag = string.Empty;
        public List<TerrainDef> terrainDefs = new List<TerrainDef>();
        public ThingDef rockDef = null;
        public ThingDef chunkDef = null;
        public ThingDef blockDef = null;
    }
}
