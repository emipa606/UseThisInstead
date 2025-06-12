using System;
using System.Linq;
using System.Xml.Serialization;
using RimWorld;
using Steamworks;
using Verse;

namespace UseThisInstead;

public class ModReplacement
{
    private static readonly Uri SteamPrefix = new("https://steamcommunity.com/sharedfiles/filedetails/?id=");

    [XmlIgnore] public ModMetaData ModMetaData { get; set; }

    public string Author { get; set; }
    public string ModId { get; set; }
    public string Versions { get; set; }

    public string ModName { get; set; }
    public string ReplacementAuthor { get; set; }
    public string ReplacementModId { get; set; }
    public string ReplacementVersions { get; set; }

    public string ReplacementName { get; set; }
    public ulong ReplacementSteamId { get; set; }
    public ulong SteamId { get; set; }

    public Uri SteamUri(bool old = false)
    {
        if (old)
        {
            return !string.IsNullOrEmpty(SteamId.ToString()) ? new Uri(SteamPrefix, SteamId.ToString()) : null;
        }

        return !string.IsNullOrEmpty(ReplacementSteamId.ToString())
            ? new Uri(SteamPrefix, ReplacementSteamId.ToString())
            : null;
    }

    public PublishedFileId_t GetReplacementPublishedFileId()
    {
        return new PublishedFileId_t(ReplacementSteamId);
    }

    public bool ReplacementSupportsVersion()
    {
        return ReplacementVersions.Split(',')
            .Any(versionString => VersionControl.CurrentVersionStringWithoutBuild == versionString);
    }
}