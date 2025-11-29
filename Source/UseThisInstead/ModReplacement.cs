using RimWorld;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace UseThisInstead;

[Serializable]
public class ModReplacement
{
    private static readonly Uri SteamPrefix = new("https://steamcommunity.com/sharedfiles/filedetails/?id=");
    public string newAuthor;
    public string newName;
    public string newPackageId;
    public string newWorkshopId;
    public string[] newVersions;
    public string oldAuthor;
    public string oldName;
    public string oldPackageId;
    public string oldWorkshopId;
    public string[] oldVersions;

    public ulong GetNewPublishedFileId() { return Convert.ToUInt64(newWorkshopId); }

    public ulong GetOldPublishedFileId() { return Convert.ToUInt64(oldWorkshopId); }

    public PublishedFileId_t GetReplacementPublishedFileId() { return new PublishedFileId_t(GetNewPublishedFileId()); }

    public bool ReplacementSupportsVersion()
    {
        if (newVersions == null)
        {
            return false;
        }

        return newVersions.Any(versionString => VersionControl.CurrentVersionStringWithoutBuild == versionString);
    }


    public Uri SteamUri(bool old = false)
    {
        if (old)
        {
            return !string.IsNullOrEmpty(oldWorkshopId) ? new Uri(SteamPrefix, oldWorkshopId) : null;
        }

        return !string.IsNullOrEmpty(newWorkshopId) ? new Uri(SteamPrefix, newWorkshopId) : null;
    }

    public ModMetaData ModMetaData { get; set; }
}
