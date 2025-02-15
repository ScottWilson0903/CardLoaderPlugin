global using UnityObject = UnityEngine.Object;

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using InscryptionCommunityPatch.Card;
using InscryptionCommunityPatch.ResourceManagers;
using InscryptionCommunityPatch.Tests;
using System.Runtime.CompilerServices;
using UnityEngine.SceneManagement;

[assembly: InternalsVisibleTo("Assembly-CSharp")]

namespace InscryptionCommunityPatch;

[BepInPlugin(ModGUID, ModName, ModVer)]
[BepInDependency("cyantist.inscryption.api")]
public class PatchPlugin : BaseUnityPlugin
{
    public const string ModGUID = "community.inscryption.patch";
    public const string ModName = "InscryptionCommunityPatch";
    public const string ModVer = "1.0.0";

    internal static PatchPlugin Instance;

    internal static ConfigEntry<bool> configEnergy;
    internal static ConfigEntry<bool> configDrone;
    internal static ConfigEntry<bool> configMox;
    internal static ConfigEntry<bool> configDroneMox;
    internal static ConfigEntry<bool> configShowSquirrelTribeOnCards;
    internal static ConfigEntry<bool> configAct3Bones;

    internal static ConfigEntry<bool> configResetEyes;
    internal static ConfigEntry<bool> undeadCatEmission;

    internal static ConfigEntry<bool> rightAct2Cost;
    internal static ConfigEntry<bool> act2CostRender;
    internal static ConfigEntry<bool> act2VanillaStyle;

    internal static ConfigEntry<bool> doubleStackSplit;

    internal static ConfigEntry<bool> act2StackIconType;

    internal static ConfigEntry<bool> configMergeOnBottom;

    internal static ConfigEntry<bool> configRemovePatches;

    internal static ConfigEntry<bool> configSmallPricetags;
    internal static ConfigEntry<bool> configMovePricetags;

    internal static ConfigEntry<bool> configTestState;

    internal static ConfigEntry<bool> configFullDebug;

    internal static ConfigEntry<bool> configDefaultDrone;

    internal static ConfigEntry<bool> act2TutorCenterRows;


    new internal static ManualLogSource Logger;

    private readonly Harmony HarmonyInstance = new(ModGUID);

    private void OnEnable()
    {
        Logger = base.Logger;

        HarmonyInstance.PatchAll(typeof(PatchPlugin).Assembly);
        SceneManager.sceneLoaded += this.OnSceneLoaded;

        if (configTestState.Value)
        {
            ExecuteCommunityPatchTests.PrepareForTests();
            TestCost.Init();
        }

        CommunityArtPatches.PatchCommunityArt();
    }

    private void OnDisable()
    {
        HarmonyInstance.UnpatchSelf();
    }

    private void Awake()
    {
        Instance = this;
        configEnergy = Config.Bind("Energy", "Energy Refresh", true, "Max energy increases and energy refreshes at end of turn");
        configDrone = Config.Bind("Energy", "Energy Drone", false, "Drone is visible to display energy (requires Energy Refresh)");
        configDefaultDrone = Config.Bind("Energy", "Default Drone", false, "Drone uses the vanilla model instead of being attached to the scales (requires Energy Drone)");
        configMox = Config.Bind("Mox", "Mox Refresh", true, "Mox refreshes at end of battle");
        configDroneMox = Config.Bind("Mox", "Mox Drone", false, "Drone displays mox (requires Energy Drone and Mox Refresh)");
        configAct3Bones = Config.Bind("Bones", "Act 3 Bones", false, "Force bones displayer to be active in Act 3");
        configShowSquirrelTribeOnCards = Config.Bind("Tribes", "Show Squirrel Tribe", false, "Shows the Squirrel tribe icon on cards");
        act2CostRender = Config.Bind("Card Costs", "GBC Cost render", true, "GBC Cards are able to display custom costs and hybrid costs through the API.");
        rightAct2Cost = Config.Bind("Card Costs", "GBC Cost On Right", true, "GBC Cards display their costs on the top-right corner. If false, display on the top-left corner");
        act2VanillaStyle = Config.Bind("Card Costs", "GBC Vanilla Render", false, "GBC cards use vanilla sprites when rendering multiple and custom costs.");
        configMergeOnBottom = Config.Bind("Sigil Display", "Merge_On_Botom", false, "Makes it so if enabled, merged sigils will display on the bottom of the card instead of on the artwork. In extreme cases, this can cause some visual bugs.");
        configRemovePatches = Config.Bind("Sigil Display", "Remove_Patches", false, "Makes it so if enabled, merged sigils will not have a patch behind them anymore and will instead be glowing yellow (only works with Merge_On_Bottom).");
        doubleStackSplit = Config.Bind("Sigil Display", "Vanilla Stacking", false, "If enabled, cards with only two visible sigils will display each separately even if they can stack.");

        configSmallPricetags = Config.Bind("Act 1", "Smaller Pricetags", false, "If enabled, the price tags placed on cards while buying from the Trapper will be scaled down.");
        configMovePricetags = Config.Bind("Act 1", "Move Pricetags", false, "If enabled, the price tags placed on cards while buying from the Trapper will be moved to the right.");
        act2StackIconType = Config.Bind("Sigil Display", "Act 2 Sigil icon type", true, "If true, stacking icons are a cream outline with a black center. If false, stacking icons are a black outline with a cream center. Act 2");
        act2TutorCenterRows = Config.Bind("Act 2", "Centred Hoarder UI", true, "If true, centres displayed cards in each row during the Hoarder selection sequence.");
        configFullDebug = Config.Bind("General", "Full Debug", true, "If true, displays all debug logs in the console.");
        configTestState = Config.Bind("General", "Test Mode", false, "Puts the game into test mode. This will cause (among potentially other things) a new run to spawn a number of cards into your opening deck that will demonstrate card behaviors.");
        configResetEyes = Config.Bind("Act 1", "Reset Red Eyes", false, "Resets Leshy's eyes to normal if they were turned red due to a boss fight's grizzly bear sequence.");
        undeadCatEmission = Config.Bind("General", "Undead Cat Emission", false, "If true, Undead Cat will have a forced red emission.");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnergyDrone.TryEnableEnergy(scene.name);
    }
}
