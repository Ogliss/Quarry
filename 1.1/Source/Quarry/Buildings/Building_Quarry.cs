using System.Collections.Generic;
using System.Linq;
using Multiplayer.API;
using UnityEngine;
using RimWorld;
using Verse;
using System.Text;

namespace Quarry
{

    public enum ResourceRequest
    {
        None,
        Resources,
        Blocks,
        Chunks
    }

    public enum MiningMode
    {
        Resources,
        Blocks,
        Chunks
    }

    public enum MoteType
    {
        None,
        LargeVein,
        Failure
    }

    [StaticConstructorOnStartup]
    public class Building_Quarry : Building
    {
        #region Fields
        public bool autoHaul = true;
        public MiningMode mineModeToggle = MiningMode.Resources;
        
        private float quarryPercent = 1f;
        private int jobsCompleted = 0;
        private bool firstSpawn = false;
        private CompAffectedByFacilities facilityComp;
        private List<string> rockTypesUnder = new List<string>();
        private List<ThingDef> blocksUnder = new List<ThingDef>();
        private List<ThingDef> chunksUnder = new List<ThingDef>();
        private List<Pawn> owners => compAssignable.AssignedPawnsForReading;
        #endregion Fields

        #region Public Properties
        public virtual int WallThickness => 2;
        public bool Unowned => owners.Count <= 0;
        public bool Depleted => QuarryPercent <= 0;

        public IEnumerable<Pawn> AssigningCandidates
        {
            get
            {
                if (!Spawned)
                {
                    return Enumerable.Empty<Pawn>();
                }
                return Map.mapPawns.FreeColonists.Where(x=> !x.WorkTagIsDisabled(WorkTags.Mining) && !x.WorkTypeIsDisabled(WorkTypeDefOf.Mining));
            }
        }
        public CompAssignableToPawn compAssignable
        {
            get
            {
                return this.TryGetComp<CompAssignableToPawn>();
            }
        }
        public CompForbiddable forbiddable
        {
            get
            {
                return this.TryGetComp<CompForbiddable>();
            }
        }

        public IEnumerable<Pawn> AssignedPawns
        {
            get
            {
                return owners;
            }
        }

        public int MaxAssignedPawnsCount
        {
            get
            {
                if (!Spawned)
                {
                    return 0;
                }
                return this.OccupiedRect().ContractedBy(WallThickness).Cells.Count();
            }
        }

        public bool AssignedAnything(Pawn pawn)
        {
            return false;
        }

        public float QuarryPercent
        {
            get
            {
                if (QuarrySettings.QuarryMaxHealth == int.MaxValue)
                {
                    return 100f;
                }
                return quarryPercent * 100f;
            }
        }

        public bool HasConnectedPlatform
        {
            get { return !facilityComp.LinkedFacilitiesListForReading.NullOrEmpty(); }
        }

        public List<ThingDef> ChunksUnder
        {
            get
            {
                if (chunksUnder.Count <= 0)
                {
                    MakeThingDefListsFrom(RockTypesUnder);
                }
                return chunksUnder;
            }
        }

        public List<ThingDef> BlocksUnder
        {
            get
            {
                if (blocksUnder.Count <= 0)
                {
                    MakeThingDefListsFrom(RockTypesUnder);
                }
                return blocksUnder;
            }
        }
        #endregion Public Properties

        #region Protected Properties
        protected virtual int QuarryDamageMultiplier => 1;
        protected virtual int SinkholeFrequency => 100;

        protected virtual List<IntVec3> LadderOffsets
        {
            get
            {
                return new List<IntVec3>() {
                    Static.LadderOffset_Big1,
                    Static.LadderOffset_Big2,
                    Static.LadderOffset_Big3,
                    Static.LadderOffset_Big4
                };
            }
        }
        #endregion Protected Properties

        #region Private Properties
        private int OwnerInspectCount => (owners.Count > 3) ? 3 : owners.Count;

