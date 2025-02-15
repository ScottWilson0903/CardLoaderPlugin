using DiskCardGame;
using GBC;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace InscryptionAPI.Localizing;

public static partial class LocalizationManager
{
    [HarmonyPatch]
    internal static class Patches
    {
        [HarmonyPatch(typeof(Localization), nameof(Localization.ReadCSVFileIntoTranslationData))]
        [HarmonyPostfix]
        private static void Localization_ReadCSVFileIntoTranslationData(Language language)
        {
            if (!AlreadyLoadedLanguages.Contains(language))
            {
                AlreadyLoadedLanguages.Add(language);
            }
            foreach (CustomTranslation translation in CustomTranslations)
            {
                InsertTranslation(translation);
            }

            OnLanguageLoaded?.Invoke(language);
        }

        [HarmonyPatch(typeof(Localization), nameof(Localization.TrySetToSystemLanguage))]
        [HarmonyPrefix]
        private static void Localization_TrySetToSystemLanguage()
        {
            foreach (CustomLanguage language in AllLanguages)
            {
                if (language.LanguageName == Application.systemLanguage.ToString())
                {
                    Localization.CurrentLanguage = language.Language;
                    return;
                }
            }
        }

        [HarmonyPatch(typeof(OptionsUI), nameof(OptionsUI.OnSetLanguageButtonPressed))]
        [HarmonyPrefix]
        private static bool OptionsUI_OnSetLanguageButtonPressed(OptionsUI __instance)
        {
            if (__instance.languageField.Value >= (int)Language.NUM_LANGUAGES)
                Localization.CurrentLanguage = NewLanguages[(__instance.languageField.Value - (int)Language.NUM_LANGUAGES)].Language;
            else
                Localization.CurrentLanguage = (Language)__instance.languageField.Value;

            __instance.setLanguageButton.SetEnabled(enabled: false);
            Singleton<InteractionCursor>.Instance.SetEnabled(enabled: false);
            CustomCoroutine.WaitThenExecute(0.1f, delegate
            {
                MenuController.ReturnToStartScreen();
                StartScreenController.startedGame = false;
            }, unscaledTime: true);
            return false;
        }

        [HarmonyPatch(typeof(Localization), nameof(Localization.ReadCSVFileIntoTranslationData))]
        [HarmonyPrefix]
        private static bool Localization_ReadCSVFileIntoTranslationData(Localization __instance, Language language)
        {
            if (language >= Language.NUM_LANGUAGES)
            {
                string table = NewLanguages.Find((a) => a.Language == language).PathToStringTable;
                ImportStringTable(table, language);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(FontReplacementData), nameof(FontReplacementData.Initialize))]
        [HarmonyPostfix]
        private static void FontReplacementData_Initialize()
        {
            foreach (CachedReplacement replacement in TemporaryFontReplacements)
            {
                AddFontReplacement(replacement.language, replacement.replacement);
            }
            TemporaryFontReplacements.Clear();
        }

        [HarmonyPatch(typeof(OptionsUI), nameof(OptionsUI.OnLanguageChanged))]
        [HarmonyPostfix]
        private static void OptionsUI_OnLanguageChanged(OptionsUI __instance, int newValue)
        {
            int indexOf = AllLanguages.FindIndex((a) => a.Language == Localization.CurrentLanguage);
            __instance.setLanguageButton.gameObject.SetActive(newValue != indexOf);
        }

        [HarmonyPatch(typeof(OptionsUI), nameof(OptionsUI.InitializeLanguageField))]
        [HarmonyPrefix]
        private static bool OptionsUI_InitializeLanguageField(OptionsUI __instance)
        {
            __instance.languageField.AssignTextItems(new List<string>(AllLanguageNames));

            int indexOf = AllLanguages.FindIndex((a) => a.Language == Localization.CurrentLanguage);
            __instance.languageField.ShowValue(indexOf, immediate: true);
            return false;
        }

        [HarmonyPatch(typeof(OptionsUI), nameof(OptionsUI.InitializeLanguageField))]
        [HarmonyPatch(typeof(OptionsUI), nameof(OptionsUI.OnLanguageChanged))]
        private static List<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // LocalizedLanguageNames.NAMES
            // LocalizedLanguageNames.SET_LANGUAGE_BUTTON_TEXT

            // To

            // LocalizationManager.AllLanguageNames
            // LocalizationManager.AllLanguageButtonText

            FieldInfo NAMES = AccessTools.Field(typeof(LocalizedLanguageNames), nameof(LocalizedLanguageNames.NAMES));
            FieldInfo SET_LANGUAGE_BUTTON_TEXT = AccessTools.Field(typeof(LocalizedLanguageNames), nameof(LocalizedLanguageNames.SET_LANGUAGE_BUTTON_TEXT));

            FieldInfo NEW_NAMES = AccessTools.Field(typeof(LocalizationManager), nameof(LocalizationManager.AllLanguageNames));
            FieldInfo NEW_SET_LANGUAGE_BUTTON_TEXT = AccessTools.Field(typeof(LocalizationManager), nameof(LocalizationManager.AllLanguageButtonText));

            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction code = codes[i];
                if (code.operand == NAMES)
                {
                    code.operand = NEW_NAMES;
                }
                else if (code.operand == SET_LANGUAGE_BUTTON_TEXT)
                {
                    code.operand = NEW_SET_LANGUAGE_BUTTON_TEXT;
                }
            }

            return codes;
        }
    }
}
