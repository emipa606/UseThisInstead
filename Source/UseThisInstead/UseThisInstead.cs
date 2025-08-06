using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using HarmonyLib;
using Steamworks;
using UnityEngine;
using UnityEngine.Networking;
using Verse;

namespace UseThisInstead;

[StaticConstructorOnStartup]
public static class UseThisInstead
{
    public static Vector2 ScrollPosition;
    private static Dictionary<ulong, ModReplacement> modReplacements = [];
    public static List<ModReplacement> FoundModReplacements;
    private static bool scanning;
    public static bool Replacing;
    public static bool ActivityMonitor;
    public static bool AnythingChanged;
    public static List<string> StatusMessages;
    public static bool? UsingLatestVersion;

    private static readonly Uri versionUri =
        new("https://raw.githubusercontent.com/emipa606/UseThisInstead/main/About/Manifest.xml");

    static UseThisInstead()
    {
        FoundModReplacements = [];
        StatusMessages = [];
        new Harmony("Mlie.UseThisInstead").PatchAll(Assembly.GetExecutingAssembly());
        ThreadPool.QueueUserWorkItem(_ => checkLatestVersionAsync());
    }

    public static bool IsVersionCheckComplete { get; private set; }

    private static void checkLatestVersionAsync()
    {
        try
        {
            using var webRequest = UnityWebRequest.Get(versionUri.ToString());
            webRequest.SendWebRequest();
            while (!webRequest.isDone)
            {
                Thread.Sleep(100);
            }

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                var manifestDocument = new XmlDocument();
                manifestDocument.LoadXml(webRequest.downloadHandler.text);
                var currentVersion = UseThisInsteadMod.CurrentVersion;
                UsingLatestVersion =
                    currentVersion == manifestDocument.SelectSingleNode("/Manifest/version")?.InnerText;
                if (UsingLatestVersion == true)
                {
                    logMessage($"Using latest version: {currentVersion}");
                }
                else
                {
                    logMessage(
                        $"You are not using the latest version of the mod: {manifestDocument.SelectSingleNode("/Manifest/version")?.InnerText}{Environment.NewLine}Suggestions may be out of date!",
                        warning: true);
                }
            }
        }
        catch (Exception e)
        {
            logMessage($"Failed to check for latest version: {e}");
        }
        finally
        {
            IsVersionCheckComplete = true;
        }
    }

    public static void CheckForReplacements(bool noWait = false)
    {
        if (scanning)
        {
            return;
        }

        if (noWait)
        {
            checkForReplacements();
            return;
        }

        scanning = true;
        new Thread(() =>
        {
            Thread.CurrentThread.IsBackground = true;
            checkForReplacements();
        }).Start();
    }

    private static void checkForReplacements()
    {
        if (!modReplacements.Any())
        {
            loadAllReplacementFiles();
        }

        FoundModReplacements = [];
        var modsToCheck = ModLister.AllInstalledMods;
        if (!UseThisInsteadMod.Instance.Settings.AllMods)
        {
            modsToCheck = ModsConfig.ActiveModsInLoadOrder;
        }

        foreach (var mod in modsToCheck)
        {
            if (mod.Official)
            {
                continue;
            }

            var publishedFileId = mod.GetPublishedFileId();
            if (publishedFileId == PublishedFileId_t.Invalid)
            {
                logMessage($"Ignoring {mod.Name} since its a local mod and does not have a steam PublishedFileId");
                continue;
            }


            if (!modReplacements.TryGetValue(publishedFileId.m_PublishedFileId, out var replacement))
            {
                continue;
            }

            var replacementPublishedFileId = replacement.GetReplacementPublishedFileId();
            if (!mod.Active &&
                ModLister.AllInstalledMods.Any(data => data.GetPublishedFileId() == replacementPublishedFileId))
            {
                logMessage($"Ignoring {mod.Name} since its not active and its replacement is also downloaded");
                continue;
            }

            if (UseThisInsteadMod.Instance.Settings.OnlyRelevant && !replacement.ReplacementSupportsVersion())
            {
                logMessage($"Ignoring {mod.Name} since it does not support this version of RimWorld");
                continue;
            }

            replacement.ModMetaData = mod;
            FoundModReplacements.Add(replacement);
        }

        FoundModReplacements = FoundModReplacements.OrderBy(replacement => replacement.ModName).ToList();

        logMessage($"Found {FoundModReplacements.Count} replacements", true);

        scanning = false;
    }

    private static void loadAllReplacementFiles()
    {
        modReplacements = [];

        var replacementFiles = Directory.GetFiles(UseThisInsteadMod.ReplacementsFolderPath, "*.xml");
        foreach (var replacementFile in replacementFiles)
        {
            using var streamReader = new StreamReader(replacementFile);
            var xml = streamReader.ReadToEnd();
            try
            {
                var serializer = new XmlSerializer(typeof(ModReplacement));
                var replacement = (ModReplacement)serializer.Deserialize(new StringReader(xml));
                modReplacements[replacement.SteamId] = replacement;
            }
            catch (Exception exception)
            {
                logMessage($"Failed to parse xml for {replacementFile}: {exception}", warning: true);
            }
        }

        logMessage($"Loaded {modReplacements.Count} possible replacements");
    }

    public static void ReplaceMods(List<ModReplacement> replacements)
    {
        if (Replacing)
        {
            return;
        }

        Replacing = true;
        StatusMessages = [];
        var counter = 0;
        foreach (var modReplacement in replacements)
        {
            counter++;
            addToStatusMessage("UTI.replacing".Translate(modReplacement.ModName, counter, replacements.Count));
            var justReplace = !modReplacement.ModMetaData.Active ||
                              modReplacement.ReplacementModId == modReplacement.ModId;
            if (!justReplace)
            {
                ModsConfig.SetActive(modReplacement.ModId, false);
            }

            if (!unSubscribeToMod(modReplacement.ModMetaData, modReplacement.ModName))
            {
                continue;
            }

            Thread.Sleep(10);

            if (!subscribeToMod(modReplacement.GetReplacementPublishedFileId(), modReplacement.ReplacementName))
            {
                continue;
            }

            Thread.Sleep(10);

            var installedMods = ModLister.AllInstalledMods.ToList();
            var subscribedMod = installedMods.FirstOrDefault(data =>
                data.GetPublishedFileId() == modReplacement.GetReplacementPublishedFileId());

            if (subscribedMod == null)
            {
                Replacing = false;
                continue;
            }

            var requirements = subscribedMod.GetRequirements();
            List<string> requirementIds = [];
            var modRequirements = requirements as ModRequirement[] ?? requirements.ToArray();
            if (modRequirements.Any() && modRequirements.Any(requirement => !requirement.IsSatisfied))
            {
                addToStatusMessage("UTI.checkingRequirements".Translate());

                foreach (var modRequirement in modRequirements.Where(requirement => !requirement.IsSatisfied))
                {
                    if (modRequirement is ModDependency dependency)
                    {
                        var match = Regex.Match(dependency.steamWorkshopUrl, @"\d+$");
                        if (!match.Success)
                        {
                            addToStatusMessage("UTI.failedToSubscribe".Translate(dependency.displayName,
                                dependency.steamWorkshopUrl));
                            Replacing = false;
                            continue;
                        }

                        var modId = ulong.Parse(match.Value);
                        if (subscribeToMod(new PublishedFileId_t(modId), dependency.displayName))
                        {
                            requirementIds.Add(dependency.packageId);
                            continue;
                        }

                        Replacing = false;
                        continue;
                    }

                    if (!justReplace && modRequirement is ModIncompatibility incompatibility)
                    {
                        addToStatusMessage("UTI.incompatibility".Translate(subscribedMod.Name,
                            incompatibility.displayName));
                    }
                }
            }

            if (justReplace)
            {
                continue;
            }

            addToStatusMessage("UTI.activatingMods".Translate());
            foreach (var requirementId in requirementIds)
            {
                if (ModLister.GetActiveModWithIdentifier(requirementId, true) == null)
                {
                    ModsConfig.SetActive(requirementId, true);
                }
            }

            ModsConfig.SetActive(modReplacement.ReplacementModId, true);
        }

        Replacing = false;
    }

    private static bool unSubscribeToMod(ModMetaData modMetaData, string modName)
    {
        if (!modMetaData.OnSteamWorkshop)
        {
            addToStatusMessage("UTI.cantUnsubscribe".Translate(modName));
            return true;
        }

        var installedMods = ModLister.AllInstalledMods.ToList();
        var modId = modMetaData.GetPublishedFileId();
        var unsubscribedMod = installedMods.FirstOrDefault(data => data.GetPublishedFileId() == modId);

        if (unsubscribedMod == null)
        {
            return true;
        }

        addToStatusMessage("UTI.unsubscribing".Translate(modName, modId.m_PublishedFileId));
        SteamUGC.UnsubscribeItem(modId);
        var isSubscribed = isSubscribedToMod(modId);
        var counter = 0;
        addToStatusMessage("UTI.waitingUnsub".Translate());
        while (isSubscribed)
        {
            counter++;
            if (counter > 120)
            {
                break;
            }

            Thread.Sleep(500);
            ActivityMonitor = !ActivityMonitor;
            isSubscribed = isSubscribedToMod(modId);
        }

        if (isSubscribedToMod(modId))
        {
            addToStatusMessage("UTI.failedToUnsubscribe".Translate(modName, modId.m_PublishedFileId));
            Replacing = false;
            return false;
        }

        addToStatusMessage("UTI.unsubscribed".Translate(modName));
        return true;
    }

    private static bool isSubscribedToMod(PublishedFileId_t modId)
    {
        var state = (EItemState)SteamUGC.GetItemState(modId);
        return (state & EItemState.k_EItemStateSubscribed) != 0;
    }

    private static bool subscribeToMod(PublishedFileId_t modId, string modName)
    {
        var installedMods = ModLister.AllInstalledMods.ToList();
        var subscribedMod = installedMods.FirstOrDefault(data => data.GetPublishedFileId() == modId);
        if (subscribedMod != null)
        {
            return true;
        }

        addToStatusMessage("UTI.subscribing".Translate(modName, modId.m_PublishedFileId));
        SteamUGC.SubscribeItem(modId);

        var counter = 0;
        var isSubscribed = isSubscribedToMod(modId);
        addToStatusMessage("UTI.waitingSub".Translate());
        while (!isSubscribed)
        {
            counter++;
            if (counter > 120)
            {
                break;
            }

            Thread.Sleep(500);
            ActivityMonitor = !ActivityMonitor;
            isSubscribed = isSubscribedToMod(modId);
        }

        if (!isSubscribedToMod(modId))
        {
            addToStatusMessage("UTI.failedToSubscribe".Translate(modName, modId.m_PublishedFileId));
            Replacing = false;
            return false;
        }

        addToStatusMessage("UTI.subscribed".Translate(modName));
        return true;
    }

    private static void addToStatusMessage(string message)
    {
        StatusMessages.Add(message);
        logMessage(message, true);
    }

    private static void logMessage(string message, bool force = false, bool warning = false)
    {
        if (warning)
        {
            Log.Warning($"[UseThisInstead]: {message}");
            return;
        }

        if (!force && !UseThisInsteadMod.Instance.Settings.VeboseLogging)
        {
            return;
        }

        Log.Message($"[UseThisInstead]: {message}");
    }
}