using DiskCardGame;
using TMPro;
using UnityEngine;

namespace InscryptionAPI.RuleBook;

// RuleBookRig/RigParent/RuleBook/Anim/PageFlipper/BookPage_1/Plane01
// RuleBookRig_Part3/RigParent/Tablet/Anim
// RulebookRig_Grimora/RigParent/RuleBook/Anim/PageFlipper/BookPage_1/Plane01
// CardBattle_Magnificus/RuleBookRig_Magnificus/RigParent/RuleBook/Anim/PageFlipper/BookPage_1/Plane01

/// <summary>
/// Controls everything regarding the creation and positioning of page text redirects.
/// </summary>
public class RuleBookRedirectManager : Singleton<RuleBookRedirectManager>
{
    public readonly List<GameObject> createdInteractableObjects = new();
    public readonly List<GameObject> currentActiveInteractables = new();
    public int currentActivePageIndex = -1;

    public Vector3 currentPageTopLeft;
    public Vector3 descriptionTopLeft;
    public float[] currentPageLengths;
    public float[] descriptionLengths;

    //public float currentPageRotation = 0f;

    public Camera RuleBookCamera;
    public Camera PixelCamera;

    public Transform currentTopPage;

    private void Start()
    {
        RuleBookCamera = RuleBookController.Instance.rigParent.GetComponentInChildren<Camera>();
        PixelCamera = ViewManager.Instance.pixelCamera;
    }

    public void ClearActiveInteractables()
    {
        currentActiveInteractables.Clear();
        createdInteractableObjects.RemoveAll(x => x == null);
        createdInteractableObjects.ForEach(x => x.SetActive(false));
    }
    public void UpdateActiveInteractables(TextMeshPro description, GameObject currentPageObj, Dictionary<string, RuleBookManager.RedirectInfo> redirects)
    {
        //InscryptionAPIPlugin.Logger.LogDebug($"[UpdateActiveInteractables]");
        Bounds pageBounds;
        Bounds borderBounds = currentPageObj.transform.Find("Border").GetComponent<SpriteRenderer>().bounds; // in world space
        Vector3 pageBottomLeft;
        float zLength;

        if (SaveManager.SaveFile.IsPart3)
        {
            pageBounds = currentTopPage.GetComponent<MeshRenderer>().bounds; // in world space

            // hard-set these due to to the pull-up animation messing up the coordinates
            currentPageTopLeft = new(-1.129529f, 6.869874f, -5.502338f);
            pageBottomLeft = new(-1.129529f, 5.6181f, -6.449201f);
            descriptionTopLeft = new(borderBounds.max.x, borderBounds.max.y, (borderBounds.max.z + borderBounds.min.z) / 2f);

            currentPageLengths = new float[2] { pageBounds.size.x - 0.4f, pageBounds.size.y - 0.4f };
            descriptionLengths ??= new float[2] { borderBounds.size.y, (currentPageLengths[1] / currentPageLengths[0]) * borderBounds.size.y };
        }
        else
        {
            pageBounds = currentTopPage.GetComponent<SkinnedMeshRenderer>().sharedMesh.bounds; // in local space
            descriptionTopLeft = new(borderBounds.min.x, borderBounds.max.y, (borderBounds.max.z + borderBounds.min.z) / 2f);

            currentPageTopLeft = currentTopPage.transform.TransformPoint(pageBounds.max);
            pageBottomLeft = currentTopPage.transform.TransformPoint(new(pageBounds.max.x, pageBounds.min.y, pageBounds.min.z));

            currentPageLengths = new float[2] { 2.95f, 1.97f }; // hard set these since the rulebook pages aren't completely flat
            descriptionLengths ??= new float[2] { borderBounds.size.x, (currentPageLengths[1] / currentPageLengths[0]) * borderBounds.size.x };
        }
        zLength = pageBottomLeft.z - currentPageTopLeft.z;
        //InscryptionAPIPlugin.Logger.LogDebug($"[DescTopLeft] {descriptionTopLeft.x} {descriptionTopLeft.y} {descriptionTopLeft.z}");
        //InscryptionAPIPlugin.Logger.LogDebug($"[PageTopLeft] {currentPageTopLeft.x} {currentPageTopLeft.y} {currentPageTopLeft.z}");
        //InscryptionAPIPlugin.Logger.LogDebug($"[PageBottomLeft] {pageBottomLeft.x} {pageBottomLeft.y} {pageBottomLeft.z} | {zLength}");

        ClearActiveInteractables();
        CreateInteractables(redirects, description, zLength);
        currentActiveInteractables.ForEach(x => x.SetActive(true));
    }

