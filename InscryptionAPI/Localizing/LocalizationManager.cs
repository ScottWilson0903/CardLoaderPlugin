using HarmonyLib;
using InscryptionAPI.Guid;
using System.Text;
using TMPro;
using UnityEngine;

namespace InscryptionAPI.Localizing;

public static partial class LocalizationManager
{
    public class CustomLanguage
    {
        public string PluginGUID;
        public string LanguageName;
        public string LanguageCode; // For translating
        public string PathToStringTable;
        public Language Language;
    }

    public class CustomTranslation
    {
        public string PluginGUID;
        public Localization.Translation Translation;
    }

    private class CachedReplacement
    {
        public Language language;
        public FontReplacement replacement;
    }

    /// <summary>
    /// The list of Fonts used in Inscryption.
    /// </summary>
    public enum FontReplacementType
    {
        Liberation,
        Marksman,
        Misc3D,
        DaggerSquare,
        HeavyWeight
    }

    public static string[] AllLanguageNames = new string[] { };
    public static string[] AllLanguageButtonText = new string[] { };

    public static List<CustomLanguage> AllLanguages = new();
    public static List<CustomLanguage> NewLanguages = new();
    public static List<CustomTranslation> CustomTranslations = new();
    public static Action<Language> OnLanguageLoaded = null;

    private static List<Language> AlreadyLoadedLanguages = new();
    private static bool FontReplacementDataInitialized = false;
    private static List<CachedReplacement> TemporaryFontReplacements = new();

    static LocalizationManager()
    {
        AllLanguageNames = LocalizedLanguageNames.NAMES.ToArray();
        AllLanguageButtonText = LocalizedLanguageNames.SET_LANGUAGE_BUTTON_TEXT.ToArray();

        // add the default supported languages to the LocalisationManager
        foreach (Language language in Enum.GetValues(typeof(Language)).Cast<Language>().Where(l => l < Language.NUM_LANGUAGES))
        {
            CustomLanguage customLanguage = new()
            {
                PluginGUID = InscryptionAPIPlugin.ModGUID,
                LanguageName = language.ToString(),
                Language = language,
                LanguageCode = language switch
                {
                    Language.English => "en",
                    Language.French => "fr",
                    Language.Italian => "it",
                    Language.German => "de",
                    Language.Spanish => "es",
                    Language.BrazilianPortuguese => "pt",
                    Language.Turkish => "tr",
                    Language.Russian => "ru",
                    Language.Japanese => "ja",
                    Language.Korean => "ko",
                    Language.ChineseSimplified => "zhcn",
                    Language.ChineseTraditional => "zhtw",
                    _ => "UNKNOWN",
                }
            };
            AllLanguages.Add(customLanguage);
        }
    }

    public static Language NewLanguage(string pluginGUID, string languageName, string code, string resetButtonText, string stringTablePath = null, List<FontReplacement> fontReplacements = null)
    {
        Language language = GuidManager.GetEnumValue<Language>(pluginGUID, languageName);
        CustomLanguage customLanguage = new()
        {
            PluginGUID = pluginGUID,
            LanguageName = languageName,
            Language = language,
            LanguageCode = code,
            PathToStringTable = stringTablePath,
        };
        AllLanguages.Add(customLanguage);
        NewLanguages.Add(customLanguage);
        AllLanguageNames = AllLanguageNames.AddToArray(languageName);
        AllLanguageButtonText = AllLanguageButtonText.AddToArray(resetButtonText);

        if (fontReplacements != null)
        {
            foreach (FontReplacement replacement in fontReplacements)
            {
                AddFontReplacement(language, replacement);
            }
        }

        return language;
    }

    public static void AddFontReplacement(Language language, FontReplacement replacement)
    {
        if (FontReplacementData.instance == null)
        {
            TemporaryFontReplacements.Add(new CachedReplacement()
            {
                language = language,
                replacement = replacement
            });
            return;
        }

        FontReplacementData.LanguageFontReplacements replacements = FontReplacementData.instance.languageFontReplacements.Find((a) => a.languages.Contains(language));
        if (replacements == null)
        {
            replacements = new FontReplacementData.LanguageFontReplacements();
            replacements.name = language.ToString();
            replacements.fontReplacements = new List<FontReplacement>();
            replacements.languages = new List<Language>()
            {
                language
            };
            FontReplacementData.instance.languageFontReplacements.Add(replacements);
        }
        replacements.fontReplacements.Add(replacement);
    }

