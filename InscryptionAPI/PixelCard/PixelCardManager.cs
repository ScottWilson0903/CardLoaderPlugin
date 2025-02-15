using DiskCardGame;
using GBC;
using HarmonyLib;
using InscryptionAPI.Card;
using InscryptionAPI.Helpers;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace InscryptionAPI.PixelCard;

[HarmonyPatch]
public static class PixelCardManager // code courtesy of Nevernamed and James/kelly
{
    public class PixelDecalData
    {
        public string PluginGUID;
        public string TextureName;
        public Texture2D DecalTexture;
    }

    public static readonly List<PixelDecalData> CustomPixelDecals = new();

    public static PixelDecalData AddGBCDecal(string pluginGUID, string textureName, Texture2D texture)
    {
        PixelDecalData result = new()
        {
            PluginGUID = pluginGUID,
            TextureName = textureName,
            DecalTexture = texture
        };
        if (!CustomPixelDecals.Contains(result))
            CustomPixelDecals.Add(result);

        return result;
    }
    internal static void Initialise()
    {
        PixelGemifiedDecal = TextureHelper.GetImageAsSprite("PixelGemifiedDecal.png", typeof(PixelCardManager).Assembly, TextureHelper.SpriteType.PixelDecal);
        PixelGemifiedOrangeLit = TextureHelper.GetImageAsSprite("PixelGemifiedOrange.png", typeof(PixelCardManager).Assembly, TextureHelper.SpriteType.PixelDecal);
        PixelGemifiedGreenLit = TextureHelper.GetImageAsSprite("PixelGemifiedGreen.png", typeof(PixelCardManager).Assembly, TextureHelper.SpriteType.PixelDecal);
        PixelGemifiedBlueLit = TextureHelper.GetImageAsSprite("PixelGemifiedBlue.png", typeof(PixelCardManager).Assembly, TextureHelper.SpriteType.PixelDecal);
    }

    [HarmonyPostfix, HarmonyPatch(typeof(CardAppearanceBehaviour), nameof(CardAppearanceBehaviour.Card), MethodType.Getter)]
    private static void GetCorrectCardComponentInAct2(CardAppearanceBehaviour __instance, ref DiskCardGame.Card __result)
    {
        if (SaveManager.SaveFile.IsPart2)
            __result = __instance.GetComponentInParent<DiskCardGame.Card>();
    }
    [HarmonyPostfix, HarmonyPatch(typeof(PixelBoardManager), nameof(PixelBoardManager.CleanUp))]
    private static IEnumerator ClearTempDecals(IEnumerator enumerator)
    {
        foreach (CardInfo info in SaveManager.SaveFile.gbcData.deck.Cards)
        {
            info.Mods.RemoveAll(x => x.IsTemporaryDecal());
        }
        yield return enumerator;
    }
    [HarmonyPatch(typeof(PixelCardDisplayer), nameof(PixelCardDisplayer.UpdateBackground))]
    [HarmonyPostfix]
    private static void PixelUpdateBackground(PixelCardDisplayer __instance, CardInfo info)
    {
        foreach (CardAppearanceBehaviour.Appearance appearance in info.appearanceBehaviour)
        {
            CardAppearanceBehaviourManager.FullCardAppearanceBehaviour fullApp = CardAppearanceBehaviourManager.AllAppearances.Find((CardAppearanceBehaviourManager.FullCardAppearanceBehaviour x) => x.Id == appearance);
            if (fullApp?.AppearanceBehaviour == null)
                continue;

            Component behav = __instance.gameObject.GetComponent(fullApp.AppearanceBehaviour);
            behav ??= __instance.gameObject.AddComponent(fullApp.AppearanceBehaviour);

            Sprite back = (behav as PixelAppearanceBehaviour)?.OverrideBackground();
            if (back != null)
            {
                SpriteRenderer component = __instance.GetComponent<SpriteRenderer>();
                if (component != null)
                    component.sprite = back;
            }
            UnityObject.Destroy(behav);
        }
    }

    [HarmonyPatch(typeof(PixelCardDisplayer), nameof(PixelCardDisplayer.DisplayInfo))]
    [HarmonyPostfix]
    private static void DecalPatches(PixelCardDisplayer __instance, PlayableCard playableCard)
    {
        if (__instance.gameObject.name == "PixelSnap" || SceneManager.GetActiveScene().name == "GBC_CardBattle" && __instance.gameObject.name != "CardPreviewPanel")
            AddDecalToCard(in __instance, playableCard);
    }