        private bool PlayerCanSeeOwners
        {
            get
            {
                if (Faction == Faction.OfPlayer)
                {
                    return true;
                }
                for (int i = 0; i < owners.Count; i++)
                {
                    if (owners[i].Faction == Faction.OfPlayer || owners[i].HostFaction == Faction.OfPlayer)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private List<string> RockTypesUnder
        {
            get
            {
                if (rockTypesUnder.Count <= 0)
                {
                    rockTypesUnder = RockTypesFromMap();
                }
                return rockTypesUnder;
            }
        }

        private string HaulDescription
        {
            get { return (autoHaul ? Static.LabelHaul : Static.LabelNotHaul); }
        }
        #endregion Private Properties


        #region MethodGroup_Root
        #region MethodGroup_SaveLoad
        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                owners.RemoveAll((Pawn x) => x.Destroyed);
            }

            Scribe_Values.Look(ref autoHaul, "QRY_boolAutoHaul", true);
            Scribe_Values.Look<MiningMode>(ref mineModeToggle, "QRY_mineMode", MiningMode.Resources);
            Scribe_Values.Look(ref quarryPercent, "QRY_quarryPercent", 1f);
            Scribe_Values.Look(ref jobsCompleted, "QRY_jobsCompleted", 0);
            Scribe_Collections.Look(ref rockTypesUnder, "QRY_rockTypesUnder", LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                SortOwners();
            }
        }


        public override void PostMake()
        {
            base.PostMake();
            firstSpawn = true;
        }


        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            facilityComp = GetComp<CompAffectedByFacilities>();

            if (firstSpawn)
            {
                // Set the initial quarry health
                quarryPercent = 1f;

                CellRect rect = this.OccupiedRect();
                // Remove this area from the quarry grid. Quarries can never be built here again
            //    map.GetComponent<QuarryGrid>().RemoveFromGrid(rect);

                foreach (IntVec3 c in rect)
                {
                    // What type of terrain are we over?
                    TerrainDef td = c.GetTerrain(Map);
                    // Original method, problem here is that mods that use prefixes like Alpha Biomes "AB_" trigger the split and only pass the prefix, not the defname
                    //    string rockType = td.defName.Split('_').First();
                    // this seems like a better method, mods with prfixes are a little easier to handle stone from
                    string rockType = td.defName;
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
                    else
                    {
                        continue;
                    }
                    //   crappy alpha biomes compatability, gotta be a better method
                    if (rockType.StartsWith("GU_"))
                    {
                        rockType = rockType.Replace("GU_", "");
                    }
                    if (rockType.StartsWith("AB_"))
                    {
                        rockType = rockType.Replace("AB_", "");
                    }
                    // If this is a valid rock type, add it to the list
                    if (QuarryUtility.IsValidQuarryRock(rockType))
                    {
                        rockTypesUnder.Add(rockType);
                    }
                    // Change the terrain here to be quarried stone					
                    if (rect.ContractedBy(WallThickness).Contains(c))
                    {
                        Map.terrainGrid.SetTerrain(c, QuarryDefOf.QRY_QuarriedGround);
                    }
                    else
                    {
                        Map.terrainGrid.SetTerrain(c, QuarryDefOf.QRY_QuarriedGroundWall);
                    }
                }
                // Now that all the cells have been processed, create ThingDef lists
                MakeThingDefListsFrom(RockTypesUnder);
                // Spawn filth for the quarry
                foreach (IntVec3 c in rect)
                {
                    SpawnFilth(c);
                }
                // Change the ground back to normal quarried stone where the ladders are
                // This is to negate the speed decrease and encourages pawns to use the ladders
                foreach (IntVec3 offset in LadderOffsets)
                {
                    Map.terrainGrid.SetTerrain(Position + offset.RotatedBy(Rotation), QuarryDefOf.QRY_QuarriedGround);
                }
            }
        }


        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            RemoveAllOwners();

            CellRect rect = this.OccupiedRect();
            // Remove this area from the quarry grid. Quarries can never be built here again
            //     Map.GetComponent<QuarryGrid>().RemoveFromGrid(rect);