    /// <summary>
    /// Creates and positions the actual interactable object using the provided position and collider vectors.
    /// The position is the world position relative to the rulebook.
    /// Translates worldPosition to a screenpoint relative to the rulebook then back to world space relative to the pixel (player) camera.
    /// </summary>
    public void CreateInteractableObject(Vector3 worldPosition, Vector3 colliderSize, string keyText, RuleBookManager.RedirectInfo redirectInfo)
    {
        GameObject obj = createdInteractableObjects.Find(x => !currentActiveInteractables.Contains(x));
        if (obj == null)
        {
            InscryptionAPIPlugin.Logger.LogDebug("Creating new page interactable");
            obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.AddComponent<PageTextInteractable>().coll = obj.GetComponent<BoxCollider>();
            createdInteractableObjects.Add(obj);
        }

        PageTextInteractable com = obj.GetComponent<PageTextInteractable>();
        com.SetRedirect(redirectInfo.redirectType, redirectInfo.redirectPageId);
        //com.relativeToRulebookPosition = worldPosition;

        Vector3 newWorldPosition = PixelCamera.ScreenToWorldPoint(RuleBookCamera.WorldToScreenPoint(worldPosition));

        //InscryptionAPIPlugin.Logger.LogDebug($"[CreateInteractable] ({colliderSize.x} {colliderSize.y})");
        //InscryptionAPIPlugin.Logger.LogDebug($"[CreateInteractable] ({worldPosition.x} {worldPosition.y} {worldPosition.z}) ({newWorldPosition.x} {newWorldPosition.y} {newWorldPosition.z})");

        obj.name = $"RuleBookPageInteractable ({keyText})";
        obj.transform.SetParent(PixelCamera.transform);
        obj.transform.position = newWorldPosition;
        obj.transform.localScale = colliderSize;
        obj.transform.localRotation = Quaternion.identity;
        currentActiveInteractables.Add(obj);
    }

    /// <summary>
    /// Creates interactable objects using word indeces to determine the positioning and size of each interactable.
    /// Interactables will be the same size of the entire key phrase to minimise object creation.
    /// If the key text wraps onto a different line, an additional interactable will be created for the new line.
    /// </summary>
    private void CreateInteractables(Dictionary<string, RuleBookManager.RedirectInfo> redirectInfos, TextMeshPro description, float zLength)
    {
        float textHeight = -1;
        float textScalar = description.transform.localScale.x;
        foreach (string keyText in redirectInfos.Keys)
        {
            foreach (List<int> indeces in GetWordIndeces(description.textInfo, keyText))
            {
                int currentLine = -1;
                List<Vector3> colliderSizes = new();
                List<Vector3> colliderPositions = new();
                Vector3 bottomLeft = Vector3.zero, topRight = Vector3.zero;

                foreach (int index in indeces) // for each word in the entire phrase
                {
                    TMP_WordInfo word = description.textInfo.wordInfo[index];
                    List<TMP_CharacterInfo> charsInWord = description.textInfo.characterInfo.ToList().GetRange(word.firstCharacterIndex, word.lastCharacterIndex - word.firstCharacterIndex + 1);
                    //Debug.Log($"Index [{index}] [{word.GetWord()}] Count: {charsInWord.Count} [{charsInWord[0].character} {charsInWord.Last().character}]");

                    if (textHeight == -1) textHeight = charsInWord[0].topLeft.y - charsInWord[0].bottomLeft.y;

                    while (charsInWord.Count > 0)
                    {
                        if (currentLine == -1)
                        {
                            currentLine = charsInWord[0].lineNumber;
                            bottomLeft = charsInWord[0].bottomLeft;
                        }
                        List<TMP_CharacterInfo> charsOnLine = charsInWord.FindAll(x => x.lineNumber == currentLine);

                        charsInWord.RemoveAll(charsOnLine.Contains);
                        if (charsOnLine.Count > 0)
                        {
                            topRight = charsOnLine.Last().topRight;
                        }

                        // if there are still characters remaining then that means there's a new line
                        // so create collider data using current info then clear info
                        if (charsInWord.Count > 0)
                        {
                            //Debug.Log("Start new line");
                            CreateColliderSizeAndPosition(description.transform, textHeight, zLength, bottomLeft, topRight, colliderSizes, colliderPositions);
                            currentLine = -1;
                        }
                    }
                }
                if (currentLine != -1)
                {
                    //Debug.Log("Finalise line");
                    CreateColliderSizeAndPosition(description.transform, textHeight, zLength, bottomLeft, topRight, colliderSizes, colliderPositions);
                }

                for (int i = 0; i < colliderSizes.Count; i++)
                {
                    Vector3 size = colliderSizes[i];
                    Vector3 position = colliderPositions[i];
                    CreateInteractableObject(
                        colliderPositions[i],
                        colliderSizes[i] * textScalar * textScalar,
                        keyText,
                        redirectInfos[keyText]);
                }
            }
        }
    }

