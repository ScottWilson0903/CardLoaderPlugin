using DiskCardGame;
using HarmonyLib;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace InscryptionCommunityPatch.Card;

[HarmonyPatch]
internal class DrawNewHand3DFix
{
    private static MethodBase TargetMethod()
    {
        MethodBase baseMethod = AccessTools.Method(typeof(DrawNewHand), nameof(DrawNewHand.OnResolveOnBoard));
        return AccessTools.EnumeratorMoveNext(baseMethod);
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = new(instructions);

        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Callvirt && codes[i].operand.ToString().Contains("DrawCardFromDeck"))
            {
                MethodInfo customMethod = AccessTools.Method(typeof(DrawNewHand3DFix), nameof(DrawNewHand3DFix.DrawFromPiles),
                    new Type[] { typeof(CardDrawPiles), typeof(CardInfo), typeof(Action<PlayableCard>) });
                codes[i].operand = customMethod;
                break;
            }
        }

        return codes;
    }
    private static IEnumerator DrawFromPiles(CardDrawPiles instance, CardInfo info, Action<PlayableCard> action)
    {
        if (!SaveManager.SaveFile.IsPart2)
        {
            (instance as CardDrawPiles3D).Pile.Draw();
            yield return new WaitForSeconds(0.1f);
        }
        yield return instance.DrawCardFromDeck(info, action);
    }
}

[HarmonyPatch]
internal class DrawNewHand3DViewFix
{
    [HarmonyPatch(typeof(DrawNewHand), nameof(DrawNewHand.OnResolveOnBoard))]
    [HarmonyPostfix]
    private static IEnumerator LookAtHand3D(IEnumerator enumerator)
    {
        if (SaveManager.SaveFile.IsPart2)
        {
            yield return enumerator;
            yield break;
        }

        yield return new WaitForSeconds(0.2f);
        ViewManager.Instance.SwitchToView(View.Hand);
        yield return new WaitForSeconds(0.25f);

        yield return enumerator;

        if (CardDrawPiles3D.Instance.Deck.CardsInDeck == 0)
            CardDrawPiles3D.Instance.ClearPileDelegates();

        yield return new WaitForSeconds(0.2f);
        ViewManager.Instance.SwitchToView(View.Default);
        yield return new WaitForSeconds(0.2f);
    }
}