
using UnityEngine;
using Verse;

namespace Quarry
{
    public static class QuarryWidgets
    {
		public static void TextFieldNumericLabeled<T>(this Listing_Standard listing, string label, ref T val, ref string buffer, float min = 0f, float max = 1E+09f, string tooltip = null, float textpart = 0.75f, float boxpart = 0.25f) where T : struct
		{
			TextFieldNumericLabeled<T>(listing.GetRect(Text.LineHeight), label, ref val, ref buffer, min, max, tooltip, textpart, boxpart);
			listing.Gap(listing.verticalSpacing);
		}
		public static void TextFieldNumericLabeled<T>(Rect rect, string label, ref T val, ref string buffer, float min = 0f, float max = 1E+09f, string tooltip = null, float textpart = 0.75f, float boxpart = 0.25f) where T : struct
		{
			Rect rect2 = rect.LeftPart(textpart).Rounded();
			Rect rect3 = rect.RightPart(boxpart).Rounded();
			TextAnchor anchor = Text.Anchor;
			Text.Anchor = TextAnchor.MiddleLeft;
			Widgets.Label(rect2, label);
			if (tooltip != null)
			{
				TooltipHandler.TipRegion(rect2, tooltip);
			}
			Text.Anchor = anchor;
			Widgets.TextFieldNumeric(rect3, ref val, ref buffer, min, max);
		}
	}
}
