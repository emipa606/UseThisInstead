using HarmonyLib;
using RimWorld;

namespace UseThisInstead;

[HarmonyPatch(typeof(Page_ModsConfig), "RecacheSelectedModInfo")]
public static class Page_ModsConfig_RecacheSelectedModInfo
{
    public static bool Prefix() { return !UseThisInstead.Replacing; }
}