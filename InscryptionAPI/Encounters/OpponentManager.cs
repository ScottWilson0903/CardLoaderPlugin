using DiskCardGame;
using HarmonyLib;
using InscryptionAPI.Guid;
using InscryptionAPI.Masks;
using InscryptionAPI.Saves;
using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace InscryptionAPI.Encounters;

[HarmonyPatch]
public static class OpponentManager
{
    public class FullOpponent
    {
        public readonly Opponent.Type Id;
        public LeshyAnimationController.Mask MaskType = MaskManager.NoMask;
        public Type Opponent;
        public string SpecialSequencerId;
        public List<Texture2D> NodeAnimation = new();

        public FullOpponent(Opponent.Type id, Type opponent, string specialSequencerId) : this(id, opponent, specialSequencerId, null) { }

        public FullOpponent(Opponent.Type id, Type opponent, string specialSequencerId, List<Texture2D> nodeAnimation)
        {
            Id = id;
            SpecialSequencerId = specialSequencerId;
            Opponent = opponent;
            if (nodeAnimation != null)
            {
                NodeAnimation = new(nodeAnimation);
            }
        }
    }

    public static readonly ReadOnlyCollection<FullOpponent> BaseGameOpponents = new(GenBaseGameOpponents());
    internal static readonly ObservableCollection<FullOpponent> NewOpponents = new();

    private static List<FullOpponent> GenBaseGameOpponents()
    {
        bool useReversePatch = true;
        try
        {
            OriginalGetSequencerIdForBoss(Opponent.Type.ProspectorBoss);
        }
        catch (NotImplementedException)
        {
            useReversePatch = false;
        }

        List<FullOpponent> baseGame = new();
        var gameAsm = typeof(Opponent).Assembly;
        foreach (Opponent.Type opponent in Enum.GetValues(typeof(Opponent.Type)))
        {
            string specialSequencerId = useReversePatch ? OriginalGetSequencerIdForBoss(opponent) : BossBattleSequencer.GetSequencerIdForBoss(opponent);
            Type opponentType = gameAsm.GetType($"DiskCardGame.{opponent.ToString()}Opponent") ?? gameAsm.GetType($"GBC.{opponent.ToString()}Opponent");

            FullOpponent fullOpponent = new FullOpponent(opponent, opponentType, specialSequencerId)
            {
                MaskType = MaskManager.BossToMask(opponent)
            };
            baseGame.Add(fullOpponent);
        }
        return baseGame;
    }

    static OpponentManager()
    {
        NewOpponents.CollectionChanged += static (_, _) =>
        {
            AllOpponents = BaseGameOpponents.Concat(NewOpponents).ToList();
        };
    }

    public static List<FullOpponent> AllOpponents { get; private set; } = BaseGameOpponents.ToList();

    public static FullOpponent Add(string guid, string opponentName, string sequencerID, Type opponentType)
    {
        return Add(guid, opponentName, sequencerID, opponentType, null);
    }

    public static FullOpponent Add(string guid, string opponentName, string sequencerID, Type opponentType, List<Texture2D> nodeAnimation)
    {
        Opponent.Type opponentId = GuidManager.GetEnumValue<Opponent.Type>(guid, opponentName);
        FullOpponent opp = new(opponentId, opponentType, sequencerID, nodeAnimation);
        NewOpponents.Add(opp);
        return opp;
    }
    public static List<Opponent.Type> RunStateOpponents
    {
        get
        {
            List<Opponent.Type> previousBosses = new List<Opponent.Type>();

            string value = ModdedSaveManager.RunState.GetValue(InscryptionAPIPlugin.ModGUID, "PreviousBosses"); // 2,0,1
            if (value == null)
            {
                // Do nothing
            }
            else if (!value.Contains(','))
            {
                // Single boss encounter
                previousBosses.Add((Opponent.Type)int.Parse(value));
            }
            else
            {
                // Multiple boss encounters
                IEnumerable<Opponent.Type> ids = value.Split(',').Select(static (a) => (Opponent.Type)int.Parse(a));
                previousBosses.AddRange(ids);
            }

            return previousBosses;
        }
        set
        {
            string result = ""; // 2,0,1
            for (int i = 0; i < value.Count; i++)
            {
                if (i > 0)
                {
                    result += ",";
                }
                result += (int)value[i];

            }
            ModdedSaveManager.RunState.SetValue(InscryptionAPIPlugin.ModGUID, "PreviousBosses", result);
        }
    }

