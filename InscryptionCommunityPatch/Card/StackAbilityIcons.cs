using DiskCardGame;
using GBC;
using HarmonyLib;
using InscryptionAPI.Card;
using InscryptionAPI.Helpers;
using UnityEngine;

namespace InscryptionCommunityPatch.Card;

[HarmonyPatch]
public static class StackAbilityIcons
{
    // This patch modifies ability sigils such that multiple instances of the same sigil (also evolve numbers, hello from the future)
    // are displayed as a single sigil with a number to indicate how many copies there are
    private static readonly Texture2D[] NUMBER_TEXTURES = new Texture2D[]
    {
        TextureHelper.GetImageAsTexture("Stack_0.png", typeof(StackAbilityIcons).Assembly),
        TextureHelper.GetImageAsTexture("Stack_1.png", typeof(StackAbilityIcons).Assembly),
        TextureHelper.GetImageAsTexture("Stack_2.png", typeof(StackAbilityIcons).Assembly),
        TextureHelper.GetImageAsTexture("Stack_3.png", typeof(StackAbilityIcons).Assembly),
        TextureHelper.GetImageAsTexture("Stack_4.png", typeof(StackAbilityIcons).Assembly),
        TextureHelper.GetImageAsTexture("Stack_5.png", typeof(StackAbilityIcons).Assembly),
        TextureHelper.GetImageAsTexture("Stack_6.png", typeof(StackAbilityIcons).Assembly),
        TextureHelper.GetImageAsTexture("Stack_7.png", typeof(StackAbilityIcons).Assembly),
        TextureHelper.GetImageAsTexture("Stack_8.png", typeof(StackAbilityIcons).Assembly),
        TextureHelper.GetImageAsTexture("Stack_9.png", typeof(StackAbilityIcons).Assembly)
    };
    private static readonly Texture2D[] MEDIUM_NUMBER_TEXTURES = new Texture2D[]
    {
        TextureHelper.GetImageAsTexture("Stack_0_med.png", typeof(StackAbilityIcons).Assembly),
        TextureHelper.GetImageAsTexture("Stack_1_med.png", typeof(StackAbilityIcons).Assembly),
        TextureHelper.GetImageAsTexture("Stack_2_med.png", typeof(StackAbilityIcons).Assembly),
        TextureHelper.GetImageAsTexture("Stack_3_med.png", typeof(StackAbilityIcons).Assembly),
        TextureHelper.GetImageAsTexture("Stack_4_med.png", typeof(StackAbilityIcons).Assembly),
        TextureHelper.GetImageAsTexture("Stack_5_med.png", typeof(StackAbilityIcons).Assembly),
        TextureHelper.GetImageAsTexture("Stack_6_med.png", typeof(StackAbilityIcons).Assembly),
        TextureHelper.GetImageAsTexture("Stack_7_med.png", typeof(StackAbilityIcons).Assembly),
        TextureHelper.GetImageAsTexture("Stack_8_med.png", typeof(StackAbilityIcons).Assembly),
        TextureHelper.GetImageAsTexture("Stack_9_med.png", typeof(StackAbilityIcons).Assembly)
    };
    private static readonly Sprite[] GBC_NUMBER_SPRITES = new Sprite[]
    {
        GetGBCNumberSprite(1),
        GetGBCNumberSprite(2),
        GetGBCNumberSprite(3),
        GetGBCNumberSprite(4),
        GetGBCNumberSprite(5),
        GetGBCNumberSprite(6),
        GetGBCNumberSprite(7),
        GetGBCNumberSprite(8),
        GetGBCNumberSprite(9),
    };

    private static Sprite GetGBCNumberSprite(int number)
    {
        string stackGBC = "stack_gbc.png";
        if (!PatchPlugin.act2StackIconType.Value)
            stackGBC = "stack_gbc_alt.png";

        Texture2D texture = TextureHelper.GetImageAsTexture(stackGBC, typeof(StackAbilityIcons).Assembly);
        return Sprite.Create(texture, new Rect(0f, 10f * (9f - number), 15f, 10f), new Vector2(0.5f, 0.5f));
    }

