using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using HarmonyLib;
using MiniJSON;
using Steamworks;
using UnityEngine;
using UnityEngine.Networking;
using Verse;

namespace UseThisInstead;

[StaticConstructorOnStartup]
public static class UseThisInstead
{
    private static Dictionary<ulong, ModReplacement> modReplacements = [];

    private static readonly string replacementsPath = Path.Combine(
        UseThisInsteadMod.Instance.Content.RootDir,
        "replacements.json");

    private static readonly Uri replacementsUrl = new("https://data.litet.net/replacements.json.gz");

    private static readonly Uri alternateUrl =
        new("https://raw.githubusercontent.com/Mlie/UseThisInstead/main/replacements.json.gz");

    private static bool scanning;
    public static bool ActivityMonitor;
    public static bool AnythingChanged;
    public static List<ModReplacement> FoundModReplacements;
    public static bool Replacing;
    public static Vector2 ScrollPosition;
    public static List<string> StatusMessages;
    private static readonly FieldInfo modsFieldInfo = AccessTools.Field(typeof(ModLister), "mods");

    static UseThisInstead()
    {
        FoundModReplacements = [];
        StatusMessages = [];
        new Harmony("Mlie.UseThisInstead").PatchAll(Assembly.GetExecutingAssembly());
        ThreadPool.QueueUserWorkItem(_ => checkLatestVersionAsync());
    }

    public static bool IsVersionCheckComplete { get; private set; }

    private static void addToStatusMessage(string message)
    {
        StatusMessages.Add(message);
        logMessage(message, true);
    }

