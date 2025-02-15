using DiskCardGame;
using GBC;
using System.Collections;
using UnityEngine;

namespace InscryptionAPI.Ascension;

public class ChallengeDisplayerPlus : ManagedBehaviour
{
    public static ChallengeDisplayerPlus TryAddChallengeDisplayerPlusToDisplayer(AscensionChallengeDisplayer displayer)
    {
        ChallengeDisplayerPlus plus = displayer.GetComponent<ChallengeDisplayerPlus>();
        if (plus == null)
        {
            plus = displayer.gameObject.AddComponent<ChallengeDisplayerPlus>();
            plus.originalTitlePos = displayer.titleText.transform.localPosition.y;
            if (displayer.descriptionText != null)
            {
                plus.originalDescriptionPos = displayer.descriptionText.transform.localPosition.y;
            }
            if (displayer.pointsText != null)
            {
                plus.originalPointsPos = displayer.pointsText.transform.localPosition.y;
            }
        }
        if (plus.incompatibilityText == null)
        {
            GameObject cloned = Instantiate(displayer.titleText.gameObject);
            cloned.name = "PixelTextLine_INCOMPATIBLE";
            cloned.transform.parent = displayer.titleText.transform.parent;
            float y = plus.originalTitlePos;
            if (displayer.descriptionText != null)
            {
                y = plus.originalDescriptionPos;
            }
            if (displayer.pointsText != null)
            {
                y = plus.originalPointsPos;
            }
            cloned.transform.localPosition = new(displayer.titleText.transform.localPosition.x, y, displayer.titleText.transform.localPosition.z);
            cloned.SetActive(true);
            plus.incompatibilityText = cloned.GetComponent<PixelText>();
            Color32 c = new(238, 244, 198, 255);
            plus.incompatibilityText.DefaultColor = c;
            plus.incompatibilityText.SetColor(c);
            plus.incompatibilityText.SetText("");
        }
        if (plus.dependencyText == null)
        {
            GameObject cloned = Instantiate(displayer.titleText.gameObject);
            cloned.name = "PixelTextLine_DEPENDENCY";
            cloned.transform.parent = displayer.titleText.transform.parent;
            float y = plus.originalTitlePos;
            if (displayer.descriptionText != null)
            {
                y = plus.originalDescriptionPos;
            }
            if (displayer.pointsText != null)
            {
                y = plus.originalPointsPos;
            }
            cloned.transform.localPosition = new(displayer.titleText.transform.localPosition.x, y, displayer.titleText.transform.localPosition.z);
            cloned.SetActive(true);
            plus.dependencyText = cloned.GetComponent<PixelText>();
            Color32 c = new(238, 244, 198, 255);
            plus.dependencyText.DefaultColor = c;
            plus.dependencyText.SetColor(c);
            plus.dependencyText.SetText("");
        }
        return plus;
    }

    public float GetBestAvailableY()
    {
        float y = originalTitlePos;
        if (displayer.descriptionText != null)
        {
            y = originalDescriptionPos;
        }
        if (displayer.pointsText != null)
        {
            y = originalPointsPos;
        }
        return y;
    }

    public AscensionChallengeDisplayer displayer => GetComponent<AscensionChallengeDisplayer>();

    public void DisplayChallenge(AscensionChallengeInfo challengeInfo, bool immediate = false)
    {
        string dependency = "", incompatibility = "";
        ChallengeManager.FullChallenge fc = challengeInfo?.GetFullChallenge();
        if (fc != null && AscensionMenuScreens.Instance?.CurrentScreen == AscensionMenuScreens.Screen.SelectChallenges)
        {
            List<AscensionChallenge> dependencies = fc.DependantChallengeGetter?.Invoke(ChallengeManager.GetChallengeIcons())?.ToList();
            List<AscensionChallenge> incompatibilities = fc.IncompatibleChallengeGetter?.Invoke(ChallengeManager.GetChallengeIcons())?.ToList();
            dependencies?.RemoveAll(x => x == challengeInfo.challengeType);
            incompatibilities?.RemoveAll(x => x == challengeInfo.challengeType);
            if (dependencies != null && dependencies.Count > 0)
            {
                incompatibilities?.RemoveAll(dependencies.Contains);
                List<PrefixedString> dependencyStrings = new();
                foreach (AscensionChallenge d in dependencies)
                {
                    AscensionChallengeInfo info = d.GetInfo();
                    if (info != null)
                    {
                        PrefixedString existing = dependencyStrings.Find(x => x.challenge == d);
                        if (existing != null)
                        {
                            existing.number++;
                        }
                        else
                        {
                            PrefixedString pstr = new()
                            {
                                prefix = Localization.Translate(info.title),
                                challenge = d,
                                number = 1
                            };
                            dependencyStrings.Add(pstr);
                        }
                    }
                }
                if (dependencyStrings.Count > 0)
                {
                    List<string> actualStrings;
                    actualStrings = dependencyStrings.ConvertAll(x =>
                        !AscensionSaveData.Data.activeChallenges.Contains(x.challenge) ? $"[c:R]{x.FullText}[c:]" :
                        AscensionSaveData.Data.activeChallenges.Count(x2 => x2 == x.challenge) < x.number ? x.RedPrefixText : x.FullText);

                    dependency = "Depends on: " + string.Join(", ", actualStrings);
                }
            }
            if (incompatibilities != null && incompatibilities.Count > 0)
            {
                List<string> incompatibilityStrings = new();
                List<AscensionChallenge> challenges = new();
                foreach (AscensionChallenge d in incompatibilities)
                {
                    AscensionChallengeInfo info = d.GetInfo();
                    if (info != null && !challenges.Contains(d))
                    {
                        string prefix = AscensionSaveData.Data.activeChallenges.Contains(d) ? $"[c:R]{Localization.Translate(info.title)}[c:]" : Localization.Translate(info.title);
                        incompatibilityStrings.Add(prefix);
                        challenges.Add(d);
                    }
                }
                if (incompatibilityStrings.Count > 0)
                    incompatibility = "Incompatible with: " + string.Join(", ", incompatibilityStrings);
            }
        }
        DisplayText(dependency, incompatibility, immediate);
    }

