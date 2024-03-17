using Verse;
using Multiplayer.API;
namespace Quarry
{
    [StaticConstructorOnStartup]
    public static class StaticConstructorClass
    {
        static StaticConstructorClass()
        {
            // Multiplayer compatibility
            if (MP.enabled)
            {
                MP.RegisterAll();
                MP.RegisterSyncMethod(typeof(RimWorld.CompAssignableToPawn), nameof(RimWorld.CompAssignableToPawn.TryAssignPawn)).CancelIfAnyArgNull();
            }
            
        }
    }
}
