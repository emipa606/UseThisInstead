using HarmonyLib;
using UnityEngine;
using Verse;

namespace UseThisInstead;

[HarmonyPatch(typeof(Widgets), nameof(Widgets.ButtonText), typeof(Rect), typeof(string), typeof(bool), typeof(bool),
    typeof(bool), typeof(TextAnchor))]
[HarmonyBefore("Mlie.ShowModUpdates")]
public static class Widgets_ButtonText_Postfix
{
    public static void Postfix(ref Rect rect, string label)
    {
        if (UseThisInstead.AnythingChanged && !Find.WindowStack.AnyWindowAbsorbingAllInput)
        {
            ModsConfig.Save();
            ModsConfig.RestartFromChangedMods();
        }

        if (label != LanguageDatabase.activeLanguage.FriendlyNameNative ||
            !UseThisInsteadMod.Instance.Settings.AlwaysShow && !UseThisInstead.FoundModReplacements.Any())
        {
            return;
        }

        if (Find.WindowStack.AnyWindowAbsorbingAllInput)
        {
            return;
        }

        var newRect = rect;
        newRect.y += rect.height + 5f;
        rect.y += rect.height + 5f;
        if (Widgets.ButtonText(newRect, "UTI.replacements".Translate(UseThisInstead.FoundModReplacements.Count)))
        {
            Find.WindowStack.Add(new Dialog_ModReplacements());
        }
    }
}