    public void DisplayText(string dependency, string incompatibility, bool immediate = false)
    {
        float yOffset = (!string.IsNullOrEmpty(dependency) ? 0.11f : 0f) + (!string.IsNullOrEmpty(incompatibility) ? 0.11f : 0f);
        dependencyText.transform.localPosition = new(dependencyText.transform.localPosition.x, GetBestAvailableY(), dependencyText.transform.localPosition.z);
        incompatibilityText.transform.localPosition = new(incompatibilityText.transform.localPosition.x, GetBestAvailableY(), incompatibilityText.transform.localPosition.z);
        if (!string.IsNullOrEmpty(dependency) && !string.IsNullOrEmpty(incompatibility))
        {
            dependencyText.transform.localPosition = new(dependencyText.transform.localPosition.x, GetBestAvailableY() + 0.11f, dependencyText.transform.localPosition.z);
        }
        displayer.titleText.transform.localPosition = new(displayer.titleText.transform.localPosition.x, originalTitlePos + yOffset, displayer.titleText.transform.localPosition.z);
        if (displayer.descriptionText != null)
        {
            displayer.descriptionText.transform.localPosition = new(displayer.descriptionText.transform.localPosition.x, originalDescriptionPos + yOffset, displayer.descriptionText.transform.localPosition.z);
        }
        if (displayer.pointsText != null)
        {
            displayer.pointsText.transform.localPosition = new(displayer.pointsText.transform.localPosition.x, originalPointsPos + yOffset, displayer.pointsText.transform.localPosition.z);
        }
        if (immediate)
        {
            dependencyText.SetText(dependency, true);
            incompatibilityText.SetText(incompatibility, true);
        }
        else
        {
            StopAllCoroutines();
            StartCoroutine(DisplayTextSequence(dependency, incompatibility));
        }
    }

    private IEnumerator DisplayTextSequence(string dependency, string incompatibility)
    {
        dependencyText?.SetText("", false);
        incompatibilityText?.SetText("", false);
        yield return new WaitForSecondsRealtime(0.125f);
        if (displayer.descriptionText != null)
        {
            yield return new WaitForSecondsRealtime(0.05f);
        }
        if (displayer.pointsText != null)
        {
            yield return new WaitForSecondsRealtime(0.05f);
        }
        if (dependencyText != null && !string.IsNullOrEmpty(dependency))
        {
            yield return new WaitForSecondsRealtime(0.05f);
            dependencyText.SetText(dependency, true);
            CommandLineTextDisplayer.PlayCommandLineClickSound();
        }
        if (incompatibilityText != null && !string.IsNullOrEmpty(incompatibility))
        {
            yield return new WaitForSecondsRealtime(0.05f);
            incompatibilityText.SetText(incompatibility, true);
            CommandLineTextDisplayer.PlayCommandLineClickSound();
        }
        yield break;
    }

    private PixelText dependencyText;
    private PixelText incompatibilityText;
    private float originalTitlePos;
    private float originalDescriptionPos;
    private float originalPointsPos;

    public class PrefixedString
    {
        public string FullText => prefix + (number > 1 ? " (x" + number + ")" : "");
        public string RedPrefixText => prefix + (number > 1 ? " [c:R](x" + number + ")[c]" : "");
        public string prefix;
        public AscensionChallenge challenge;
        public int number;
    }
}