using System.IO;
using Mlie;
using UnityEngine;
using Verse;
using Verse.Steam;

namespace UseThisInstead;

[StaticConstructorOnStartup]
internal class UseThisInsteadMod : Mod
{
    /// <summary>
    ///     The instance of the settings to be read by the mod
    /// </summary>
    public static UseThisInsteadMod Instance;

    public static string CurrentVersion;

    public string LastVersion
    {
        get { 
            return Settings.LastVersion; 
        }
        set
        {
            Settings.LastVersion = value;
            WriteSettingsOnly();
        }
    }

    public string LastAlternateVersion
    {
        get
        {
            return Settings.LastAlternateVersion;
        }
        set
        {
            Settings.LastAlternateVersion = value;
            WriteSettingsOnly();
        }
    }

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="content"></param>
    public UseThisInsteadMod(ModContentPack content) : base(content)
    {
        Instance = this;
        Settings = GetSettings<UseThisInsteadSettings>();
        CurrentVersion = VersionFromManifest.GetVersionFromModMetaData(content.ModMetaData);
    }

    /// <summary>
    ///     The instance-settings for the mod
    /// </summary>
    internal UseThisInsteadSettings Settings { get; }

    /// <summary>
    ///     The title for the mod-settings
    /// </summary>
    /// <returns></returns>
    public override string SettingsCategory()
    {
        return "Use This Instead";
    }

    /// <summary>
    ///     The settings-window
    ///     For more info: https://rimworldwiki.com/wiki/Modding_Tutorials/ModSettings
    /// </summary>
    /// <param name="rect"></param>
    public override void DoSettingsWindowContents(Rect rect)
    {
        var listingStandard = new Listing_Standard();
        listingStandard.Begin(rect);
        listingStandard.Gap();

        if (listingStandard.ButtonText("UTI.replacements".Translate(UseThisInstead.FoundModReplacements.Count),
                widthPct: 0.5f))
        {
            Find.WindowStack.Add(new Dialog_ModReplacements());
        }

        listingStandard.CheckboxLabeled("UTI.alwaysShow".Translate(), ref Settings.AlwaysShow,
            "UTI.alwaysShowtt".Translate());
        listingStandard.CheckboxLabeled("UTI.allMods".Translate(), ref Settings.AllMods,
            "UTI.allModstt".Translate());
        listingStandard.CheckboxLabeled("UTI.onlyRelevant".Translate(), ref Settings.OnlyRelevant,
            "UTI.onlyRelevanttt".Translate());
        if (SteamManager.Initialized)
        {
            listingStandard.CheckboxLabeled("UTI.preferOverlay".Translate(), ref Settings.PreferOverlay,
                "UTI.preferOverlaytt".Translate());
        }

        listingStandard.CheckboxLabeled("UTI.veboseLogging".Translate(), ref Settings.VeboseLogging,
            "UTI.veboseLoggingtt".Translate());
        if (CurrentVersion != null)
        {
            listingStandard.Gap();
            GUI.contentColor = Color.gray;
            listingStandard.Label("UTI.modVersion".Translate(CurrentVersion));
            GUI.contentColor = Color.white;
        }

        listingStandard.End();
    }

    public override void WriteSettings()
    {
        base.WriteSettings();
        UseThisInstead.CheckForReplacements();
    }

    public void WriteSettingsOnly()
    {
        base.WriteSettings();
    }
}