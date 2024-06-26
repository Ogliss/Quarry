﻿using System.Collections.Generic;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;
using Multiplayer.API;

namespace Quarry
{

    public class JobDriver_MineQuarry : JobDriver
    {

        private const int BaseTicksBetweenPickHits = 120;
        private const TargetIndex CellInd = TargetIndex.A;
        private const TargetIndex HaulableInd = TargetIndex.B;
        private const TargetIndex StorageCellInd = TargetIndex.C;
        private int ticksToPickHit = -1000;
        private Effecter effecter;
        private Building_Quarry quarryBuilding = null;

        protected Building_Quarry Quarry
        {
            get
            {
                if (quarryBuilding == null)
                {
                    quarryBuilding = job.GetTarget(TargetIndex.A).Cell.GetThingList(Map).Find(q => q is Building_Quarry) as Building_Quarry;
                }
                return quarryBuilding;
            }
        }

        protected Thing Haulable
        {
            get
            {
                if (TargetB.Thing != null)
                {
                    return TargetB.Thing;
                }
                Log.Warning("Quarry:: Trying to assign a null haulable to a pawn.");
                EndJobWith(JobCondition.Errored);
                return null;
            }
        }


        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(TargetIndex.A), job);
        }


        public override IEnumerable<Toil> MakeNewToils()
        {
            // Set up fail conditions
            this.FailOn(delegate
            {
                return Quarry == null || Quarry.IsForbidden(pawn) || Quarry.Depleted;
            });

            // Go to the quarry
            yield return Toils_Goto.Goto(CellInd, PathEndMode.OnCell);

            // Mine at the quarry. This is only for the delay
            yield return Mine();
            pawn.records.Increment(QuarryDefOf.QRY_CellsMined);
            // Collect resources from the quarry
            yield return Collect();

            // Reserve the resource
            yield return Toils_Reserve.Reserve(TargetIndex.B);

            // Reserve the storage cell
            yield return Toils_Reserve.Reserve(TargetIndex.C);

            // Go to the resource
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);

            // Pick up the resource
            yield return Toils_Haul.StartCarryThing(TargetIndex.B);

            // Carry the resource to the storage cell, then place it down
            Toil carry = Toils_Haul.CarryHauledThingToCell(TargetIndex.C);
            yield return carry;
            yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.C, carry, true);
        }


        private void ResetTicksToPickHit()
        {
            float statValue = pawn.GetStatValue(StatDefOf.MiningSpeed, true);
            if (statValue < 0.5f && pawn.Faction != Faction.OfPlayer)
            {
                statValue = 0.5f;
            }
            ticksToPickHit = Mathf.RoundToInt((120f / statValue));
        }

        [SyncMethod]
        private Toil Mine()
        {
            int time = QuarrySettings.mineTicksAverage;
            int min = Mathf.Max(time - QuarrySettings.mineTicksVariance, 30);
            int max = time + QuarrySettings.mineTicksVariance;
            return new Toil()
            {
                tickAction = delegate
                {
                    ResourceRequest req = (ResourceRequest)(((int)Quarry.mineModeToggle) + 1);
                    pawn.rotationTracker.Face(Quarry.Position.ToVector3Shifted());
                    if (req == ResourceRequest.Chunks)
                    {

                    }
                    if (ticksToPickHit < -100)
                    {
                        ResetTicksToPickHit();
                    }
                    if (pawn.skills != null)
                    {
                        pawn.skills.Learn(SkillDefOf.Mining, 0.11f, false);
                    }
                    ticksToPickHit--;

                    if (ticksToPickHit <= 0)
                    {
                        if (effecter == null)
                        {
                            effecter = EffecterDefOf.Mine.Spawn();
                        }
                        effecter.Trigger(pawn, Quarry);

                        ResetTicksToPickHit();
                    }
                },
                defaultDuration = (int)Mathf.Clamp(time / pawn.GetStatValue(StatDefOf.MiningSpeed, true), min, max),
                defaultCompleteMode = ToilCompleteMode.Delay,
                handlingFacing = true,
                activeSkill = (() => SkillDefOf.Mining)

        }.WithProgressBarToilDelay(TargetIndex.B, false, -0.5f);
        }

        [SyncMethod]
        private Toil Collect()
        {
            return new Toil()
            {
                initAction = delegate
                {
                    // Increment the record for how many cells this pawn has mined since this counts as mining
                    // TODO: B19 - change to quarry m3
                    pawn.records.Increment(RecordDefOf.CellsMined);

                    // Start with None to act as a fallback. Rubble will be returned with this parameter
                    ResourceRequest req = ResourceRequest.None;

                    // Use the mineModeToggle to determine the request
                    req = (ResourceRequest)(((int)Quarry.mineModeToggle)+1);

                    MoteType mote = MoteType.None;
                    int stackCount = 1;

                    // Get the resource from the quarry
                    ThingDef def = Quarry.GiveResources(req, out mote, out bool singleSpawn, out bool eventTriggered);
                    // If something went wrong, bail out
                    if (def == null || def.thingClass == null)
                    {
                        Log.Warning("Quarry:: Tried to quarry mineable ore, but the ore given was null.");
                        mote = MoteType.None;
                        singleSpawn = true;
                        // This shouldn't happen at all, but if it does let's add a little reward instead of just giving rubble
                        def = ThingDefOf.ChunkSlagSteel;
                    }
                    bool component = def == ThingDefOf.ComponentIndustrial || def == ThingDefOf.ComponentSpacer;
                    Thing haulableResult = ThingMaker.MakeThing(def);
                    if (!singleSpawn && !component)
                    {
                        int sub = (int)(def.BaseMarketValue / 2f);
                        sub = Mathf.Clamp(sub, 0, 10);
                        //patching rng for multiplayer - Cody Spring
                        if (MP.IsInMultiplayer)
                        {
                            Rand.PushState();
                            stackCount += Mathf.Min(Rand.RangeInclusive(15 - sub, 40 - (sub * 2)), def.stackLimit - 1);
                            stackCount = (int)((stackCount * QuarrySettings.resourceModifer) * GetActor().GetStatValue(StatDefOf.MiningYield));
                            Rand.PopState();
                        }
                        else
                        {
                            stackCount += Mathf.Min(Rand.RangeInclusive(15 - sub, 40 - (sub * 2)), def.stackLimit - 1);
                            stackCount = (int)((stackCount * QuarrySettings.resourceModifer) * GetActor().GetStatValue(StatDefOf.MiningYield));
                        }
                    }

                    if (component)
                    {
                        int limit = def == ThingDefOf.ComponentIndustrial ? 5 : 1;
                        //changing from UnityEngine random to Verse random and patching for multiplayer
                        if (MP.IsInMultiplayer)
                        {
                            Rand.PushState();
                            stackCount += Rand.Range(0, limit);
                            Rand.PopState();
                        }
                        else
                        {
                            stackCount += Rand.Range(0, limit);
                        }
                    }

                    haulableResult.stackCount = stackCount;

                    if (stackCount >= 30)
                    {
                        mote = MoteType.LargeVein;
                    }

                    bool usesQuality = false;
                    // Adjust quality for items that use it
                    if (haulableResult.TryGetComp<CompQuality>() != null)
                    {
                        usesQuality = true;
                        haulableResult.TryGetComp<CompQuality>().SetQuality(QualityUtility.GenerateQualityTraderItem(), ArtGenerationContext.Outsider);
                    }
                    // Adjust hitpoints, this was just mined from under the ground after all
                    if (def.useHitPoints && !def.thingCategories.Contains(QuarryDefOf.StoneChunks) && def != ThingDefOf.ComponentIndustrial)
                    {
                        float minHpThresh = 0.25f;
                        if (usesQuality)
                        {
                            minHpThresh = Mathf.Clamp((float)haulableResult.TryGetComp<CompQuality>().Quality / 10f, 0.1f, 0.7f);
                        }
                        //patching RNG for multiplayer - Cody Spring
                        int hp;
                        if(MP.IsInMultiplayer)
                        {
                            Rand.PushState();
                            hp = Mathf.RoundToInt(Rand.Range(minHpThresh, 1f) * haulableResult.MaxHitPoints);
                            Rand.PopState();
                        }
                        else
                        {
                            hp = Mathf.RoundToInt(Rand.Range(minHpThresh, 1f) * haulableResult.MaxHitPoints);
                        }
                        hp = Mathf.Max(1, hp);
                        haulableResult.HitPoints = hp;
                    }

                    // Place the resource near the pawn
                    GenPlace.TryPlaceThing(haulableResult, pawn.Position, Map, ThingPlaceMode.Near);

                    // If the resource had a mote, throw it
                    if (mote == MoteType.LargeVein)
                    {
                        MoteMaker.ThrowText(haulableResult.DrawPos, Map, Static.TextMote_LargeVein, Color.green, 3f);
                    }
                    else if (mote == MoteType.Failure)
                    {
                        MoteMaker.ThrowText(haulableResult.DrawPos, Map, Static.TextMote_MiningFailed, Color.red, 3f);
                    }

                    // If the sinkhole event was triggered, damage the pawn and end this job
                    // Even if the sinkhole doesn't incapacitate the pawn, they will probably want to seek medical attention
                    if (eventTriggered)
                    {
                        NamedArgument pawnName = new NamedArgument(0, pawn.NameShortColored);
                        Messages.Message("QRY_MessageSinkhole".Translate(pawnName), pawn, MessageTypeDefOf.NegativeEvent);
                        DamageInfo dInfo = new DamageInfo(DamageDefOf.Crush, 9, category: DamageInfo.SourceCategory.Collapse);
                        dInfo.SetBodyRegion(BodyPartHeight.Bottom, BodyPartDepth.Inside);
                        pawn.TakeDamage(dInfo);

                        EndJobWith(JobCondition.Succeeded);
                    }
                    else
                    {
                        // Prevent the colonists from trying to haul rubble, which just makes them visit the platform
                        if (def == ThingDefOf.Filth_RubbleRock)
                        {
                            EndJobWith(JobCondition.Succeeded);
                        }
                        else
                        {
                            // If this is a chunk or slag, mark it as haulable if allowed to
                            if (def.designateHaulable && Quarry.autoHaul)
                            {
                                Map.designationManager.AddDesignation(new Designation(haulableResult, DesignationDefOf.Haul));
                            }

                            // Try to find a suitable storage spot for the resource, removing it from the quarry
                            // If there are no platforms with free space, or if the resource is a chunk, try to haul it to a storage area
                            if (Quarry.autoHaul)
                            {
                                if (!def.thingCategories.Contains(QuarryDefOf.StoneChunks) && Quarry.HasConnectedPlatform && Quarry.TryFindBestPlatformCell(haulableResult, pawn, Map, pawn.Faction, out IntVec3 c))
                                {
                                    job.SetTarget(TargetIndex.B, haulableResult);
                                    job.count = haulableResult.stackCount;
                                    job.SetTarget(TargetIndex.C, c);
                                }
                                else
                                {
                                    StoragePriority currentPriority = StoreUtility.CurrentStoragePriorityOf(haulableResult);
                                    Job result;
                                    if (!StoreUtility.TryFindBestBetterStorageFor(haulableResult, pawn, Map, currentPriority, pawn.Faction, out c, out IHaulDestination haulDestination, true))
                                    {
                                        JobFailReason.Is("NoEmptyPlaceLower".Translate(), null);
                                    }
                                    else if (haulDestination is ISlotGroupParent)
                                    {
                                        result = HaulAIUtility.HaulToCellStorageJob(pawn, haulableResult, c, false);
                                    }
                                    else
                                    {
                                        job.SetTarget(TargetIndex.B, haulableResult);
                                        job.count = haulableResult.stackCount;
                                        job.SetTarget(TargetIndex.C, c);
                                    }
                                }
                            }
                            // If there is no spot to store the resource, end this job
                            else
                            {
                                EndJobWith(JobCondition.Succeeded);
                            }
                        }
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
}