    /// <summary>
    /// Gets the indeces of every instance of every word in the key.
    /// </summary>
    public static IEnumerable<List<int>> GetWordIndeces(TMP_TextInfo textInfo, string key)
    {
        string[] splitKey = key.Split();
        TMP_WordInfo[] wordInfo = textInfo.wordInfo;
        List<List<int>> indeces = new();

        //Debug.Log($"[GetWordIndeces] {splitKey.Length} F: {splitKey.First()} L: {splitKey.Last()}");
        for (int i = 0; i < wordInfo.Length; i++)
        {
            if (wordInfo[i].characterCount == 0)
                continue;

            if (wordInfo[i].GetWord() == splitKey[0])
            {
                int k = -1;
                List<int> keyIndeces = new() { i };
                for (int j = 1; j < splitKey.Length; j++)
                {
                    k = j;
                    if (wordInfo[i + k].characterCount == 0)
                        continue;

                    if (wordInfo[i + k].GetWord() == splitKey[j])
                    {
                        //Debug.Log($"NextWord: {wordInfo[k].GetWord()}");
                        keyIndeces.Add(i + k);
                    }
                }
                if (k != -1) i += k;

                indeces.Add(keyIndeces);
            }
        }
        return indeces;
    }
    private void CreateColliderSizeAndPosition(Transform textMesh, float sizeY, float zLength, Vector3 bottomLeft, Vector3 topRight, List<Vector3> colliderSizes, List<Vector3> colliderPositions)
    {
        Vector3 bottomLeftWorld = textMesh.TransformPoint(bottomLeft);
        Vector3 topRightWorld = textMesh.TransformPoint(topRight);
        Vector3 wordPos = new(
            (topRightWorld.x + bottomLeftWorld.x) / 2f,
            (topRightWorld.y + bottomLeftWorld.y) / 2f,
            bottomLeftWorld.z + 0.01f
            );

        float correctedXProportion;
        float correctedX;
        float correctedYProportion;
        float zCorrection; // zCorrection stuff is completely made up nonsense
        float correctedY;
        Vector3 correctedPos;

        if (SaveManager.SaveFile.IsPart3)
        {
            correctedXProportion = (descriptionTopLeft.y - wordPos.y) / descriptionLengths[0];
            correctedX = currentPageTopLeft.x + (currentPageLengths[0] * correctedXProportion);

            correctedYProportion = (descriptionTopLeft.x - wordPos.x) / descriptionLengths[1];
            zCorrection = zLength * correctedYProportion;
            correctedY = currentPageTopLeft.y - (currentPageLengths[1] * correctedYProportion) - (zCorrection * correctedYProportion);
        }
        else
        {
            correctedXProportion = (wordPos.x - descriptionTopLeft.x) / descriptionLengths[0];
            correctedX = currentPageTopLeft.x + (currentPageLengths[0] * correctedXProportion);

            correctedYProportion = (descriptionTopLeft.y - wordPos.y) / descriptionLengths[1];
            zCorrection = zLength * correctedYProportion;
            correctedY = currentPageTopLeft.y - (currentPageLengths[1] * correctedYProportion) - (zCorrection * correctedYProportion);

        }
        correctedPos = new(correctedX, correctedY, currentPageTopLeft.z + (zCorrection + zCorrection * correctedYProportion));
        //Debug.Log($"[Corrected] ({correctedPos.x} {correctedPos.y} {correctedPos.z}) | {correctedXProportion} {correctedYProportion}");
        colliderPositions.Add(correctedPos);
        colliderSizes.Add(new((topRight.x - bottomLeft.x) + sizeY / 2f, sizeY * 3f / 2f, 0.001f)); // add padding to compensate for inaccurate positioning
    }
}