    private static Color[] _topBorder = null;
    private static Color[] TOP_BORDER
    {
        get
        {
            if (_topBorder == null)
            {
                _topBorder = new Color[NUMBER_TEXTURES[0].width + 1];
                for (int i = 0; i < _topBorder.Length; i++)
                    _topBorder[i] = new Color(0f, 0f, 0f, 0f); // Add a completely transparent pixel
            }
            return _topBorder;
        }
    }

    private static Color[] _leftBorder = null;
    private static Color[] LEFT_BORDER
    {
        get
        {
            if (_leftBorder == null)
            {
                _leftBorder = new Color[NUMBER_TEXTURES[0].height + 1];
                for (int i = 0; i < _leftBorder.Length; i++)
                    _leftBorder[i] = new Color(0f, 0f, 0f, 0f); // Add a completely transparent pixel
            }
            return _leftBorder;
        }
    }

    private static readonly int NORMAL = 1;
    private static readonly int MEDIUM = 2;
    private static readonly int FORCED = 3;

    private static readonly Dictionary<string, Texture2D> patchedTexture = new();
    private static readonly Dictionary<Ability, Tuple<Vector2Int, int>> patchLocations = new();

    private static Vector2Int FindMatchingOnesDigit(Texture2D searchTex, bool normalSize = true)
    {
        Texture2D onesTexture = normalSize ? NUMBER_TEXTURES[1] : MEDIUM_NUMBER_TEXTURES[1];
        Color[] onesColor = onesTexture.GetPixels();
        return FindMatchingTexture(searchTex, onesTexture.width, onesTexture.height, onesColor);
    }

    private static Vector2Int FindMatchingTexture(Texture2D searchTex, int width, int height, Color[] matchPixels = null)
    {
        Color[] searchPixels = searchTex.GetPixels();

        int locX = -1;
        int locY = -1;
        bool failed = false;

        for (int sX = 0; sX < searchTex.width - width; sX++)
        {
            for (int sY = 0; sY < searchTex.height - height; sY++)
            {
                failed = false;
                for (int nX = 0; nX < width; nX++)
                {
                    for (int nY = 0; nY < height; nY++)
                    {
                        int j = nX + (nY * width);
                        int i = sX + nX + (sY + nY) * searchTex.width;
                        if (matchPixels != null && (searchPixels[i].a > 0) != (matchPixels[j].a > 0))
                        {
                            failed = true;
                            break;
                        }

                    }

                    if (failed)
                        break;
                }

                if (failed)
                    continue;

                // Success!
                locX = sX;
                locY = sY;
            }

            if (!failed)
                break;
        }

        return new Vector2Int(locX, locY);
    }

    private static Tuple<Vector2Int, int> FindNextBestLocation(Texture2D texture)
    {
        // First, we want to see if the lower-right corner is available.
        // We will call it available if it's all blank and there is a one-pixel border around it
        Color[] pixels = texture.GetPixels();

        Vector2Int lowerRight = new Vector2Int(texture.width - NUMBER_TEXTURES[0].width, 0);
        bool success = true;
        for (int sX = lowerRight.x - 1; sX < texture.width; sX++)
        {
            for (int sY = lowerRight.y; sY < texture.height; sY++)
            {
                int i = sX + (texture.width * sY);

                if (pixels[i].a > 0f)
                {
                    success = false;
                    break;
                }
            }

            if (!success)
                break;
        }

        if (success)
            return new Tuple<Vector2Int, int>(lowerRight, NORMAL);

        // Okay, the lower right is not clear.
        // At this point, we're just going to look for any clear space.
        // A clear space is all blank and has a one-pixel gap around it
        Vector2Int nextBest = FindMatchingTexture(texture, NUMBER_TEXTURES[0].width + 2, NUMBER_TEXTURES[0].height + 2);
        if (nextBest.x != -1)
            return new Tuple<Vector2Int, int>(nextBest, NORMAL);

        // Ugh. Okay, we have to use the lower right now.
        // And we'll just have to deal with what that looks like.
        return new Tuple<Vector2Int, int>(lowerRight, FORCED);
    }

