using HarmonyLib;
using Verse;

namespace Quarry
{
    [HarmonyPatch(typeof(FogGrid), "Notify_FogBlockerRemoved")]
    static class FogGrid_Notify_FogBlockerRemoved_Patch
    {
        static void Postfix()
        {
            Find.CurrentMap?.GetComponent<QuarryGrid>()?.Notify_FogGridUpdate();
        }
    }
}