        //    Log.Message("Quarry at "+ QuarryPercent+ " "+ (int)(rect.Count() * (QuarryPercent / 100)) + " of " + rect.Count()+" cells should retain their quarrability");
            List<IntVec3> cells = GenAdj.CellsOccupiedBy(this).ToList();
            for (int i = 0; i < cells.Count; i++)
            {
                IntVec3 c = cells[i];
                if (rect.Count() * (QuarryPercent / 100) == 0 || rect.Count() * (QuarryPercent / 100) < i)
                {
                    Map.GetComponent<QuarryGrid>().RemoveFromGrid(c);
                }
                // Change the terrain here back to quarried stone, removing the walls
                Map.terrainGrid.SetTerrain(c, QuarryDefOf.QRY_QuarriedGround);
            }
            if (!QuarrySettings.letterSent && !TutorSystem.AdaptiveTrainingEnabled)
            {
                Find.LetterStack.ReceiveLetter(Static.LetterLabel, Static.LetterText, QuarryDefOf.CuproLetter, new RimWorld.Planet.GlobalTargetInfo(Position, Map));
                QuarrySettings.letterSent = true;
            }
            if (TutorSystem.AdaptiveTrainingEnabled)
            {
                LessonAutoActivator.TeachOpportunity(QuarryDefOf.QRY_ReclaimingSoil, OpportunityType.GoodToKnow);
            }
            base.Destroy(mode);
        }
        #endregion MethodGroup_SaveLoad


        #region MethodGroup_Assigning
        public void TryAssignPawn(Pawn pawn)
        {
            if (!owners.Contains(pawn))
            {
                owners.Add(pawn);
            }
        }

        public void TryUnassignPawn(Pawn pawn)
        {
            if (owners.Contains(pawn))
            {
                owners.Remove(pawn);
            }
        }

        public void SortOwners()
        {
            owners.SortBy((Pawn x) => x.thingIDNumber);
        }


        private void RemoveAllOwners()
        {
            owners.Clear();
        }
        #endregion MethodGroup_Assigning


        #region MethodGroup_Quarry
        private List<string> RockTypesFromMap()
        {
            // Try to add all the rock types found in the map
            List<string> list = new List<string>();
            List<TaggedString> tempRockTypesUnder = Find.World.NaturalRockTypesIn(Map.Tile).Select(r => r.LabelCap).ToList();
            foreach (string str in tempRockTypesUnder)
            {
                if (QuarryUtility.IsValidQuarryRock(str))
                {
                    list.Add(str);
                }
            }
            // This will cause an error if there still isn't a list, so make a new one using known rocks
            if (list.Count <= 0)
            {
                Log.Warning("Quarry:: No valid rock types were found in the map. Building list using vanilla rocks.");
                list = new List<string>() { "Sandstone", "Limestone", "Granite", "Marble", "Slate" };
            }
            return list;
        }


        private void MakeThingDefListsFrom(List<string> stringList)
        {
            chunksUnder = new List<ThingDef>();
            blocksUnder = new List<ThingDef>();
            foreach (string str in stringList)
            {
                if (QuarryUtility.IsValidQuarryChunk(str, out ThingDef chunk) && !chunksUnder.Contains(chunk))
                {
                    chunksUnder.Add(chunk);
                }
                if (QuarryUtility.IsValidQuarryBlocks(str, out ThingDef blocks) && !blocksUnder.Contains(blocks))
                {
                    blocksUnder.Add(blocks);
                }
            }
        }


