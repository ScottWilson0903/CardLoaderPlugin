using DiskCardGame;
using HarmonyLib;
using InscryptionAPI.Items.Extensions;
using InscryptionAPI.RuleBook;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using static InscryptionAPI.Items.ConsumableItemManager;

namespace InscryptionAPI.Items;

[HarmonyPatch]
public static class ConsumableItemPatches
{
    [HarmonyPostfix, HarmonyPatch(typeof(ResourceBank), "Awake", new System.Type[] { })]
    private static void ResourceBank_Awake(ResourceBank __instance)
    {
        Initialize();
    }

    [HarmonyPostfix, HarmonyPatch(typeof(ItemsUtil), "AllConsumables", MethodType.Getter)]
    private static void ItemsUtil_AllConsumables(ref List<ConsumableItemData> __result)
    {
        __result.AddRange(allNewItems);
    }

    [HarmonyPrefix, HarmonyPatch(typeof(ConsumableItem), nameof(ConsumableItem.CanActivate), new Type[] { })]
    private static bool ConsumableItem_CanActivate(ConsumableItem __instance, ref bool __result)
    {
        if (__instance.Data is ConsumableItemData consumableItemData && consumableItemData.CanActivateOutsideBattles() && !GameFlowManager.IsCardBattle)
        {
            // We are not in a card battle. Unique behaviour!
            __result = __instance.ExtraActivationPrerequisitesMet();
            return false;
        }

        return true; // Keep original activation behaviour
    }