    public static FontReplacement GetFontReplacementForFont(FontReplacementType type, Font font = null, TMP_FontAsset tmpFont = null)
    {
        FontReplacement replacement;
        switch (type)
        {
            case FontReplacementType.Liberation:
                replacement = Resources.Load<FontReplacement>("data/localization/fontreplacement/LIBERATION_to_DAGGERSQUARE");
                break;
            case FontReplacementType.Marksman:
                replacement = Resources.Load<FontReplacement>("data/localization/fontreplacement/MARKSMAN_to_CHINESE-PIXEL");
                break;
            case FontReplacementType.Misc3D:
                replacement = Resources.Load<FontReplacement>("data/localization/fontreplacement/MISC3D_to_JP-SCRIPT");
                break;
            case FontReplacementType.DaggerSquare:
                replacement = Resources.Load<FontReplacement>("data/localization/fontreplacement/DAGGERSQUARE_to_JP-SANS");
                break;
            case FontReplacementType.HeavyWeight:
                replacement = Resources.Load<FontReplacement>("data/localization/fontreplacement/HEAVYWEIGHT_to_VICIOUSHUNGER");
                break;
            default:
                InscryptionAPIPlugin.Logger.LogError("Unknown font replacement type: " + type.ToString().ToUpper());
                return null;
        }

        if (replacement == null)
        {
            InscryptionAPIPlugin.Logger.LogError("Could not find font replacement for " + type.ToString().ToUpper());
            return null;
        }

        replacement = UnityObject.Instantiate(replacement);
        if (font != null || tmpFont != null)
        {
            replacement.replacementFont = font;
            replacement.replacementTMPFont = tmpFont;
        }

        return replacement;
    }

    private static void ImportStringTable(string stringTablePath, Language language)
    {
        string lines = File.ReadAllText(stringTablePath);
        try
        {
            using StringReader aReader = new(lines);
            CSVParser cSVParser = new(aReader, ',');
            List<string> list = new();
            int num = 0;
            while (cSVParser.NextLine(list))
            {
                if (list.Count < 3)
                {
                    continue;
                }
                string text = list[0];
                bool num2 = text.EndsWith("_F");
                Localization.Translation translation;
                if (num2)
                {
                    translation = Localization.translations[num - 1];
                }
                else if (num < Localization.translations.Count)
                {
                    translation = Localization.translations[num];
                }
                else
                {
                    translation = new Localization.Translation();
                    Localization.translations.Add(translation);
                }
                if (string.IsNullOrEmpty(translation.id))
                {
                    translation.id = text;
                    translation.englishString = list[1];
                    translation.englishStringFormatted = Localization.FormatString(list[1]);
                }
                if (num2)
                {
                    if (!string.IsNullOrEmpty(list[2]) && translation.values[language] != list[2])
                    {
                        translation.femaleGenderValues.Add(language, list[2]);
                    }
                    continue;
                }
                if (text != translation.id)
                {
                    InscryptionAPIPlugin.Logger.LogInfo("Mismatched Translation! Starting at: " + translation.id);
                }
                translation.values.Add(language, list[2]);
                num++;
            }
        }
        catch
        {
            InscryptionAPIPlugin.Logger.LogInfo("No Translation File for " + language);
        }
    }

    /// <summary>
    /// Adds a translation for a string into the provided Language.
    /// </summary>
    /// <param name="pluginGUID">The GUID of the mod adding the translation.</param>
    /// <param name="id">A unique identifier for this translation.</param>
    /// <param name="englishString">The original string.</param>
    /// <param name="translatedString">The original string translated into the target language.</param>
    /// <param name="language">The language this translation is for.</param>
    /// <returns>A CustomTranslation object corresponding to the created translation.</returns>
    public static CustomTranslation Translate(string pluginGUID, string id, string englishString, string translatedString, Language language)
    {
        CustomTranslation customTranslation = Get(englishString, id);

        bool newTranslation = customTranslation == null;
        if (newTranslation)
        {
            customTranslation = new CustomTranslation
            {
                PluginGUID = pluginGUID,
                Translation = new Localization.Translation()
            };
            customTranslation.Translation.id = id;
            customTranslation.Translation.englishString = id;
            customTranslation.Translation.englishStringFormatted = !string.IsNullOrEmpty(englishString) ? Localization.FormatString(englishString) : null;
            customTranslation.Translation.values = new Dictionary<Language, string>();
            customTranslation.Translation.femaleGenderValues = new Dictionary<Language, string>();
        }

        customTranslation.Translation.values[language] = translatedString;

        if (newTranslation)
        {
            return Add(customTranslation);
        }

        if (AlreadyLoadedLanguages.Count > 0)
        {
            InsertTranslation(customTranslation);
        }
        return customTranslation;
    }

