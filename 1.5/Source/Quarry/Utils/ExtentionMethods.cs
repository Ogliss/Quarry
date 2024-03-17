using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Quarry
{
    public static class ExtentionMethods
	{
		public static void LabeledScrollbarSetting(this Listing_Standard listing_Standard, string label, ref float setting, string tooltip = null)
		{
			Rect rect = listing_Standard.GetRect(Text.LineHeight).Rounded();
			Rect SliderOffset = rect.RightHalf().Rounded().RightPartPixels(200);

			Widgets.Label(rect, label);
			setting = Widgets.HorizontalSlider(
			SliderOffset,
			setting, 0.01f, 2f, true, roundTo: 0.01f);
			if (!tooltip.NullOrEmpty())
			{
				if (Mouse.IsOver(rect))
				{
					Widgets.DrawHighlight(rect);
				}
				TooltipHandler.TipRegion(rect, tooltip);
			}
		}

		public static void LabeledScrollbarSetting(this Listing_Standard listing_Standard, string label, ref int setting, string tooltip = null)
		{
			Rect rect = listing_Standard.GetRect(Text.LineHeight).Rounded();
			Rect SliderOffset = rect.RightHalf().Rounded().RightPartPixels(200);
			Widgets.Label(rect, label);
			setting = Widgets.HorizontalSlider(
			SliderOffset,
			setting, 0f, 100f, true).RoundToAsInt(1);
			if (!tooltip.NullOrEmpty())
			{
				if (Mouse.IsOver(rect))
				{
					Widgets.DrawHighlight(rect);
				}
				TooltipHandler.TipRegion(rect, tooltip);
			}
		}

        public static void QuarryHealthSetting(this Listing_Standard listing_Standard)
		{
			Rect rect = listing_Standard.GetRect(Text.LineHeight);
			Rect leftRect = rect.LeftHalf().Rounded();
			Rect rightRect = rect.RightHalf().Rounded();
			if (QuarrySettings.quarryMaxHealth <= 10000)
			{
				Widgets.Label(leftRect, "QRY_DepletionLabel".Translate(QuarrySettings.quarryMaxHealth.ToString("N0")));
			}
			else
			{
				Widgets.Label(leftRect, "QRY_DepletionLabel".Translate("Infinite"));
			}

			//Increment timer value by -100 (button).
			if (Widgets.ButtonText(new Rect(rightRect.xMin, rightRect.y, rightRect.height, rightRect.height), "-", true, false, true))
			{
				if (QuarrySettings.quarryMaxHealth >= 200)
				{
					QuarrySettings.quarryMaxHealth -= 100;
				}
			}

			QuarrySettings.quarryMaxHealth = Widgets.HorizontalSlider
			(
				new Rect(rightRect.xMin + rightRect.height + 10f, rightRect.y, rightRect.width - ((rightRect.height * 2) + 20f), rightRect.height),
				QuarrySettings.quarryMaxHealth, 100f, 10100f, true
			).RoundToAsInt(100);

			//Increment timer value by +100 (button).
			if (Widgets.ButtonText(new Rect(rightRect.xMax - rightRect.height, rightRect.y, rightRect.height, rightRect.height), "+", true, false, true))
			{
				if (QuarrySettings.quarryMaxHealth < 10100)
				{
					QuarrySettings.quarryMaxHealth += 100;
				}
			}

		}

		public static Listing_Standard BeginSection(this Listing_Standard L, float height, bool hidesection = false, int type = 0)
		{
			Rect rect = L.GetRect(height + 8f);
			if (!hidesection)
			{
				switch (type)
				{
					case 1:
						Widgets.DrawWindowBackground(rect);
						break;
					case 2:
						Widgets.DrawWindowBackgroundTutor(rect);
						break;
					case 3:
						Widgets.DrawOptionUnselected(rect);
						break;
					case 4:
						Widgets.DrawOptionSelected(rect);
						break;
					default:
						Widgets.DrawMenuSection(rect);
						break;
				}
			}
			Listing_Standard listing_Standard = new Listing_Standard();
			listing_Standard.Begin(rect.ContractedBy(4f));
			return listing_Standard;
		}
	}
}
