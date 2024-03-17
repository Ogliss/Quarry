using RimWorld;
using Verse;

namespace Quarry {

  public sealed class ThingCountExposable : IExposable {

    public ThingDef _thingDef = null;
    public string thingDefName;
    public int count;
    public float weight;

    public ThingCountExposable() {
    }


    public ThingCountExposable(ThingDef thingDef, int count) {
      this._thingDef = thingDef;
      this.thingDefName = thingDef.defName;
      this.count = count;
    }


    public override string ToString() {
      return string.Concat(new object[]
      {
        "(",
        count,
        "x ",
        (thingDef == null) ? "null" : thingDef.defName,
        ")"
      });
    }

        /*
    public override int GetHashCode() 
        {
            return (int)thingDef.shortHash + count << 16;
        }
        */
        private bool? loaded;
        public bool Loaded => loaded != null ? loaded.Value : (loaded = thingDef != null).Value;
        public ThingDef thingDef => _thingDef != null ? _thingDef : (thingDefName.NullOrEmpty() ? null : _thingDef = DefDatabase<ThingDef>.GetNamedSilentFail(thingDefName));
    public void ExposeData()
        {
    //        Log.Message($"Quarry:: ThingCountExposable ExposeData({Scribe.mode})");
            /*
            if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs && thingDef == null)
            {
                Log.Warning($"{Static.Quarry}:: Failed to load ThingCount. Setting default.");
                count = (count <= 0) ? 10 : count;
            }
            */
            if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
            {
                _thingDef = DefDatabase<ThingDef>.GetNamedSilentFail(thingDefName);
                loaded = _thingDef != null;
            }
            Scribe_Values.Look(ref thingDefName, "thingDef");
             Scribe_Values.Look(ref weight, "weight", 0f);
             Scribe_Values.Look(ref count, "count", 0);
    }
  }
}