        private void SpawnFilth(IntVec3 c)
        {
            List<Thing> thingsInCell = new List<Thing>();
            // Skip this cell if it is occupied by a placed object
            // This is to avoid save compression errors
            thingsInCell = Map.thingGrid.ThingsListAtFast(c);
            for (int t = 0; t < thingsInCell.Count; t++)
            {
                if (thingsInCell[t].def.saveCompressible)
                {
                    return;
                }
            }
            Rand.PushState();
            int filthAmount = Rand.RangeInclusive(1, 100);
            Rand.PopState();
            // If this cell isn't filthy enough, skip it
            if (filthAmount <= 20)
            {
                return;
            }
            // Check for dirt filth
            if (filthAmount <= 40)
            {
                GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.Filth_Dirt), c, Map);
            }
            else
            {
                GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.Filth_RubbleRock), c, Map);
                // Check for chunks
                if (filthAmount > 80)
                {
                    Rand.PushState();
                    GenSpawn.Spawn(ThingMaker.MakeThing(ChunksUnder.RandomElement()), c, Map);
                    Rand.PopState();
                }
            }
        }


        public ThingDef GiveResources(ResourceRequest req, out MoteType mote, out bool singleSpawn, out bool eventTriggered)
        {
            // Increment the jobs completed
            jobsCompleted++;

            eventTriggered = false;
            mote = MoteType.None;
            singleSpawn = true;

            // Decrease the amount this quarry can be mined, eventually depleting it
            if (QuarrySettings.QuarryMaxHealth != int.MaxValue)
            {
                QuarryMined();
            }

            Rand.PushState();
            // Determine if the mining job resulted in a sinkhole event, based on game difficulty
            if (jobsCompleted % SinkholeFrequency == 0 && Rand.Chance(Find.Storyteller.difficulty.difficulty / 50f))
            {
                eventTriggered = true;
                // The sinkhole damages the quarry a little
                QuarryMined(Rand.RangeInclusive(1, 3));
            }
            Rand.PopState();
            // Cache values since this process is convoluted and the values need to remain the same
            Rand.PushState();
            bool junkMined = Rand.Chance(QuarrySettings.junkChance / 100f);
            Rand.PopState();

            if (req == ResourceRequest.Chunks)
            {
                if (!junkMined)
                {
                    return ChunksUnder.RandomElement();
                }
                // The rock didn't break into a usable size, spawn rubble
                mote = MoteType.Failure;
                return ThingDefOf.Filth_RubbleRock;
            }
            // Check for blocks first to prevent spawning chunks (these would just be cut into blocks)
            if (req == ResourceRequest.Blocks)
            {
                if (!junkMined)
                {
                    singleSpawn = false;
                    return BlocksUnder.RandomElement();
                }
                // The rock didn't break into a usable size, spawn rubble
                mote = MoteType.Failure;
                return ThingDefOf.Filth_RubbleRock;
            }

            // Try to give junk before resources. This simulates only mining chunks or useless rubble
            if (junkMined)
            {
                Rand.PushState();
                bool junk = (Rand.Chance(QuarrySettings.chunkChance / 100f));
                Rand.PopState();
                if (junk)
                {
                    return ChunksUnder.RandomElement();
                }
                else
                {
                    mote = MoteType.Failure;
                    return ThingDefOf.Filth_RubbleRock;
                }
            }

            // Try to give resources
            if (req == ResourceRequest.Resources)
            {
                singleSpawn = false;

                return OreDictionary.TakeOne();
            }
            // The quarry was most likely toggled off while a pawn was still working. Give junk
            else
            {
                return ThingDefOf.Filth_RubbleRock;
            }
        }


        private void QuarryMined(int damage = 1)
        {
            quarryPercent = ((QuarrySettings.quarryMaxHealth * quarryPercent) - (damage * QuarryDamageMultiplier)) / QuarrySettings.quarryMaxHealth;
        }


        public bool TryFindBestPlatformCell(Thing t, Pawn carrier, Map map, Faction faction, out IntVec3 foundCell)
        {
            List<Thing> facilities = facilityComp.LinkedFacilitiesListForReading;
            for (int f = 0; f < facilities.Count; f++)
            {
                if (facilities[f].GetSlotGroup() == null || !facilities[f].GetSlotGroup().Settings.AllowedToAccept(t))
                {
                    continue;
                }
                foreach (IntVec3 c in GenAdj.CellsOccupiedBy(facilities[f]))
                {
                    if (StoreUtility.IsGoodStoreCell(c, map, t, carrier, faction))
                    {
                        foundCell = c;
                        return true;
                    }
                }
            }
            foundCell = IntVec3.Invalid;
            return false;
        }
        #endregion MethodGroup_Quarry

        #region MethodGroup_Inspecting
        public static FloatMenu MakeModeMenu(Building_Quarry __instance)
        {
            List<FloatMenuOption> floatMenu = new List<FloatMenuOption>();

            if (__instance.mineModeToggle != MiningMode.Resources)
            {
                floatMenu.Add(new FloatMenuOption(Static.LabelMineResources, delegate ()
                {
                    MineModeResources(__instance);
                }, MenuOptionPriority.Default, null, null, 0f, null, null));
            }
            if (__instance.mineModeToggle != MiningMode.Blocks && QuarryDefOf.Stonecutting.IsFinished)
            {
                floatMenu.Add(new FloatMenuOption(Static.LabelMineBlocks, delegate ()
                {
                    MineModeBlocks(__instance);
                }, MenuOptionPriority.Default, null, null, 0f, null, null));
            }
            if (__instance.mineModeToggle != MiningMode.Chunks)
            {
                floatMenu.Add(new FloatMenuOption(Static.LabelMineChunks, delegate ()
                {
                    MineModeChunks(__instance);
                }, MenuOptionPriority.Default, null, null, 0f, null, null));
            }

            return new FloatMenu(floatMenu);
        }
        
        [SyncMethod]
        private static void MineModeResources(Building_Quarry __instance)
        {
            __instance.mineModeToggle = MiningMode.Resources;
        }
        [SyncMethod]
        private static void MineModeBlocks(Building_Quarry __instance)
        {
            __instance.mineModeToggle = MiningMode.Blocks;
        }
        [SyncMethod]
        private static void MineModeChunks(Building_Quarry __instance)
        {
            __instance.mineModeToggle = MiningMode.Chunks;
        }

        private Texture2D icon
        {
            get
            {
                switch (this.mineModeToggle)
                {
                    case MiningMode.Resources:
                        return Static.DesignationQuarryResources;
                    case MiningMode.Blocks:
                        return Static.DesignationQuarryBlocks;
                    case MiningMode.Chunks:
                        return Static.DesignationQuarryChunks;
                    default:
                        return BaseContent.BadTex;
                }
            }
        }
        private string defaultLabel
        {
            get
            {
                switch (this.mineModeToggle)
                {
                    case MiningMode.Resources:
                        return Static.LabelMineResources;
                    case MiningMode.Blocks:
                        return Static.LabelMineBlocks;
                    case MiningMode.Chunks:
                        return Static.LabelMineChunks;
                    default:
                        return string.Empty;
                }
            }
        }
        private string defaultDesc
        {
            get
            {
                switch (this.mineModeToggle)
                {
                    case MiningMode.Resources:
                        return Static.DescriptionMineResources;
                    case MiningMode.Blocks:
                        return Static.DescriptionMineBlocks;
                    case MiningMode.Chunks:
                        return Static.DescriptionMineChunks;
                    default:
                        return string.Empty;
                }
            }
        }

        [SyncMethod]
        private void ToggleAutoHaul()
        {
            autoHaul = !autoHaul;
        }


        public override IEnumerable<Gizmo> GetGizmos()
        {
            Command_Action mineMode = new Command_Action()
            {
                icon = this.icon,
                defaultLabel = this.defaultLabel,
                defaultDesc = defaultDesc,
                hotKey = KeyBindingDefOf.Misc10,
                activateSound = SoundDefOf.Click,
                action = delegate ()
                {
                    Find.WindowStack.Add(MakeModeMenu(this));
                },
            };
            // Only allow this option if stonecutting has been researched
            // The default behavior is to allow resources, but not blocks
            if (!QuarryDefOf.Stonecutting.IsFinished)
            {
                mineMode.Disable(Static.ReportGizmoLackingResearch);
            }
            yield return mineMode;

            yield return new Command_Toggle()
            {
                icon = Static.DesignationHaul,
                defaultLabel = Static.LabelHaulMode,
                defaultDesc = HaulDescription,
                hotKey = KeyBindingDefOf.Misc11,
                activateSound = SoundDefOf.Click,
                isActive = () => autoHaul,
                toggleAction = () => ToggleAutoHaul(),
            };

            yield return new Command_Action
            {
                defaultLabel = Static.CommandBedSetOwnerLabel,
                icon = ContentFinder<Texture2D>.Get("UI/Commands/AssignOwner", true),
                defaultDesc = Static.CommandSetOwnerDesc,
                action = delegate
                {
                    Find.WindowStack.Add(new Dialog_AssignBuildingOwner(this.TryGetComp<CompAssignableToPawn>()));
                },
                hotKey = KeyBindingDefOf.Misc3
            };

            IEnumerator<Gizmo> enumerator = null;
            if (((this.def.BuildableByPlayer && this.def.passability != Traversability.Impassable && !this.def.IsDoor) || this.def.building.forceShowRoomStats) && Gizmo_RoomStats.GetRoomToShowStatsFor(this) != null && Find.Selector.SingleSelectedObject == this)
            {
                yield return new Gizmo_RoomStats(this);
            }
            if (this.def.Minifiable && base.Faction == Faction.OfPlayer)
            {
                yield return InstallationDesignatorDatabase.DesignatorFor(this.def);
            }
            Command command = BuildCopyCommandUtility.BuildCopyCommand(this.def, base.Stuff);
            if (command != null)
            {
                yield return command;
            }
            if (base.Faction == Faction.OfPlayer)
            {
                foreach (Command command2 in BuildFacilityCommandUtility.BuildFacilityCommands(this.def))
                {
                    yield return command2;
                }
                IEnumerator<Command> enumerator2 = null;
            }
            if (forbiddable!=null)
            {
                foreach (Gizmo item in forbiddable.CompGetGizmosExtra())
                {
                    yield return item;
                }
                ;
            }
            yield break;
        }


        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(Static.InspectQuarryPercent + ": " + QuarryPercent.ToStringDecimalIfSmall() + "%");
            if (PlayerCanSeeOwners)
            {
            //    stringBuilder.AppendLine("ForColonistUse".Translate());
                if (owners.Count == 0)
                {
                    stringBuilder.AppendLine("QRY_RestrictedTo".Translate() + ": " + "Nobody".Translate().ToLower());
                }
                else if (owners.Count == 1)
                {
                    stringBuilder.AppendLine("QRY_RestrictedTo".Translate() + ": " + owners[0].Label);
                }
                else
                {
                    stringBuilder.Append("QRY_RestrictedTo".Translate() + ": ");
                    bool conjugate = false;
                    for (int i = 0; i < OwnerInspectCount; i++)
                    {
                        if (conjugate)
                        {
                            stringBuilder.Append(", ");
                        }
                        conjugate = true;
                        stringBuilder.Append(owners[i].LabelShort);
                    }
                    if (owners.Count > 3)
                    {
                        stringBuilder.Append($" (+ {owners.Count - 3})");
                    }
                    stringBuilder.AppendLine();
                }
            }
            if (Prefs.DevMode)
            {
                List<string> report = new List<string>();
                for (int i = 0; i < rockTypesUnder.Count; i++)
                {
                    if (!report.Contains(rockTypesUnder[i]))
                    {
                        report.Add(rockTypesUnder[i]);
                    }
                }
                stringBuilder.AppendLine("Rock Types available: " + report.Count);
                foreach (string item in report)
                {
                    stringBuilder.AppendLine("     " + item.CapitalizeFirst());
                }
                stringBuilder.AppendLine();
            }
            return stringBuilder.ToString().TrimEndNewlines();
        }

        #endregion MethodGroup_Inspecting
        #endregion MethodGroup_Root
    }
}