    private static Tuple<Vector2Int, int> GetPatchLocationForAbility(Ability ability, Texture2D abilityTexture, int stackAmount)
    {
        if (patchLocations.ContainsKey(ability))
            return patchLocations[ability];

        // We have to calculate the location

        // First, see if the texture has the 'one' icon on it. If so, we will replace at that location
        if (stackAmount < 10)
        {
            Vector2Int oneLoc = FindMatchingOnesDigit(abilityTexture, true);
            if (oneLoc.x != -1)
            {
                patchLocations.Add(ability, new Tuple<Vector2Int, int>(oneLoc, NORMAL));
                return patchLocations[ability];
            }

            // Let's try the medium-sized digit
            oneLoc = FindMatchingOnesDigit(abilityTexture, false);
            if (oneLoc.x != -1)
            {
                patchLocations.Add(ability, new Tuple<Vector2Int, int>(oneLoc, MEDIUM));
                return patchLocations[ability];
            }
        }

        // Simplified the logic for next best to prevent issues - can be easily reverted if more issues arise

        Vector2Int lowerRight = new(abilityTexture.width - NUMBER_TEXTURES[0].width, 0);
        Tuple<Vector2Int, int> nextBest = new(lowerRight, NORMAL);

        patchLocations.Add(ability, nextBest);
        return patchLocations[ability];
    }

    private static Texture2D PatchTexture(Ability ability, int count)
    {
        if (count <= 1 || count > 99) // only supports 2-digit-long numbers (why would you have 100 stacks anyways?)
            return null;

        string textureName = $"{ability}-icon-{count}-PATCHEDINF";
        if (patchedTexture.ContainsKey(textureName))
            return patchedTexture[textureName];

        PatchPlugin.Logger.LogDebug($"Ability [{AbilitiesUtil.GetInfo(ability).rulebookName}] stacks [{count}] time(s).");

        // Copy the old texture to the new texture
        bool doubleDigit = count > 9;
        Texture2D newTexture = TextureHelper.DuplicateTexture(AbilitiesUtil.LoadAbilityIcon(ability.ToString(), false, false) as Texture2D);
        newTexture.name = textureName;

        Tuple<Vector2Int, int> patchTuple = GetPatchLocationForAbility(ability, newTexture, count);
        Vector2Int patchLocation = patchTuple.Item1;
        int textureType = patchTuple.Item2; // This means that it's on the lower-right and needs a one-pixel border

        if (textureType == FORCED)
        {
            // We will set a one-pixel border around the location
            newTexture.SetPixels(patchLocation.x - 1, patchLocation.y + 1, TOP_BORDER.Length, 1, TOP_BORDER, 0);
            newTexture.SetPixels(patchLocation.x - 1, patchLocation.y + 1, 1, LEFT_BORDER.Length, LEFT_BORDER, 0);
        }

        // Set the new number
        Texture2D tensDigitTex = null;
        Texture2D onesDigitTex;
        if (doubleDigit)
        {
            int tensPlace = 1;
            int onesPlace = count - 10;
            while (onesPlace > 9)
            {
                tensPlace++;
                onesPlace -= 10;
            }
            Texture2D tensTex = textureType == NORMAL ? NUMBER_TEXTURES[tensPlace] : MEDIUM_NUMBER_TEXTURES[tensPlace];
            Texture2D onesTex = textureType == NORMAL ? NUMBER_TEXTURES[onesPlace] : MEDIUM_NUMBER_TEXTURES[onesPlace];
            onesDigitTex = onesTex;
            tensDigitTex = tensTex;
        }
        else
            onesDigitTex = textureType == NORMAL ? NUMBER_TEXTURES[count] : MEDIUM_NUMBER_TEXTURES[count];

        if (tensDigitTex != null)
        {
            try
            {
                newTexture.SetPixels(patchLocation.x - (onesDigitTex.width + 1), patchLocation.y, tensDigitTex.width, tensDigitTex.height, tensDigitTex.GetPixels(), 0);
            }
            catch { PatchPlugin.Logger.LogError("Couldn't properly set new texture."); }
        }

        newTexture.SetPixels(patchLocation.x, patchLocation.y, onesDigitTex.width, onesDigitTex.height, onesDigitTex.GetPixels(), 0);
        newTexture.filterMode = FilterMode.Point;
        newTexture.Apply(); // Apply all of the set pixels

        // Upload to the dictionary
        patchedTexture.Add(textureName, newTexture);
        return newTexture;
    }

