using DiskCardGame;
using GBC;
using HarmonyLib;
using InscryptionAPI.Pelts;
using System.Collections;
using UnityEngine;
using static DiskCardGame.TextDisplayer;

namespace InscryptionAPI.Dialogue;

public static class DialogueManager
{
    public class Dialogue
    {
        public DialogueEvent DialogueEvent;
        public string PluginGUID;
    }

    public class DialogueColor
    {
        public Color Color;
        public string ColorCode;
        public string PluginGUID;
    }

    public static List<Dialogue> CustomDialogue = new();
    public static List<DialogueColor> CustomDialogueColor = new();
    public static Dictionary<string, Color> ColorLookup = new();

    public static Dialogue Add(string pluginGUID, DialogueEvent dialogueEvent)
    {
        Dialogue dialogue = new()
        {
            PluginGUID = pluginGUID,
            DialogueEvent = dialogueEvent
        };

        CustomDialogue.RemoveAll(d => d.DialogueEvent.id == dialogueEvent.id);
        CustomDialogue.Add(dialogue);

        DialogueDataUtil.Data?.events?.RemoveAll(d => d.id == dialogueEvent.id);
        DialogueDataUtil.Data?.events?.Add(dialogueEvent);

        return dialogue;
    }

    public static DialogueColor AddColor(string pluginGUID, string code, Color color)
    {
        DialogueColor data = new()
        {
            PluginGUID = pluginGUID,
            ColorCode = code,
            Color = color
        };
        ColorLookup[code] = color;
        CustomDialogueColor.Add(data);
        return data;
    }

    public static DialogueEvent GenerateEvent(string pluginGUID, string name, List<CustomLine> mainLines, List<List<CustomLine>> repeatLines = null, DialogueEvent.MaxRepeatsBehaviour afterMaxRepeats =
        DialogueEvent.MaxRepeatsBehaviour.RandomDefinedRepeat, DialogueEvent.Speaker defaultSpeaker = DialogueEvent.Speaker.Single)
    {
        DialogueEvent ev = new();
        List<DialogueEvent.Speaker> speakers = new() { DialogueEvent.Speaker.Single };
        ev.id = name;
        ev.mainLines = new(mainLines != null ? mainLines.ConvertAll((x) => x.ToLine(speakers, defaultSpeaker)) : new());
        ev.repeatLines = repeatLines != null ? repeatLines.ConvertAll((x) => new DialogueEvent.LineSet(x.ConvertAll((x2) => x2.ToLine(speakers, defaultSpeaker)))) : new();
        ev.maxRepeatsBehaviour = afterMaxRepeats;
        ev.speakers = new(speakers);

        Add(pluginGUID, ev);
        return ev;
    }

    public static DialogueEvent GenerateTraderPeltsEvent(string pluginGUID, PeltManager.PeltData peltData, List<CustomLine> lines, List<List<CustomLine>> repeatLines = null)
    {
        return GenerateTraderPeltsEvent(pluginGUID, peltData.peltTierName ?? PeltManager.GetTierNameFromData(peltData), lines, repeatLines);
    }
    public static DialogueEvent GenerateTraderPeltsEvent(string pluginGUID, string peltTierName, List<CustomLine> lines, List<List<CustomLine>> repeatLines = null)
    {
        return GenerateEvent(pluginGUID, "TraderPelts" + peltTierName, lines, repeatLines);
    }

    public static DialogueEvent GenerateRegionIntroductionEvent(string pluginGUID, RegionData regionData, List<CustomLine> lines, List<List<CustomLine>> repeatLines = null)
    {
        return GenerateRegionIntroductionEvent(pluginGUID, regionData.name, lines, repeatLines);
    }
    public static DialogueEvent GenerateRegionIntroductionEvent(string pluginGUID, string regionName, List<CustomLine> lines, List<List<CustomLine>> repeatLines = null)
    {
        return GenerateEvent(pluginGUID, "Region" + regionName, lines, repeatLines);
    }

    /// <summary>
    /// A version of PlayDialogueEvent that can be used in the 3D Acts as well as Act 2.
    /// Effectively just adds a check for Act 2 and then runs the correct PlayDialogueEvent method.
    /// </summary>
    public static IEnumerator PlayDialogueEventSafe(string eventId,
        MessageAdvanceMode advanceMode = MessageAdvanceMode.Auto,
        EventIntersectMode intersectMode = EventIntersectMode.Wait,
        DialogueSpeaker speaker = null, TextBox.Style style = TextBox.Style.Neutral,
        TextBox.ScreenPosition screenPosition = TextBox.ScreenPosition.OppositeOfPlayer,
        string[] variableStrings = null,
        Action<DialogueEvent.Line> newLineCallback = null,
        bool adjustAudioVolume = true)
    {
        if (SaveManager.SaveFile.IsPart2)
        {
            yield return DialogueHandler.Instance.PlayDialogueEvent(eventId, style, speaker, variableStrings, screenPosition, adjustAudioVolume);
        }
        else
        {
            yield return TextDisplayer.Instance.PlayDialogueEvent(eventId, advanceMode, intersectMode, variableStrings, newLineCallback);
        }
    }

    /// <summary>
    /// A quick method to convert a card's CardTemple into the respective TextBox.Style.
    /// </summary>
    /// <param name="temple">The CardTemple we want to use.</param>
    /// <returns>The corresponding TextBox.Style.</returns>
    public static TextBox.Style GetStyleFromTemple(CardTemple temple) => (TextBox.Style)(int)temple;

    /// <summary>
    /// A method to grab the correct TextBox.Style based off the player's chosen ambition.
    /// </summary>
    /// <returns>The corresponding TextBox.Style.</returns>
    public static TextBox.Style GetStyleFromAmbition()
    {
        if (StoryEventsData.EventCompleted(StoryEvent.GBCUndeadAmbition))
        {
            return TextBox.Style.Undead;
        }
        else if (StoryEventsData.EventCompleted(StoryEvent.GBCNatureAmbition))
        {
            return TextBox.Style.Nature;
        }
        else if (StoryEventsData.EventCompleted(StoryEvent.GBCTechAmbition))
        {
            return TextBox.Style.Tech;
        }
        else
        {
            return TextBox.Style.Magic;
        }
    }

    #region Patches
    [HarmonyPatch(typeof(DialogueDataUtil), nameof(DialogueDataUtil.ReadDialogueData), new System.Type[] { })]
    public class DialogueDataUtil_ReadDialogueData
    {
        public static void Postfix()
        {
            List<string> ids = DialogueDataUtil.data.events.Select((a) => a.id).ToList();

            foreach (Dialogue dialogue in CustomDialogue)
            {
                if (ids.Contains(dialogue.DialogueEvent.id))
                {
                    InscryptionAPIPlugin.Logger.LogWarning("Attempting to add dialogue with id that already exists: " + dialogue.DialogueEvent.id);
                }
                DialogueDataUtil.data.events.Add(dialogue.DialogueEvent);
            }
        }
    }

    [HarmonyPatch(typeof(DialogueParser), nameof(DialogueParser.GetColorFromCode), new System.Type[] { typeof(string), typeof(Color) })]
    public class DialogueParser_GetColorFromCode
    {
        public static bool Prefix(DialogueParser __instance, string code, Color defaultColor, ref Color __result)
        {
            string stringValue = DialogueParser.GetStringValue(code, "c");
            if (ColorLookup.TryGetValue(stringValue, out Color value))
            {
                __result = value;
                return false;
            }

            return true;
        }
    }
    #endregion
}
