using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Steam;

namespace UseThisInstead;

[StaticConstructorOnStartup]
public class Dialog_ModReplacements : Window
{
    private const int headerHeight = 50;
    private const int rowHeight = 60;
    private static Vector2 scrollPosition;
    private static readonly Color alternateBackground = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    private static readonly Vector2 previewImage = new Vector2(120f, 100f);
    private static readonly Vector2 buttonSize = new Vector2(140f, 25f);
    private static readonly Texture2D ArrowTex = ContentFinder<Texture2D>.Get("UI/Overlays/TutorArrowRight");
    private static readonly Texture2D steamIcon = ContentFinder<Texture2D>.Get("UI/Steam");
    private static readonly Texture2D folderIcon = ContentFinder<Texture2D>.Get("UI/Folder");
    private readonly List<ModReplacement> selectedReplacements = [];

    public Dialog_ModReplacements()
    {
        doCloseX = true;
        forcePause = true;
        absorbInputAroundWindow = true;
    }

    public override Vector2 InitialSize => new Vector2(700f, 700f);

    public override void Close(bool doCloseSound = true)
    {
        if (UseThisInstead.Replacing)
        {
            return;
        }

        base.Close(doCloseSound);
    }

    public override void PostClose()
    {
        base.PostClose();
        if (!UseThisInstead.AnythingChanged)
        {
            return;
        }

        Messages.Message("UTI.activeModsChanged".Translate(), MessageTypeDefOf.NeutralEvent);
        Find.WindowStack.Add(new Page_ModsConfig());
    }

