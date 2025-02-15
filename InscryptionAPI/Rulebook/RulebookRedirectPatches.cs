using DiskCardGame;
using HarmonyLib;
using System.Collections;
using TMPro;
using UnityEngine;

namespace InscryptionAPI.RuleBook;

/* Outlining my manifesto here
 * So we want to create interactable objects that correspond to the dimensions and position of words in the rulebook
 * because god hates me, this isn't as easy as attaching objects to the book and then sizing them
 * rather what we want to do is:
 * get the current top page
 * get the position and dimensions of words we want to create interactable icons for
 * convert the world coordinates to the viewport of the rulebook camera
 * convert that viewport to world coordinates relative to the main camera
 * create interactables at the new world coords, parented to the main camera
 * destroy interactables when unneeded*/

// we want modders to be able to set multiple redirects that can go to different types of pages
// eg: abilities, stat icons, boons, items
[HarmonyPatch]
public static class RulebookRedirectPatches
{
    [HarmonyPostfix, HarmonyPatch(typeof(RuleBookController), nameof(RuleBookController.Start))]
    private static void CreateRedirectManager(RuleBookController __instance)
    {
        if (__instance.gameObject.GetComponent<RuleBookRedirectManager>() == null)
        {
            RuleBookRedirectManager.m_Instance = __instance.gameObject.AddComponent<RuleBookRedirectManager>();
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(RuleBookController), nameof(RuleBookController.SetShown))]
    private static void ResetInteractables(RuleBookController __instance, bool shown)
    {
        if (__instance.rigParent.activeSelf == shown)
            return;

        RuleBookRedirectManager.Instance.ClearActiveInteractables();
    }

    [HarmonyPostfix, HarmonyPatch(typeof(RulebookPageFlipper), nameof(RulebookPageFlipper.ShowFlipToPage))]
    private static IEnumerator ResetTrueTopIndex(IEnumerator enumerator, int pageIndex)
    {
        rulebookShowFlipIndex = pageIndex;
        yield return enumerator;
        rulebookShowFlipIndex = -1;
    }

    public static int rulebookShowFlipIndex = -1;

    [HarmonyPrefix, HarmonyPatch(typeof(RulebookPageFlipper), nameof(RulebookPageFlipper.RenderPages))]
    private static bool RetrieveRuleBookTopPage(Transform topPage)
    {
        RuleBookRedirectManager.Instance.currentTopPage = topPage.Find("Plane01");
        return true;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(PageFlipper), nameof(PageFlipper.LoadPageContent))]
    private static void AddInteractablesToTopPage(PageFlipper __instance, PageContentLoader loader, int index)
    {
        if (__instance.currentPageIndex != RuleBookRedirectManager.Instance.currentActivePageIndex)
            RuleBookRedirectManager.Instance.ClearActiveInteractables();

        if (index != __instance.currentPageIndex)
            return;

        RuleBookPageInfo pageInfo = __instance.PageData[__instance.currentPageIndex];
        RuleBookManager.RuleBookPageInfoExt fullInfo = RuleBookManager.ConstructedPagesWithRedirects.Find(x => x.parentPageInfo == pageInfo);
        if (fullInfo == null || fullInfo.RulebookDescriptionRedirects.Count == 0)
            return;

        if (__instance is TabletPageFlipper && RuleBookRedirectManager.Instance.currentTopPage == null)
        {
            RuleBookRedirectManager.Instance.currentTopPage = RuleBookRedirectManager.Instance.currentTopPage ??= Singleton<RuleBookController>.Instance.rigParent.transform.Find("Tablet").Find("Anim").transform;
        }
        else if (__instance is RulebookPageFlipper && rulebookShowFlipIndex != -1 && index != rulebookShowFlipIndex)
        {
            return; // account for ShowFlipToPage moving the currentIndex back 3     
        }

        TextMeshPro descriptionMesh;
        RuleBookPage component = loader.currentPageObj.GetComponent<RuleBookPage>();
        if (component is AbilityPage ab)
            descriptionMesh = ab.mainAbilityGroup.descriptionTextMesh;
        else if (component is StatIconPage icon)
            descriptionMesh = icon.descriptionTextMesh;
        else if (component is BoonPage boon)
            descriptionMesh = boon.descriptionTextMesh;
        else if (component is ItemPage item)
            descriptionMesh = item.descriptionTextMesh;
        else
            descriptionMesh = loader.currentPageObj.transform.Find("Description").GetComponent<TextMeshPro>();

        if (descriptionMesh != null)
        {
            descriptionMesh.ForceMeshUpdate();
            RuleBookRedirectManager.Instance.UpdateActiveInteractables(descriptionMesh, loader.currentPageObj, fullInfo.RulebookDescriptionRedirects);
        }
        RuleBookRedirectManager.Instance.currentActivePageIndex = __instance.currentPageIndex;
    }
}