    public static CustomTranslation Add(CustomTranslation translation)
    {
        CustomTranslations.Add(translation);
        if (AlreadyLoadedLanguages.Count > 0)
        {
            InsertTranslation(translation);
        }
        return translation;
    }

    public static CustomTranslation Get(string englishText, string id)
    {
        CustomTranslation translation = null;
        if (!string.IsNullOrEmpty(englishText))
        {
            translation = CustomTranslations.Find((a) => a.Translation.englishString == englishText);
        }

        if (translation == null && !string.IsNullOrEmpty(id))
        {
            translation = CustomTranslations.Find((a) => a.Translation.id == id);
        }

        return translation;
    }

    private static void InsertTranslation(CustomTranslation customTranslation)
    {
        Localization.Translation translation = null;
        if (!string.IsNullOrEmpty(customTranslation.Translation.englishStringFormatted))
        {
            translation = Localization.Translations.Find((a) => a.englishStringFormatted == customTranslation.Translation.englishStringFormatted);
        }
        else
        {
            translation = Localization.Translations.Find((a) => a.id == customTranslation.Translation.id);
        }

        if (translation == null)
        {
            translation = new Localization.Translation
            {
                id = customTranslation.Translation.id,
                englishString = customTranslation.Translation.englishString,
                englishStringFormatted = customTranslation.Translation.englishStringFormatted,
                values = new Dictionary<Language, string>(),
                femaleGenderValues = new Dictionary<Language, string>()
            };
            Localization.Translations.Add(translation);
        }

        // Update translations
        foreach (Language language in AlreadyLoadedLanguages)
        {
            if (customTranslation.Translation.values.TryGetValue(language, out string word))
            {
                translation.values[language] = word;
            }
            if (customTranslation.Translation.femaleGenderValues.TryGetValue(language, out string femaleWord))
            {
                translation.femaleGenderValues[language] = femaleWord;
            }
        }
    }

    /// <summary>
    /// Retrieves the LanguageCode for the given Language.
    /// </summary>
    public static string LanguageToCode(Language language)
    {
        CustomLanguage customLanguage = AllLanguages.Find((a) => a.Language == language);
        if (customLanguage != null)
        {
            return customLanguage.LanguageCode;
        }

        return null;
    }

    /// <summary>
    /// Retrieves the LanguageCode with the given LanguageCode.
    /// </summary>
    public static Language CodeToLanguage(string code)
    {
        CustomLanguage customLanguage = AllLanguages.Find((a) => a.LanguageCode == code);
        if (customLanguage != null)
        {
            return customLanguage.Language;
        }

        return Language.NUM_LANGUAGES;
    }

    public static void ExportAllToCSV()
    {
        // Exports all localisation to a spreadsheet in the APIs directory
        StringBuilder stringBuilder = new("id");
        for (int i = 0; i < AllLanguages.Count; i++)
        {
            var language = AllLanguages[i];
            stringBuilder.Append("," + language);
        }

        int keys = Localization.Translations.Count;
        for (var i = 0; i < keys; i++)
        {
            Localization.Translation translation = Localization.Translations[i];
            stringBuilder.Append($"\n{translation.id}");

            for (var j = 0; j < AllLanguages.Count; j++)
            {
                Language language = AllLanguages[j].Language;
                stringBuilder.Append(",");

                if (language == Language.English)
                {
                    stringBuilder.Append(translation.englishString);
                }
                else if (translation.values.TryGetValue(language, out string word))
                {
                    stringBuilder.Append(word);
                }
            }
        }

        string path = Path.Combine(InscryptionAPIPlugin.Directory, "localisation_table.csv");
        File.WriteAllText(path, stringBuilder.ToString());
        InscryptionAPIPlugin.Logger.LogInfo($"Exported .csv file to {path}");
    }

    [Obsolete("Use Translate() instead")]
    public static CustomTranslation New(string pluginGUID, string id, string englishString, string translatedString, Language language)
    {
        return Translate(pluginGUID, id, englishString, translatedString, language);
    }
}
