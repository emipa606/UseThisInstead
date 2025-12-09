using System;
using System.Linq;
using RimWorld;
using Steamworks;
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

    public ModMetaData ModMetaData { get; set; }

    public ulong GetNewPublishedFileId()
    {
        return Convert.ToUInt64(newWorkshopId);
    }

    public ulong GetOldPublishedFileId()
    {
        return Convert.ToUInt64(oldWorkshopId);
    }

    public PublishedFileId_t GetReplacementPublishedFileId()
    {
        return new PublishedFileId_t(GetNewPublishedFileId());
    }

    public bool ReplacementSupportsVersion()
    {
        return newVersions != null &&
               newVersions.Any(versionString => VersionControl.CurrentVersionStringWithoutBuild == versionString);
    }

    public override string ToString()
    {
        var returnString =
            $"oldName: {oldName}, oldAuthor: {oldAuthor}, oldPackageId: {oldPackageId}, oldWorkshopId: {oldWorkshopId}, oldVersions: {string.Join(",", oldVersions)}, " +
            $"newName: {newName}, newAuthor: {newAuthor}, newPackageId: {newPackageId}, newWorkshopId: {newWorkshopId}, newVersions: {string.Join(",", newVersions)}";
        return returnString;
    }

    public Uri SteamUri(bool old = false)
    {
        if (old)
        {
            return !string.IsNullOrEmpty(oldWorkshopId) ? new Uri(SteamPrefix, oldWorkshopId) : null;
        }

        return !string.IsNullOrEmpty(newWorkshopId) ? new Uri(SteamPrefix, newWorkshopId) : null;
    }
}