    private static void checkForReplacements()
    {
        if (!modReplacements.Any())
        {
            loadAllReplacements();
        }

        if (!modReplacements.Any())
        {
            scanning = false;
            return;
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

            while (true)
            {
                if (!modReplacements.TryGetValue(replacement.GetNewPublishedFileId(), out var newReplacement))
                {
                    break;
                }

                logMessage(
                    $"Replacement for {mod.Name} does not support the latest version of the game, but found a replacement for that mod as well.");
                replacement = newReplacement;
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

        FoundModReplacements = FoundModReplacements.OrderBy(replacement => replacement.oldName).ToList();

        logMessage($"Found {FoundModReplacements.Count} replacements", true);

        scanning = false;
    }

    private static void checkLatestVersionAsync()
    {
        try
        {
            var lastVersion = UseThisInsteadMod.Instance.LastVersion;
            if (!File.Exists(replacementsPath))
            {
                lastVersion = null;
            }

            foreach (var url in new[] { replacementsUrl, alternateUrl })
            {
                var request = UnityWebRequest.Get(url);

                if (!string.IsNullOrEmpty(lastVersion))
                {
                    request.SetRequestHeader("If-None-Match", lastVersion);
                }

                request.SendWebRequest();
                while (!request.isDone)
                {
                    Thread.Sleep(100);
                }

                if (request.responseCode == 304)
                {
                    logMessage($"Replacement database is up to date, checksum: {lastVersion}.");
                    break;
                }

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var newEtag = request.GetResponseHeader("ETag");
                    var json = decompressGzip(request.downloadHandler.data);
                    saveCached(replacementsPath, json);
                    logMessage($"Updated replacement database from checksum {lastVersion} to {newEtag}");
                    if (url == alternateUrl)
                    {
                        UseThisInsteadMod.Instance.LastAlternateVersion = newEtag;
                    }
                    else
                    {
                        UseThisInsteadMod.Instance.LastVersion = newEtag;
                    }

                    break;
                }

                logMessage($"Failed to update replacement database from {url}: {request.error}");
                lastVersion = UseThisInsteadMod.Instance.LastAlternateVersion;
                if (!File.Exists(replacementsPath))
                {
                    lastVersion = null;
                }
            }

            static void saveCached(string file, string content)
            {
                File.WriteAllText(file, content);
            }
        }
        catch (Exception e)
        {
            logMessage($"Failed to check for latest version: {e}");
        }
        finally
        {
            IsVersionCheckComplete = true;
            CheckForReplacements();
        }
    }

    private static string decompressGzip(byte[] compressed)
    {
        using var ms = new MemoryStream(compressed);
        using var gz = new GZipStream(ms, CompressionMode.Decompress);
        using var sr = new StreamReader(gz, Encoding.UTF8);
        return sr.ReadToEnd();
    }

    private static bool isSubscribedToMod(PublishedFileId_t modId)
    {
        var state = (EItemState)SteamUGC.GetItemState(modId);
        return (state & EItemState.k_EItemStateSubscribed) != 0;
    }


    private static void loadAllReplacements()
    {
        modReplacements = [];

        if (!File.Exists(replacementsPath))
        {
            logMessage($"No replacement database found at {replacementsPath}, skipping loading replacements");
            return;
        }

        logMessage($"Loading replacement database from {replacementsPath}");
        try
        {
            var json = File.ReadAllText(replacementsPath);

            if (string.IsNullOrWhiteSpace(json) || !json.TrimStart().StartsWith("{"))
            {
                logMessage("replacements.json does not start with '{' — malformed JSON?", warning: true);
                return;
            }

            if (Json.Deserialize(json) is not Dictionary<string, object> root ||
                !root.TryGetValue("rules", out var value))
            {
                logMessage("Failed to parse replacements.json: rules missing", warning: true);
                return;
            }

            if (value is List<object> rules)
            {
                foreach (var rObj in rules)
                {
                    if (rObj is not Dictionary<string, object> rDict)
                    {
                        logMessage($"Failed to parse replacement rule, invalid format: {rObj}", warning: true);
                        continue;
                    }

                    var mod = new ModReplacement
                    {
                        oldWorkshopId = rDict.GetValueOrDefault("oldWorkshopId") as string,
                        oldName = rDict.GetValueOrDefault("oldName") as string,
                        oldAuthor = rDict.GetValueOrDefault("oldAuthor") as string,
                        oldPackageId = rDict.GetValueOrDefault("oldPackageId") as string,

                        newWorkshopId = rDict.GetValueOrDefault("newWorkshopId") as string,
                        newName = rDict.GetValueOrDefault("newName") as string,
                        newAuthor = rDict.GetValueOrDefault("newAuthor") as string,
                        newPackageId = rDict.GetValueOrDefault("newPackageId") as string,

                        oldVersions = (rDict.GetValueOrDefault("oldVersions") as List<object>)
                            ?.Select(v => v.ToString())
                            .ToArray() ?? [],
                        newVersions = (rDict.GetValueOrDefault("newVersions") as List<object>)
                            ?.Select(v => v.ToString())
                            .ToArray() ?? []
                    };

                    if (!string.IsNullOrEmpty(mod.oldWorkshopId))
                    {
                        modReplacements[mod.GetOldPublishedFileId()] = mod;
                    }
                    else
                    {
                        logMessage($"Failed to parse replacement rule, oldWorkshopId is empty:\n{mod}",
                            warning: true);
                    }
                }
            }

            logMessage(
                $"Loaded {modReplacements.Count} possible replacements (generated {root.GetValueOrDefault("version")})");
        }
        catch (Exception exception)
        {
            logMessage($"Failed to parse replacements.json: {exception}", warning: true);
        }
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


        try
        {
            var currentMods = ModLister.AllInstalledMods;
            currentMods = currentMods.Where(data => data.GetPublishedFileId() != modId).ToList();
            modsFieldInfo.SetValue(null, currentMods.ToList());
        }
        catch
        {
            // Ignored
        }

        addToStatusMessage("UTI.unsubscribed".Translate(modName));
        return true;
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
            addToStatusMessage("UTI.replacing".Translate(modReplacement.oldName, counter, replacements.Count));
            var justReplace = !modReplacement.ModMetaData.Active ||
                              modReplacement.newWorkshopId == modReplacement.oldWorkshopId;
            if (!justReplace)
            {
                ModsConfig.SetActive(modReplacement.oldPackageId, false);
            }

            if (!unSubscribeToMod(modReplacement.ModMetaData, modReplacement.oldName))
            {
                continue;
            }

            Thread.Sleep(10);

            if (!subscribeToMod(modReplacement.GetReplacementPublishedFileId(), modReplacement.newName))
            {
                continue;
            }

            Thread.Sleep(10);

            var installedMods = ModLister.AllInstalledMods.ToList();
            var subscribedMod = installedMods.FirstOrDefault(data =>
                data.GetPublishedFileId() == modReplacement.GetReplacementPublishedFileId());

            if (subscribedMod == null)
            {
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
                            addToStatusMessage(
                                "UTI.failedToSubscribe".Translate(dependency.displayName, dependency.steamWorkshopUrl));
                            continue;
                        }

                        var modId = ulong.Parse(match.Value);
                        if (subscribeToMod(new PublishedFileId_t(modId), dependency.displayName))
                        {
                            requirementIds.Add(dependency.packageId);
                        }

                        continue;
                    }

                    if (!justReplace && modRequirement is ModIncompatibility incompatibility)
                    {
                        addToStatusMessage(
                            "UTI.incompatibility".Translate(subscribedMod.Name, incompatibility.displayName));
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

            ModsConfig.SetActive(modReplacement.newPackageId, true);
        }

        Replacing = false;
    }
}