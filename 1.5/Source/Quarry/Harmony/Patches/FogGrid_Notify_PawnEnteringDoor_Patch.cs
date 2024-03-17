using HarmonyLib;
using Verse;

namespace Quarry
{
    [HarmonyPatch(typeof(FogGrid), "Notify_PawnEnteringDoor")]
    static class FogGrid_Notify_PawnEnteringDoor_Patch
    {
        static void Postfix()
        {
            Find.CurrentMap?.GetComponent<QuarryGrid>()?.Notify_FogGridUpdate();
        }
    }
}
