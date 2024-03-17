using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using Verse;

namespace Quarry
{

    public sealed class QuarryMod : Mod 
	{
		
		private Vector2 scrollPosition = Vector2.zero;
		private float scrollViewHeight = 0f;
		private string baseTimeBuffer;
		private string varTimeBuffer;
		private string reclaimedSoilFertilityBuffer;

		public QuarryMod(ModContentPack mcp) : base(mcp) 
		{
			LongEventHandler.ExecuteWhenFinished(GetSettings);
			LongEventHandler.ExecuteWhenFinished(PushDatabase);
			LongEventHandler.ExecuteWhenFinished(BuildDictionary);
			LongEventHandler.ExecuteWhenFinished(SetFertility);
		}


		public void GetSettings() 
		{
			GetSettings<QuarrySettings>();
		}


		public override void WriteSettings() {
			base.WriteSettings();
			QuarryDefOf.QRY_ReclaimedSoil.fertility = QuarrySettings.reclaimedSoilFertility;
		}


		private void PushDatabase() 
		{
			QuarrySettings.database = DefDatabase<ThingDef>.AllDefsListForReading;

		}

		private void SetFertility()
        {
			QuarryDefOf.QRY_ReclaimedSoil.fertility = QuarrySettings.reclaimedSoilFertility;
		}
		private void BuildDictionary()
		{
			if (QuarrySettings.oreDictionary == null)
			{
				OreDictionary.Build();
			}
			if (QuarrySettings.quarryableStone == null)
			{
				QuarryUtility.BuildStoneDict();
			}
			QuarryDefOf.QRY_ReclaimedSoil.fertility = QuarrySettings.reclaimedSoilFertility;
		}

		public override string SettingsCategory() 
		{
			return Static.Quarry;
		}