    #region Patches
    [HarmonyPrefix, HarmonyPatch(typeof(Opponent), nameof(Opponent.SpawnOpponent))]
    private static bool ReplaceSpawnOpponent(EncounterData encounterData, ref Opponent __result)
    {
        if (encounterData.opponentType == Opponent.Type.Default || !ProgressionData.LearnedMechanic(MechanicsConcept.OpponentQueue))
            return true; // For default opponents or if we're in the tutorial, just let the base game logic flow

        // This mostly just follows the logic of the base game, other than the fact that the
        // opponent gets instantiated by looking up the type from the list

        GameObject gameObject = new()
        {
            name = "Opponent"
        };

        __result = gameObject.AddComponent(AllOpponents.First(o => o.Id == encounterData.opponentType).Opponent) as Opponent;

        string typeName = string.IsNullOrWhiteSpace(encounterData.aiId) ? "AI" : encounterData.aiId;
        __result.AI = Activator.CreateInstance(CustomType.GetType("DiskCardGame", typeName)) as AI;
        __result.NumLives = __result.StartingLives;
        __result.OpponentType = encounterData.opponentType;
        __result.TurnPlan = __result.ModifyTurnPlan(encounterData.opponentTurnPlan);
        __result.Blueprint = encounterData.Blueprint;
        __result.Difficulty = encounterData.Difficulty;
        __result.ExtraTurnsToSurrender = SeededRandom.Range(0, 3, SaveManager.SaveFile.GetCurrentRandomSeed());
        return false;
    }

    [HarmonyReversePatch(HarmonyReversePatchType.Original)]
    [HarmonyPatch(typeof(BossBattleSequencer), nameof(BossBattleSequencer.GetSequencerIdForBoss))]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string OriginalGetSequencerIdForBoss(Opponent.Type bossType) { throw new NotImplementedException(); }

