using System.Linq;
using UnityEngine;
using Verse;

namespace UseThisInstead;

[StaticConstructorOnStartup]
public class ReplacementStatus_Window : Window
{
    public ReplacementStatus_Window()
    {
        doCloseX = true;
        forcePause = true;
        absorbInputAroundWindow = true;
    }

    public override void Close(bool doCloseSound = true)
    {
        if (UseThisInstead.Replacing)
        {
            Find.WindowStack
                .Add(
                    Dialog_MessageBox.CreateConfirmation(
                        "UTI.reallyAbort".Translate(),
                        delegate
                        {
                            UseThisInstead.Replacing = false;
                            Close(doCloseSound);
                        }));
            return;
        }

        UseThisInstead.CheckForReplacements(true);
        base.Close(doCloseSound);
    }

    public override void DoWindowContents(Rect inRect)
    {
        if (UseThisInstead.StatusMessages == null || !UseThisInstead.StatusMessages.Any())
        {
            return;
        }

        var statusMessages = UseThisInstead.StatusMessages.ToList();
        statusMessages.Reverse();
        var listingStandard = new Listing_Standard();
        listingStandard.Begin(inRect);
        Text.Font = GameFont.Medium;
        listingStandard.Label("UTI.replacementStatus".Translate());
        Text.Font = GameFont.Small;
        listingStandard.Gap();
        listingStandard.End();
        var outRect = inRect;
        outRect.yMin += listingStandard.CurHeight;
        var viewRect = outRect;
        viewRect.height = (statusMessages.Count + 1) * 30f;
        viewRect.width -= 16f;
        Widgets.BeginScrollView(outRect, ref UseThisInstead.ScrollPosition, viewRect);
        var innerListing = new Listing_Standard();
        innerListing.Begin(viewRect);
        if (UseThisInstead.Replacing)
        {
            innerListing.Label(UseThisInstead.ActivityMonitor ? ". . ." : " . . ");
        }
        else
        {
            var imageRect = innerListing.GetRect(50f);
            var lastStatus = statusMessages.Last();
            if (lastStatus == "UTI.failedToSubscribe".Translate() ||
                lastStatus == "UTI.failedToUnsubscribe".Translate())
            {
                Widgets.DrawTextureFitted(imageRect, Widgets.CheckboxOffTex, 1f);
            }
            else
            {
                Widgets.DrawTextureFitted(imageRect, Widgets.CheckboxOnTex, 1f);
            }

            innerListing.GapLine();
            if (innerListing.ButtonText("Close".Translate(), widthPct: 0.5f))
            {
                Close();
            }
        }

        innerListing.Gap();

        foreach (var statusMessage in statusMessages)
        {
            innerListing.Label(statusMessage);
        }

        innerListing.End();
        Widgets.EndScrollView();
    }
}