    [HarmonyPostfix, HarmonyPatch(typeof(ConsumableItemSlot), "ConsumeItem")]
    private static IEnumerator ConsumableItemSlot_ConsumeItemEnumerator(IEnumerator result, ConsumableItemSlot __instance)
    {
        ConsumableItemSlot_ConsumeItem.currentItemData = __instance.Consumable.Data;
        return result;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(ItemSlot), "CreateItem", new Type[] { typeof(ItemData), typeof(bool) })]
    private static bool ItemSlot_CreateItem(ItemSlot __instance, ItemData data, bool skipDropAnimation)
    {
        if (__instance.Item != null)
        {
            UnityObject.Destroy(__instance.Item.gameObject);
        }

        string prefabId = "Prefabs/Items/" + data.PrefabId;
        GameObject gameObject = null;
        if (prefabIDToResourceLookup.TryGetValue(prefabId.ToLowerInvariant(), out ConsumableItemResource resource) && data is ConsumableItemData consumableItemData)
        {
            GameObject prefab = resource.Get<GameObject>();
            if (prefab == null)
            {
                InscryptionAPIPlugin.Logger.LogError($"Failed to get {consumableItemData.rulebookName} model from ConsumableItemAssetGetter " + resource);
            }
            else
            {
                gameObject = UnityObject.Instantiate<GameObject>(prefab, __instance.transform);
            }

            resource.PreSetupCallback?.Invoke(gameObject, consumableItemData);
            SetupPrefab(consumableItemData, gameObject, consumableItemData.GetComponentType(), consumableItemData.GetPrefabModelType());
        }
        else
        {
            gameObject = UnityObject.Instantiate<GameObject>(ResourceBank.Get<GameObject>(prefabId), __instance.transform);
            if (gameObject == null)
            {
                InscryptionAPIPlugin.Logger.LogError($"Failed to get {data.name} model from ResourceBank " + prefabId);
            }
        }

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        gameObject.transform.localPosition = Vector3.zero;
        __instance.Item = gameObject.GetComponent<Item>();
        __instance.Item.SetData(data);
        if (skipDropAnimation)
        {
            __instance.Item.PlayEnterAnimation(true);
        }

        // Setup cards
        if (__instance.Item is CardBottleItem cardBottleItem && data is ConsumableItemData consumableItemData2)
        {
            string cardWithinBottle = consumableItemData2.GetCardWithinBottle();
            if (!string.IsNullOrEmpty(cardWithinBottle))
            {
                CardInfo cardInfo = CardLoader.GetCardByName(cardWithinBottle);
                if (cardInfo != null)
                {
                    cardBottleItem.cardInfo = cardInfo;
                    cardBottleItem.GetComponentInChildren<SelectableCard>().SetInfo(cardInfo);
                    cardBottleItem.gameObject.GetComponent<AssignCardOnStart>().enabled = false;
                }
                else
                {
                    InscryptionAPIPlugin.Logger.LogError("Could not get card for bottled card item: " + cardWithinBottle);
                }
            }
        }

        return false;
    }

    [HarmonyPatch]
    internal class ConsumableItemSlot_ConsumeItem
    {
        private static Type ConsumableItemSlot_ConsumeItem_Class = Type.GetType("DiskCardGame.ConsumableItemSlot+<ConsumeItem>d__15, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");

        private static MethodInfo SwitchToViewMethod = AccessTools.Method(typeof(ViewManager), nameof(ViewManager.SwitchToView), new Type[] { typeof(View), typeof(bool), typeof(bool) });
        private static MethodInfo CustomSwitchToViewMethod = AccessTools.Method(typeof(ConsumableItemSlot_ConsumeItem), nameof(SwitchToView), new Type[] { typeof(ViewManager), typeof(View), typeof(bool), typeof(bool) });

        internal static ItemData currentItemData = null;

        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(ConsumableItemSlot_ConsumeItem_Class, "MoveNext");
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // === We want to turn this

            // ConsumableItemSlot consumableItemSlot = <>4__this;
            // ...
            // Singleton<ViewManager>.Instance.SwitchToView(..., ..., ...);

            // === Into this

            // ConsumableItemSlot consumableItemSlot = <>4__this;
            // ...
            // GetNextTargetView(..., ..., ...);

            // ===

            List<CodeInstruction> codes = new(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction codeInstruction = codes[i];
                if (codeInstruction.operand == SwitchToViewMethod)
                {
                    codeInstruction.operand = CustomSwitchToViewMethod; // Call CustomSwitchToView instead of View
                    i++;
                }
            }

            return codes;
        }

        public static void SwitchToView(ViewManager instance, View view, bool immediate, bool lockAfter)
        {
            if (currentItemData != null && currentItemData is ConsumableItemData itemData && itemData.CanActivateOutsideBattles())
            {
                return; // Do nothing!
            }

            instance.SwitchToView(view, immediate, lockAfter);
        }
    }

    [HarmonyTranspiler, HarmonyPatch(typeof(RuleBookInfo), nameof(RuleBookInfo.ConstructPageData))]
    private static IEnumerable<CodeInstruction> ConstructItemPageData(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = new(instructions);

        int startIndex = codes.FindIndex(x => x.opcode == OpCodes.Ldfld && x.operand.ToString() == "System.Collections.Generic.List`1[DiskCardGame.ConsumableItemData] allConsumables");
        if (startIndex != -1)
        {
            startIndex -= 3;
            int endIndex = codes.FindIndex(startIndex, x => x.opcode == OpCodes.Callvirt && x.operand.ToString() == "System.Collections.Generic.List`1[DiskCardGame.RuleBookPageInfo] ConstructPages(DiskCardGame.PageRangeInfo, Int32, Int32, System.Func`2[System.Int32,System.Boolean], System.Action`3[DiskCardGame.RuleBookPageInfo,DiskCardGame.PageRangeInfo,System.Int32], System.String)");
            MethodInfo method = AccessTools.Method(typeof(ConsumableItemPatches), nameof(ConsumableItemPatches.ConstructItemPages), new Type[]
            { typeof(RuleBookInfo), typeof(PageRangeInfo), typeof(AbilityMetaCategory) });

            codes.RemoveRange(startIndex, endIndex - startIndex + 1);
            codes.Insert(startIndex++, new(OpCodes.Ldarg_0));
            codes.Insert(startIndex++, new(OpCodes.Ldloc_3));
            codes.Insert(startIndex++, new(OpCodes.Ldarg_1));
            codes.Insert(startIndex++, new(OpCodes.Callvirt, method));
        }
        return codes;
    }

    public static List<RuleBookPageInfo> ConstructItemPages(RuleBookInfo instance, PageRangeInfo pageRange, AbilityMetaCategory metaCategory)
    {
        List<ConsumableItemData> allConsumables = ItemsUtil.AllConsumables;
        return instance.ConstructPages(pageRange, allConsumables.Count, 0,
            (int index) => RuleBookManager.ItemShouldBeAdded(allConsumables[index], metaCategory),
            instance.FillItemPage,
            Localization.Translate("APPENDIX XII, SUBSECTION IX - ITEMS {0}")
        );
    }
}