    [HarmonyPatch(typeof(CardAbilityIcons), "GetDistinctShownAbilities")]
    [HarmonyPostfix]
    private static void ClearStackableIcons(ref List<Ability> __result, CardInfo info, List<CardModificationInfo> mods, List<Ability> hiddenAbilities)
    {
        int preDistinctCount = __result.Count;
        __result = __result.Distinct().ToList();
        if (PatchPlugin.doubleStackSplit.Value && __result.Count == 1 && preDistinctCount == 2 && AbilitiesUtil.GetInfo(__result[0]).canStack)
        {
            __result.Add(__result[0]);
        }
    }

    [HarmonyPatch(typeof(AbilityIconInteractable), "AssignAbility")]
    [HarmonyPostfix]
    private static void AddIconNumber(Ability ability, CardInfo info, PlayableCard card, ref AbilityIconInteractable __instance)
    {
        if (info == null || !AbilitiesUtil.GetInfo(ability).canStack)
            return;

        // Here's the goal
        // Find all abilities on the card
        // Replace all of the textures where it stacks with a texture showing that it stacks
        // Okay, go through each ability on the card and see how many instances it has.
        int? count = null;
        List<Ability> baseAbilities = new(info.Abilities);
        AbilityInfo ai = AbilityManager.AllAbilityInfos.AbilityByID(ability);
        if (card != null)
        {
            baseAbilities.AddRange(AbilitiesUtil.GetAbilitiesFromMods(card.TemporaryMods/*.Where(x => !x.fromTotem && (!x.fromCardMerge || PatchPlugin.configMergeOnBottom.Value)).ToList()*/));
            if (ai.GetHideSingleStacks())
            {
                for (int i = 0; i < card.Status.hiddenAbilities.Count(x => x == ability); i++)
                {
                    baseAbilities.Remove(ability);
                }
            }
            else if (card.Status.hiddenAbilities.Contains(ability))
                baseAbilities.RemoveAll(x => x == ability);

            if (ai.IsShieldAbility())
                count = card.GetShieldBehaviour(ability)?.NumShields;
        }

        count ??= baseAbilities.Count(ab => ab == ability);
        //PatchPlugin.Logger.LogDebug($"[{AbilitiesUtil.GetInfo(ability).rulebookName}] {count} {baseAbilities.Count}");

        if (count > 1) // we have a stack and need to add an override
        {
            if (PatchPlugin.doubleStackSplit.Value && count == 2 && count == baseAbilities.Count)
                __instance.SetIcon(__instance.LoadIcon(info, ai, card != null && card.OpponentCard));//RenderSameSigilTwice(__instance, ai, info, card);
            else
                __instance.SetIcon(PatchTexture(ability, (int)count));
        }
    }

    [HarmonyPatch(typeof(PixelCardAbilityIcons), "DisplayAbilities", new Type[] { typeof(List<Ability>), typeof(PlayableCard) })]
    [HarmonyPrefix]
    private static bool PatchPixelCardStacks(PixelCardAbilityIcons __instance, List<Ability> abilities, PlayableCard card)
    {
        return RenderPixelAbilityStacks(__instance, abilities, card);
    }