    public override void DoWindowContents(Rect inRect)
    {
        var listingStandard = new Listing_Standard();
        listingStandard.Begin(inRect);
        Text.Font = GameFont.Medium;

        listingStandard.Label("UTI.foundReplacements".Translate(UseThisInstead.FoundModReplacements.Count));

        Text.Font = GameFont.Small;
        if (UseThisInstead.AnythingChanged)
        {
            listingStandard.Label("UTI.restartNeeded".Translate());
        }
        else
        {
            listingStandard.Gap();
        }

        Rect subtitleRect;
        if (!UseThisInstead.Replacing)
        {
            if (SteamManager.Initialized)
            {
                listingStandard.CheckboxLabeled("UTI.preferOverlay".Translate(),
                    ref UseThisInsteadMod.instance.Settings.PreferOverlay,
                    "UTI.preferOverlaytt".Translate());
            }

            var settingChanged = false;
            var originalSetting = UseThisInsteadMod.instance.Settings.OnlyRelevant;
            listingStandard.CheckboxLabeled("UTI.onlyRelevant".Translate(),
                ref UseThisInsteadMod.instance.Settings.OnlyRelevant,
                "UTI.onlyRelevanttt".Translate());

            if (originalSetting != UseThisInsteadMod.instance.Settings.OnlyRelevant)
            {
                settingChanged = true;
            }

            originalSetting = UseThisInsteadMod.instance.Settings.AllMods;
            listingStandard.CheckboxLabeled("UTI.allMods".Translate(), ref UseThisInsteadMod.instance.Settings.AllMods,
                "UTI.allModstt".Translate());
            subtitleRect = listingStandard.GetRect(0);
            if (originalSetting != UseThisInsteadMod.instance.Settings.AllMods)
            {
                settingChanged = true;
            }

            if (settingChanged)
            {
                UseThisInsteadMod.instance.WriteSettingsOnly();
                UseThisInstead.CheckForReplacements(true);
            }
        }
        else
        {
            listingStandard.Label(UseThisInsteadMod.instance.Settings.OnlyRelevant
                ? "UTI.showingRelevant".Translate()
                : "UTI.showingAll".Translate());
            subtitleRect = listingStandard.Label(UseThisInsteadMod.instance.Settings.AllMods
                ? "UTI.checkingAll".Translate()
                : "UTI.checkingEnabled".Translate());
        }

        var buttonRect = listingStandard.GetRect(30f);

        for (var i = 0; i < selectedReplacements.Count; i++)
        {
            var replacement = selectedReplacements[i];
            if (UseThisInstead.FoundModReplacements.Contains(replacement))
            {
                continue;
            }

            selectedReplacements.Remove(replacement);
        }

        if (Widgets.ButtonText(buttonRect.LeftHalf().ContractedBy(5, 0),
                selectedReplacements.Any() ? "UTI.selectNone".Translate() : "UTI.selectAll".Translate(),
                active: UseThisInstead.FoundModReplacements.Any()))
        {
            if (selectedReplacements.Any())
            {
                selectedReplacements.Clear();
            }
            else
            {
                selectedReplacements.AddRange(
                    UseThisInstead.FoundModReplacements.Where(replacement => replacement.ReplacementSupportsVersion()));
            }
        }

        if (Widgets.ButtonText(buttonRect.RightHalf().ContractedBy(5, 0),
                "UTI.updateSelected".Translate(selectedReplacements.Count),
                active: selectedReplacements.Any()))
        {
            var replaceModString = "UTI.replaceMultipleMods";
            if (selectedReplacements.Any(replacement => replacement.ModMetaData.Active))
            {
                replaceModString += "Active";
            }

            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                replaceModString.Translate(selectedReplacements.Count),
                delegate
                {
                    if (selectedReplacements.Any(replacement => replacement.ModMetaData.Active))
                    {
                        UseThisInstead.AnythingChanged = true;
                    }

                    new Thread(() =>
                    {
                        Thread.CurrentThread.IsBackground = true;
                        UseThisInstead.ReplaceMods(selectedReplacements);
                    }).Start();
                    Find.WindowStack.Add(new ReplacementStatus_Window());
                }));
        }

        listingStandard.End();

        var borderRect = inRect;
        borderRect.y += subtitleRect.y + headerHeight;
        borderRect.height -= subtitleRect.y + headerHeight;
        var scrollContentRect = inRect;
        scrollContentRect.height = UseThisInstead.FoundModReplacements.Count * (rowHeight + 1);

        scrollContentRect.width -= 20;
        scrollContentRect.x = 0;
        scrollContentRect.y = 0;

        var scrollListing = new Listing_Standard();
        Widgets.BeginScrollView(borderRect, ref scrollPosition, scrollContentRect);
        scrollListing.Begin(scrollContentRect);

        var alternate = false;
        foreach (var modInfo in UseThisInstead.FoundModReplacements)
        {
            var rowRectFull = scrollListing.GetRect(rowHeight);
            alternate = !alternate;
            if (alternate)
            {
                Widgets.DrawBoxSolid(rowRectFull, alternateBackground);
            }

            var rowRectLeft = rowRectFull.ContractedBy(5f).LeftPart(0.9f);
            var rowRectRight = rowRectFull.ContractedBy(5f).RightPart(0.1f);

            var modInfoRect = rowRectLeft.RightPartPixels(rowRectLeft.width - previewImage.x - 5f);

            var leftModRect = modInfoRect.LeftHalf().LeftPartPixels(modInfoRect.LeftHalf().width - (buttonSize.y / 2));
            var rightModRect = modInfoRect.RightHalf()
                .RightPartPixels(modInfoRect.LeftHalf().width - (buttonSize.y / 2));
            var spacerRect = modInfoRect.LeftHalf();
            spacerRect.width = buttonSize.y;
            spacerRect.x = leftModRect.xMax;
            spacerRect = spacerRect.ContractedBy(5);
            var previewRect = rowRectLeft.LeftPartPixels(previewImage.x);

            if (modInfo.ModMetaData.PreviewImage != null)
            {
                Widgets.DrawBoxSolid(previewRect.ContractedBy(1f), Color.black);
                Widgets.DrawTextureFitted(previewRect.ContractedBy(1f), modInfo.ModMetaData.PreviewImage, 1f);
            }

            Widgets.Label(leftModRect.TopHalf(), modInfo.ModName);
            Widgets.Label(leftModRect.BottomHalf(), $"{"UTI.author".Translate()}{modInfo.Author}");
            TooltipHandler.TipRegion(leftModRect, modInfo.SteamUri(true).AbsoluteUri);
            Widgets.DrawHighlightIfMouseover(leftModRect);
            if (Widgets.ButtonInvisible(leftModRect))
            {
                if (UseThisInsteadMod.instance.Settings.PreferOverlay)
                {
                    SteamUtility.OpenUrl(modInfo.SteamUri(true).AbsoluteUri);
                }
                else
                {
                    Application.OpenURL(modInfo.SteamUri(true).AbsoluteUri);
                }
            }

            Widgets.DrawTextureFitted(spacerRect, ArrowTex, 1f);

            if (UseThisInstead.Replacing)
            {
                Widgets.DrawTextureFitted(rowRectRight, TexButton.SpeedButtonTextures[0], 1f);
                TooltipHandler.TipRegion(rowRectRight, "UTI.alreadyReplacing".Translate());
            }

            var originalColor = GUI.color;
            if (!modInfo.ReplacementSupportsVersion())
            {
                GUI.color = Color.red;
            }
            else if (!UseThisInstead.Replacing)
            {
                TooltipHandler.TipRegion(rowRectRight, "UTI.replace".Translate());
                var selected = selectedReplacements.Contains(modInfo);
                var selectedOriginally = selected;
                Widgets.Checkbox(rowRectRight.position, ref selected);
                if (selectedOriginally != selected)
                {
                    if (selected)
                    {
                        selectedReplacements.Add(modInfo);
                    }
                    else
                    {
                        selectedReplacements.Remove(modInfo);
                    }
                }
            }

            Widgets.Label(rightModRect.TopHalf(), modInfo.ReplacementName);
            Widgets.Label(rightModRect.BottomHalf(), $"{"UTI.author".Translate()}{modInfo.ReplacementAuthor}");
            TooltipHandler.TipRegion(rightModRect, modInfo.SteamUri().AbsoluteUri);
            if (!modInfo.ReplacementSupportsVersion())
            {
                GUI.color = originalColor;
                TooltipHandler.TipRegion(rightModRect,
                    "UTI.notUpdated".Translate(VersionControl.CurrentVersionStringWithoutBuild));
            }

            Widgets.DrawHighlightIfMouseover(rightModRect);
            if (Widgets.ButtonInvisible(rightModRect))
            {
                if (UseThisInsteadMod.instance.Settings.PreferOverlay)
                {
                    SteamUtility.OpenUrl(modInfo.SteamUri().AbsoluteUri);
                }
                else
                {
                    Application.OpenURL(modInfo.SteamUri().AbsoluteUri);
                }
            }

            if (modInfo.ModMetaData.OnSteamWorkshop)
            {
                Widgets.DrawTextureFitted(previewRect.BottomHalf().LeftHalf().LeftHalf(), steamIcon, 1f);
                TooltipHandler.TipRegion(previewRect.BottomHalf().LeftHalf().LeftHalf(), "SMU.SteamMod".Translate());
                continue;
            }

            Widgets.DrawTextureFitted(previewRect.BottomHalf().LeftHalf().LeftHalf(), folderIcon, 1f);
            TooltipHandler.TipRegion(previewRect.BottomHalf().LeftHalf().LeftHalf(), "SMU.LocalMod".Translate());
        }

        scrollListing.End();
        Widgets.EndScrollView();
    }
}