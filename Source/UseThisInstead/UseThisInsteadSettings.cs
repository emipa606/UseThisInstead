using Verse;

namespace UseThisInstead;

/// <summary>
///     Definition of the settings for the mod
/// </summary>
internal class UseThisInsteadSettings : ModSettings
{
    public bool AllMods;
    public bool AlwaysShow;
    public bool OnlyRelevant;
    public bool PreferOverlay;
    public bool VeboseLogging;
    public string LastVersion;
    public string LastAlternateVersion;

    /// <summary>
    ///     Saving and loading the values
    /// </summary>
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref AllMods, "AllMods");
        Scribe_Values.Look(ref LastVersion, "LastVersion");
        Scribe_Values.Look(ref LastAlternateVersion, "LastAlternateVersion");
        Scribe_Values.Look(ref PreferOverlay, "PreferOverlay");
        Scribe_Values.Look(ref AlwaysShow, "AlwaysShow");
        Scribe_Values.Look(ref OnlyRelevant, "OnlyRelevant");
        Scribe_Values.Look(ref VeboseLogging, "VeboseLogging");
    }
}