    public static bool RenderPixelAbilityStacks(PixelCardAbilityIcons __instance, List<Ability> abilities, PlayableCard card)
    {
        List<GameObject> abilityIconGroups = __instance.abilityIconGroups;
        if (abilityIconGroups.Count == 0)
            return false;

        foreach (GameObject gameObject in abilityIconGroups)
            gameObject.gameObject.SetActive(false);

        List<Ability> allDisplayableAbilities = new(abilities);
        if (card != null)
        {
            allDisplayableAbilities.AddRange(AbilitiesUtil.GetAbilitiesFromMods(card.TemporaryMods));
        }

        List<Tuple<Ability, int>> grps = allDisplayableAbilities.Distinct().Select(a => new Tuple<Ability, int>(a, AbilitiesUtil.GetInfo(a).canStack ? abilities.Count(ab => ab == a) : 1)).ToList();

        if (grps.Count > 0 && grps.Count - 1 < abilityIconGroups.Count) // if there are displayable sigils and there are enough icon groups
        {
            // if there is only 1 ability and there are two stacks of it, render it twice
            if (PatchPlugin.doubleStackSplit.Value && grps.Count == 1 && grps[0].Item2 == 2/* && AbilitiesUtil.GetInfo(grps[0].Item1).canStack*/)
            {
                //PatchPlugin.Logger.LogDebug($"Displaying {grps[0].Item1} twice");
                grps[0] = new Tuple<Ability, int>(grps[0].Item1, 1);
                grps.Add(grps[0]);
            }

            GameObject iconGroup = abilityIconGroups[grps.Count - 1];
            iconGroup.gameObject.SetActive(true);

            List<SpriteRenderer> abilityRenderers = new();
            foreach (Transform child in iconGroup.transform)
                abilityRenderers.Add(child.gameObject.GetComponent<SpriteRenderer>());

            abilityRenderers.RemoveAll(sr => sr == null);

            CardInfo cardInfo = card?.Info ?? __instance.GetComponentInParent<DiskCardGame.Card>()?.Info;
            for (int i = 0; i < abilityRenderers.Count; i++)
            {
                SpriteRenderer abilityRenderer = abilityRenderers[i];
                AbilityInfo abilityInfo = AbilitiesUtil.GetInfo(grps[i].Item1);
                int stackCount = grps[i].Item2;
                if (card != null)
                {
                    if (abilityInfo.GetHideSingleStacks())
                    {
                        for (int j = 0; j < card.Status.hiddenAbilities.Count(x => x == grps[i].Item1); j++)
                        {
                            stackCount--;
                        }
                    }
                    else if (card.Status.hiddenAbilities.Contains(grps[i].Item1))
                        stackCount -= allDisplayableAbilities.Count(x => x == grps[i].Item1);

                    if (abilityInfo.IsShieldAbility())
                        stackCount = card.GetShieldBehaviour(grps[i].Item1)?.NumShields ?? 0;
                }

                abilityRenderer.sprite = abilityInfo.activated ? new() : OverridePixelSprite(abilityInfo, cardInfo, card);
                if (abilityInfo.flipYIfOpponent && card != null && card.OpponentCard)
                {
                    if (abilityInfo.customFlippedPixelIcon)
                        abilityRenderer.sprite = abilityInfo.customFlippedPixelIcon;
                    else
                        abilityRenderer.flipY = true;
                }
                else
                    abilityRenderer.flipY = false;

                PatchPlugin.Logger.LogDebug($"Ability [{grps[i].Item1}] stacks [{stackCount}] time(s)");
                AddStackCount(abilityRenderer, grps[i].Item1, stackCount);
            }
        }
        __instance.conduitIcon.SetActive(abilities.Exists((Ability x) => AbilitiesUtil.GetInfo(x).conduit));
        Ability ability = abilities.Find((Ability x) => AbilitiesUtil.GetInfo(x).activated);
        PixelActivatedAbilityButton button = __instance.activatedAbilityButton;

        if (ability != Ability.None)
        {
            button.gameObject.SetActive(true);
            button.SetAbility(ability);
        }
        else
        {
            button.gameObject.SetActive(false);
        }

        return false;
    }
    private static void AddStackCount(SpriteRenderer abilityRenderer, Ability ability, int count)
    {
        // And now my custom code to add the ability counter if we need to
        Transform countTransform = abilityRenderer.transform.Find("Count");

        if (countTransform == null)
        {
            if (count <= 1)
                return;

            GameObject counter = new();
            counter.transform.SetParent(abilityRenderer.transform);
            counter.layer = LayerMask.NameToLayer("GBCPauseMenu");
            SpriteRenderer renderer = counter.AddComponent<SpriteRenderer>();
            renderer.size = new Vector2(0.14f, 0.08f);
            renderer.color = new Color(1f, 1f, 1f, 1f);
            renderer.adaptiveModeThreshold = 0.5f;
            renderer.enabled = true;
            renderer.sortingLayerName = "PauseMenuUI";
            renderer.sortingOrder = 200;

            counter.name = "Count";
            counter.transform.localPosition = new Vector3(.03f, -.05f, 0f);
            countTransform = counter.transform;
        }

        if (count <= 1)
            countTransform.gameObject.SetActive(false);
        else
        {
            countTransform.gameObject.SetActive(true);
            PatchPlugin.Logger.LogDebug($"countTransform [{count - 1}]");
            countTransform.gameObject.GetComponent<SpriteRenderer>().sprite = GBC_NUMBER_SPRITES[count - 1];
        }
    }
    private static Sprite OverridePixelSprite(AbilityInfo abilityInfo, CardInfo cardInfo, PlayableCard card)
    {
        // countdown numbers for evolve and transformer
        if (cardInfo != null && (abilityInfo.ability == Ability.Evolve || abilityInfo.ability == Ability.Transformer))
        {
            int turnsInPlay = card?.GetComponentInChildren<Evolve>()?.numTurnsInPlay ?? 0;
            int turnsToEvolve = Mathf.Max(1, (cardInfo.evolveParams == null ? 1 : cardInfo.evolveParams.turnsToEvolve) - turnsInPlay);
            int pngIndex = turnsToEvolve > 3 ? 0 : turnsToEvolve;

            if (pngIndex == 0)
                return abilityInfo.pixelIcon;

            Texture2D texture;
            if (abilityInfo.ability == Ability.Evolve)
                texture = TextureHelper.GetImageAsTexture($"pixel_evolve_{pngIndex}.png", typeof(StackAbilityIcons).Assembly);
            else
                texture = TextureHelper.GetImageAsTexture($"pixel_transformer_{pngIndex}.png", typeof(StackAbilityIcons).Assembly);

            return TextureHelper.ConvertTexture(texture, TextureHelper.SpriteType.PixelAbilityIcon);
        }

        if (card != null && card.RenderInfo.overriddenAbilityIcons.ContainsKey(abilityInfo.ability))
        {
            card.RenderInfo.overriddenAbilityIcons.TryGetValue(abilityInfo.ability, out Texture texture);

            if (texture != null)
                return TextureHelper.ConvertTexture((Texture2D)texture, TextureHelper.SpriteType.PixelAbilityIcon);
        }
        return abilityInfo.pixelIcon;
    }

    public static string StackDescription(string input)
    {
        string[] lines = input.Split('\n');
        string retval = lines[0];
        int duplicateCount = 0;
        for (int i = 1; i < lines.Length; i++)
        {
            if (lines[i] == lines[i - 1])
                duplicateCount += 1;
            else
            {
                if (duplicateCount > 0)
                    retval += $" (x{duplicateCount + 1})";

                retval = retval + "\n" + lines[i];
                duplicateCount = 0;
            }
        }
        if (duplicateCount > 0)
            retval += $" (x{duplicateCount + 1})";

        return retval;
    }

    [HarmonyPatch(typeof(CardInfo), "GetGBCDescriptionLocalized")]
    [HarmonyPostfix]
    private static void GetStackedGBCDescriptionLocalized(ref string __result) => __result = StackDescription(__result);
}