    [HarmonyPrefix, HarmonyPatch(typeof(BossBattleSequencer), nameof(BossBattleSequencer.GetSequencerIdForBoss))]
    private static bool ReplaceGetSequencerId(Opponent.Type bossType, ref string __result)
    {
        __result = AllOpponents.First(o => o.Id == bossType).SpecialSequencerId;
        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(BossBattleNodeData), nameof(BossBattleNodeData.PrefabPath), MethodType.Getter)]
    private static bool ReplacePrefabPath(ref string __result, Opponent.Type ___bossType)
    {
        string fullPath = "Prefabs/Map/MapNodesPart1/MapNode_" + ___bossType;
        GameObject obj = ResourceBank.Get<GameObject>(fullPath);
        __result = obj != null ? fullPath : "Prefabs/Map/MapNodesPart1/MapNode_ProspectorBoss";
        return false;
    }

    [HarmonyPatch(typeof(Opponent), nameof(Opponent.CreateCard))]
    [HarmonyPrefix]
    private static bool CloneCardInfo(ref CardInfo cardInfo)
    {
        // Dynamic costs require a unique CardInfo to get the playable card
        cardInfo = (CardInfo)cardInfo.Clone();
        return true;
    }

    [HarmonyPatch]
    private class MapGenerator_CreateNode
    {
        private static readonly MethodInfo ProcessMethodInfo = AccessTools.Method(typeof(MapGenerator_CreateNode), nameof(ProcessBossType), new Type[] { typeof(NodeData) });

        internal static ItemData currentItemData = null;

        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(MapGenerator), "CreateNode");
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // === We want to turn this

            // nodeData = new BossBattleNodeData();
            // (nodeData as BossBattleNodeData).bossType = RunState.CurrentMapRegion.bosses[Random.Range(0, RunState.CurrentMapRegion.bosses.Count)];

            // === Into this

            // nodeData = new BossBattleNodeData();
            // (nodeData as BossBattleNodeData).bossType = RunState.CurrentMapRegion.bosses[Random.Range(0, RunState.CurrentMapRegion.bosses.Count)];
            // ProcessBossType(nodeData);

            // ===
            FieldInfo bossTypeField = AccessTools.Field(typeof(BossBattleNodeData), "bossType");

            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction codeInstruction = codes[i];
                if (codeInstruction.opcode == OpCodes.Stfld)
                {
                    if (codeInstruction.operand == bossTypeField)
                    {
                        codes.Insert(++i, new CodeInstruction(OpCodes.Ldloc_0));
                        codes.Insert(++i, new CodeInstruction(OpCodes.Call, ProcessMethodInfo));
                        break;
                    }
                }
            }

            return codes;
        }

        public static void ProcessBossType(NodeData nodeData)
        {
            BossBattleNodeData bossBattleNodeData = (BossBattleNodeData)nodeData;

            // Parse data
            List<Opponent.Type> bosses = RunStateOpponents;
            if (bosses.Count == 0)
            {
                bosses.Add(Opponent.Type.ProspectorBoss);
                bosses.Add(Opponent.Type.AnglerBoss);
                bosses.Add(Opponent.Type.TrapperTraderBoss);
            }

            bossBattleNodeData.bossType = bosses[RunState.CurrentRegionTier];
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(CardDrawPiles), nameof(CardDrawPiles.ExhaustedSequence))]
    private static IEnumerator CustomBossExhaustionSequence(IEnumerator enumerator, CardDrawPiles __instance)
    {
        if (TurnManager.Instance.Opponent is ICustomExhaustSequence exhaustSeq && exhaustSeq != null)
        {
            Singleton<ViewManager>.Instance.SwitchToView(View.CardPiles, immediate: false, lockAfter: true);
            yield return new WaitForSeconds(1f);
            yield return exhaustSeq.DoCustomExhaustSequence(__instance);
        }
        else
        {
            yield return enumerator;
        }
    }
    #endregion

    #region Optimization Patches

    [HarmonyPatch]
    internal class SpawnScenery_Patches
    {
        [HarmonyPrefix, HarmonyPatch(typeof(Part1BossOpponent), nameof(Part1BossOpponent.SpawnScenery))]
        private static bool DisableTableScenery(Part1BossOpponent __instance)
        {
            // Show run method if scenery is disabled
            if (InscryptionAPIPlugin.configHideAct1BossScenery.Value)
            {
                __instance.sceneryObject = new GameObject("TemporaryScenary");
                __instance.sceneryObject.AddComponent<Animation>();
                if (__instance is PirateSkullBossOpponent)
                    __instance.sceneryObject.AddComponent<PirateSkullBossCannons>();
                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PirateSkullBossCannons), nameof(PirateSkullBossCannons.AimCannons))]
        [HarmonyPatch(typeof(PirateSkullBossCannons), nameof(PirateSkullBossCannons.FireLeftSide))]
        [HarmonyPatch(typeof(PirateSkullBossCannons), nameof(PirateSkullBossCannons.FireRightSide))]
        [HarmonyPatch(typeof(PirateSkullBossCannons), nameof(PirateSkullBossCannons.ResetLeftSide))]
        [HarmonyPatch(typeof(PirateSkullBossCannons), nameof(PirateSkullBossCannons.ResetRightSide))]
        private static bool DisableCannonAnims()
        {
            if (InscryptionAPIPlugin.configHideAct1BossScenery.Value)
                return false;

            return true;
        }
    }

    [HarmonyPatch]
    internal class PreventCallsOnScenary_Patches
    {
        private static readonly Type StartNewPhaseSequence = Type.GetType("DiskCardGame.TrapperTraderBossOpponent+<StartNewPhaseSequence>d__6, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
        private static readonly MethodInfo PlayAnimationInfo = AccessTools.Method(typeof(PreventCallsOnScenary_Patches), nameof(PlayAnimation), new[] { typeof(Animation), typeof(string) });

        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(StartNewPhaseSequence, "MoveNext");
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // We want to change
            //
            // sceneryObject.GetComponent<Animation>().Play("knives_table_exit");
            //
            // To
            //
            // PlayAnimation(sceneryObject.GetComponent<Animation>(), "knives_table_exit");

            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldstr && codes[i].operand == "knives_table_exit")
                {

                    // ldstr "knives_table_exit" 
                    codes[i + 1] = new CodeInstruction(OpCodes.Call, PlayAnimationInfo); // callvirt instance bool [UnityEngine.AnimationModule]UnityEngine.Animation::Play(string)
                    // pop
                    break;
                }
            }

            return codes;
        }

        private static bool PlayAnimation(Animation animation, string key)
        {
            if (InscryptionAPIPlugin.configHideAct1BossScenery.Value)
            {
                InscryptionAPIPlugin.Logger.LogInfo("PlayAnimation false");
                return false;
            }

            InscryptionAPIPlugin.Logger.LogInfo("PlayAnimation true");
            return animation.Play(key);
        }
    }
    #endregion
}