		public override void DoSettingsWindowContents(Rect rect) 
		{
			Listing_Standard list = new Listing_Standard() 
			{
				ColumnWidth = rect.width
			};

			Rect topRect = rect.TopPart(0.25f);
			Rect bottomRect = rect.BottomPart(0.75f);
			Rect fullRect = list.GetRect(Text.LineHeight).ContractedBy(4);
			Rect leftRect = fullRect.LeftHalf().Rounded();
			Rect rightRect = fullRect.RightHalf().Rounded();

			list.Begin(rect);

			list.QuarryHealthSetting();
			Listing_Standard listtop = list.BeginSection(topRect.height, true);
			{
				listtop.ColumnWidth = fullRect.width * 0.488f;
				listtop.CheckboxLabeled(Static.LetterSent, ref QuarrySettings.letterSent, Static.ToolTipLetter);
				listtop.CheckboxLabeled(Static.AllowRottable, ref QuarrySettings.allowRottable, Static.ToolTipAllowRottable);
				listtop.CheckboxLabeled(Static.PlaceAnywhere, ref QuarrySettings.placeAnywhere, Static.ToolTipPlaceAnywhere);
				listtop.TextFieldNumericLabeled("QRY_SettingsReclaimedSoilFertility".Translate(QuarrySettings.reclaimedSoilFertility), ref QuarrySettings.reclaimedSoilFertility, ref reclaimedSoilFertilityBuffer, 0f, tooltip: Static.ToolTipTicksReclaimedSoilFertility);
				listtop.NewColumn();
				listtop.LabeledScrollbarSetting("QRY_SettingsJunkChance".Translate(QuarrySettings.junkChance), ref QuarrySettings.junkChance, Static.ToolTipJunkChance);
				listtop.LabeledScrollbarSetting("QRY_SettingsChunkChance".Translate(QuarrySettings.chunkChance), ref QuarrySettings.chunkChance, Static.ToolTipChunkChance);
				listtop.TextFieldNumericLabeled("QRY_SettingsMineTimeBase".Translate(QuarrySettings.mineTicksAverage), ref QuarrySettings.mineTicksAverage, ref baseTimeBuffer, 30, tooltip: Static.ToolTipTicksAverage);
				listtop.TextFieldNumericLabeled("QRY_SettingsMineTimeVar".Translate(QuarrySettings.mineTicksVariance), ref QuarrySettings.mineTicksVariance, ref varTimeBuffer, 0, QuarrySettings.mineTicksAverage * 0.9f, tooltip: Static.ToolTipTicksVariance);
			}
			list.EndSection(listtop);
			/*
			Listing_Standard listbottom = list.BeginSection(bottomRect.height, true);
			{

			}
			list.EndSection(listbottom);
			*/
			list.End();
			{

				Rect DictInterfaceRect = bottomRect.TopPart(0.25f).Rounded();
				{
					Rect labelRect = DictInterfaceRect.TopPart(0.75f).Rounded();
					Text.Font = GameFont.Medium;
					Text.Anchor = TextAnchor.MiddleCenter;
					Widgets.Label(labelRect, Static.LabelDictionary);
					Text.Font = GameFont.Small;
					Text.Anchor = TextAnchor.UpperLeft;
				}
				{
					Rect buttonsRect = DictInterfaceRect.BottomPart(0.25f).Rounded();
					Rect addRect = buttonsRect.LeftThird();
					Rect rmvRect = buttonsRect.MiddleThird();
					Rect resRect = buttonsRect.RightThird();

					// Add an entry to the dictionary
					if (Widgets.ButtonText(addRect, Static.LabelAddThing)) {
						List<FloatMenuOption> thingList = new List<FloatMenuOption>();
						foreach (ThingDef current in from t in QuarryUtility.PossibleThingDefs()
																					orderby t.label
																					select t) {

							bool skip = false;
							for (int i = 0; i < QuarrySettings.oreDictionary.Count; i++) {
								if (QuarrySettings.oreDictionary[i].thingDef == current) {
									skip = true;
									break;
								}
							};
							if (skip) continue;

							thingList.Add(new FloatMenuOption(current.LabelCap, delegate {
								QuarrySettings.oreDictionary.Add(new ThingCountExposable(current, 1));
							}));
						}
						FloatMenu menu = new FloatMenu(thingList);
						Find.WindowStack.Add(menu);
					}

					// Remove an entry from the dictionary
					if (Widgets.ButtonText(rmvRect, Static.LabelRemoveThing) && QuarrySettings.oreDictionary.Count >= 2) {
						List<FloatMenuOption> thingList = new List<FloatMenuOption>();
						foreach (ThingCountExposable current in from t in QuarrySettings.oreDictionary
																										orderby t.thingDef.label
																										select t) {
							ThingDef localTd = current.thingDef;
							thingList.Add(new FloatMenuOption(localTd.LabelCap, delegate {
								for (int i = 0; i < QuarrySettings.oreDictionary.Count; i++) {
									if (QuarrySettings.oreDictionary[i].thingDef == localTd) {
										QuarrySettings.oreDictionary.Remove(QuarrySettings.oreDictionary[i]);
										break;
									}
								};
							}));
						}
						FloatMenu menu = new FloatMenu(thingList);
						Find.WindowStack.Add(menu);
					}

					// Reset the dictionary
					if (Widgets.ButtonText(resRect, Static.LabelResetList)) {
						OreDictionary.Build(true);
					}
				}

				{
					Rect listRect = bottomRect.BottomPart(0.75f).Rounded();
					Rect cRect = listRect.ContractedBy(10f);
					Rect position = listRect;
					Rect outRect = new Rect(0f, 0f, position.width, position.height);
					Rect viewRect = new Rect(0f, 0f, position.width - 16f, scrollViewHeight);

					float num = 0f;
					List<ThingCountExposable> dict = new List<ThingCountExposable>(QuarrySettings.oreDictionary.Where(x=> x.Loaded));

					GUI.BeginGroup(position);
					Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect, true);

					foreach (var tc in dict.Select((value, index) => new { index, value })) {
						Rect entryRect = new Rect(0f, num, viewRect.width, 32);
						Rect iconRect = entryRect.LeftPartPixels(32);
						Rect labelRect = new Rect(entryRect.LeftThird().x + 33f, entryRect.y, entryRect.LeftThird().width - 33f, entryRect.height);
						Rect texEntryRect = new Rect(entryRect.LeftHalf().RightPartPixels(103).x, entryRect.y, 60f, entryRect.height);
						Rect pctRect = new Rect(entryRect.LeftHalf().RightPartPixels(41).x, entryRect.y, 40f, entryRect.height);
						Rect sliderRect = new Rect(entryRect.RightHalf().x, entryRect.y, entryRect.RightHalf().width, entryRect.height);

						Widgets.ThingIcon(iconRect, tc.value.thingDef);
						Widgets.Label(labelRect, tc.value.thingDef.LabelCap);
						Widgets.Label(pctRect, $"{QuarrySettings.oreDictionary.WeightAsPercentageOf(tc.value.count).ToStringDecimal()}%");
						int val = tc.value.count;
						string nullString = null;
						Widgets.TextFieldNumeric(
							texEntryRect,
							ref QuarrySettings.oreDictionary[tc.index].count,
							ref nullString,
							0, OreDictionary.MaxWeight);
						val = Widgets.HorizontalSlider(
							sliderRect,
							QuarrySettings.oreDictionary[tc.index].count, 0f, OreDictionary.MaxWeight, true
						).RoundToAsInt(1);
						if (val != QuarrySettings.oreDictionary[tc.index].count) {
							QuarrySettings.oreDictionary[tc.index].count = val;
						}

						if (Mouse.IsOver(entryRect)) {
							Widgets.DrawHighlight(entryRect);
						}
						TooltipHandler.TipRegion(entryRect.LeftThird(), tc.value.thingDef.description);

						num += 32f;
						scrollViewHeight = num;
					}

					Widgets.EndScrollView();
					GUI.EndGroup();
				}

				}
		}
	}
}
