using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Quarry
{
    [StaticConstructorOnStartup]
    public static partial class HarmonyInstance
    {
        public static Harmony harmony = new Harmony("com.ogliss.rimworld.mod.quarry");
        static HarmonyInstance()
        {
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
