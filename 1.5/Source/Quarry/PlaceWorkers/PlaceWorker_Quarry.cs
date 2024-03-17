using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using Verse;

namespace Quarry
{

    public class PlaceWorker_Quarry : PlaceWorker
    {

        List<IntVec3> occupiedCellsTemp = new List<IntVec3>();

        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            // God Mode allows placing the quarry without the grid restriction
            if (!DebugSettings.godMode && !QuarrySettings.placeAnywhere)
            {
                int occCells = 0;
                int rockCells = 0;
                foreach (IntVec3 c in GenAdj.CellsOccupiedBy(loc, rot, checkingDef.Size))
                {
                    occCells++;

                    // Make sure the quarry is placeable here
                    if (map.GetComponent<QuarryGrid>().GetCellBool(c))
                    {
                        rockCells++;
                    }
                }

                // Require at least 60% rocky terrain
                if ((float)(occCells - rockCells) / occCells > 0.4f)
                {
                    return Static.ReportNotEnoughStone;
                }
            }

            return true;
        }

        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            if (!DebugSettings.godMode /*&& !QuarrySettings.placeAnywhere*/)
            {
                // Draw the placement areas
                Find.CurrentMap.GetComponent<QuarryGrid>().MarkForDraw();
                GenDraw.DrawFieldEdges(GenAdj.CellsOccupiedBy(center, rot, def.Size).ToList());
            }
        }
        
    }
}