    private static void AddDecalToCard(in PixelCardDisplayer instance, PlayableCard playableCard)
    {
        Transform cardElements = instance?.gameObject?.transform?.Find("CardElements");
        if (cardElements == null)
            return;

        List<Transform> existingDecals = new();

        // clear current decals and appearances
        foreach (Transform child in cardElements.transform)
        {
            if (child?.gameObject?.GetComponent<DecalIdentifier>())
                existingDecals.Add(child);
        }
        for (int i = existingDecals.Count - 1; i >= 0; i--)
        {
            existingDecals[i].parent = null;
            UnityObject.Destroy(existingDecals[i].gameObject);
        }

        if (instance.info.Gemified && cardElements.Find("PixelGemifiedBorder") == null)
        {
            GameObject border = CreateDecal(in cardElements, PixelGemifiedDecal, "PixelGemifiedBorder");
            PixelGemificationBorder gemBorder = border.AddComponent<PixelGemificationBorder>();
            gemBorder.BlueGemLit = CreateDecal(in cardElements, PixelGemifiedBlueLit, "PixelGemifiedBlue");
            gemBorder.GreenGemLit = CreateDecal(in cardElements, PixelGemifiedGreenLit, "PixelGemifiedGreen");
            gemBorder.OrangeGemLit = CreateDecal(in cardElements, PixelGemifiedOrangeLit, "PixelGemifiedOrange");
        }

        foreach (CardAppearanceBehaviour.Appearance appearance in instance.info.appearanceBehaviour)
        {
            CardAppearanceBehaviourManager.FullCardAppearanceBehaviour fullApp = CardAppearanceBehaviourManager.AllAppearances.Find((x) => x.Id == appearance);
            if (fullApp?.AppearanceBehaviour == null)
                continue;

            Component behav = instance.gameObject.GetComponent(fullApp.AppearanceBehaviour);
            behav ??= instance.gameObject.AddComponent(fullApp.AppearanceBehaviour);

            if (behav is PixelAppearanceBehaviour pixelBehav)
            {
                pixelBehav.OnAppearanceApplied();
                Sprite behavAppearance = pixelBehav.PixelAppearance();
                Transform behavTransform = cardElements.Find(appearance.ToString() + "_Displayer");

                if (behavAppearance != null && behavTransform == null)
                    CreateDecal(in cardElements, behavAppearance, appearance.ToString() + "_Displayer");
                // override portrait
                Sprite overridePortrait = pixelBehav.OverridePixelPortrait();
                if (overridePortrait != null)
                    instance.SetPortrait(overridePortrait);
            }
            UnityObject.Destroy(behav);
        }

        if (playableCard == null)
            return;

        List<Tuple<Texture2D, string>> decalTextures = new();
        foreach (CardModificationInfo mod in playableCard.Info.Mods)
        {
            foreach (string decalId in mod.DecalIds)
            {
                PixelDecalData data = CustomPixelDecals.Find(x => x.TextureName == decalId);

                if (data != null)
                    decalTextures.Add(new(data.DecalTexture, data.TextureName));
            }
        }

        foreach (Tuple<Texture2D, string> decalTex in decalTextures)
        {
            Sprite decalSprite = TextureHelper.ConvertTexture(decalTex.Item1, TextureHelper.SpriteType.PixelDecal);
            CreateDecal(in cardElements, decalSprite, decalTex.Item2);
        }
    }
    private static GameObject CreateDecal(in Transform cardElements, Sprite sprite, string name)
    {
        GameObject decal = new(name);
        decal.transform.SetParent(cardElements, false);
        decal.layer = LayerMask.NameToLayer("GBCUI");
        decal.AddComponent<DecalIdentifier>();

        SpriteRenderer sr = decal.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;

        // Find sorting group values
        SpriteRenderer sortingReference = cardElements?.Find("Portrait")?.gameObject?.GetComponent<SpriteRenderer>();
        if (sortingReference != null)
        {
            sr.sortingLayerID = sortingReference.sortingLayerID;
            sr.sortingOrder = sortingReference.sortingOrder;
        }

        return decal;
    }

    public static Sprite PixelGemifiedDecal;
    public static Sprite PixelGemifiedOrangeLit;
    public static Sprite PixelGemifiedBlueLit;
    public static Sprite PixelGemifiedGreenLit;

    private class DecalIdentifier : MonoBehaviour { }
}
