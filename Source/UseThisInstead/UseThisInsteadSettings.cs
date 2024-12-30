using Verse;

namespace UseThisInstead;

/// <summary>
///     Definition of the settings for the mod
/// </summary>
internal class UseThisInsteadSettings : ModSettings
{
    public bool AllMods;
    public bool VeboseLogging;

    /// <summary>
    ///     Saving and loading the values
    /// </summary>
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref AllMods, "AllMods");
        Scribe_Values.Look(ref VeboseLogging, "VeboseLogging");
    }
}