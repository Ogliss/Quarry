using System.Collections.Generic;

using Verse;

namespace Quarry
{

    public class QuarrySettings : ModSettings
    {

        internal static bool placeAnywhere = false;
        internal static bool letterSent = false;
        internal static bool allowRottable = false;
        internal static int quarryMaxHealth = 2000;
        internal static int junkChance = 60;
        internal static int chunkChance = 50;
        internal static int mineTicksAverage = 4500;
        internal static int mineTicksVariance = 4250;
        internal static float resourceModifer = 1f;
        internal static float reclaimedSoilFertility = 0.25f;
        internal static List<ThingCountExposable> oreDictionary = null;
        internal static List<ThingDef> database;
        internal static Dictionary<string, QuarryRockType> quarryableStone;

        internal static int QuarryMaxHealth
        {
            get
            {
                if (quarryMaxHealth > 10000)
                {
                    return int.MaxValue;
                }
                return quarryMaxHealth;
            }
        }


        public override void ExposeData()
        {
        //    Log.Message($"Quarry:: QuarrySettings ExposeData({Scribe.mode})");
            base.ExposeData();

            Scribe_Values.Look(ref letterSent, "QRY_letterSent", false);
            Scribe_Values.Look(ref placeAnywhere, "QRY_placeAnywhere", false);
            Scribe_Values.Look(ref allowRottable, "QRY_allowRottable", false);
            Scribe_Values.Look(ref quarryMaxHealth, "QRY_quarryMaxHealth", 2000);
            Scribe_Values.Look(ref junkChance, "QRY_junkChance", 60);
            Scribe_Values.Look(ref chunkChance, "QRY_chunkChance", 50);
            Scribe_Values.Look(ref mineTicksAverage, "QRY_mineTicksAverage", 4500);
            Scribe_Values.Look(ref mineTicksVariance, "QRY_mineTicksVariance", 4250);
            Scribe_Values.Look(ref resourceModifer, "QRY_resourceModifer", 1f);
            Scribe_Values.Look(ref reclaimedSoilFertility, "QRY_reclaimedSoilFertility", 0.25f);
            Scribe_Collections.Look(ref oreDictionary, "QRY_OreDictionary");

            // Remove all null entries in the oreDictionary
            // This is most likely due to removing a mod, which will trigger a game reset
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                /*
                List<ThingCountExposable> dict = new List<ThingCountExposable>();
                bool warning = false;
                for (int i = 0; i < oreDictionarySaved.Count; i++)
                {
                    if (oreDictionarySaved[i].Loaded)
                    {
                        dict.Add(oreDictionarySaved[i]);
                    }

                    else if (!warning)
                    {
                    //    warning = true;
                        Log.Warning($"Quarry:: Found 1({oreDictionarySaved[i].thingDefName}, {oreDictionarySaved[i].thingDefName}) or more null entries in ore dictionary. This is most likely due to an uninstalled mod. Removing entries from list.");
                    }
                }
                */
            }
        }
    }
}
