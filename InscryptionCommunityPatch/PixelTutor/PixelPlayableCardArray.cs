using DiskCardGame;
using GBC;
using InscryptionAPI.Helpers;
using InscryptionAPI.Helpers.Extensions;
using Pixelplacement;
using Sirenix.Utilities;
using System.Collections;
using UnityEngine;

namespace InscryptionCommunityPatch.PixelTutor;

public class PixelPlayableCardArray : ManagedBehaviour
{
    public PixelPlayableCard selectedCard = null;

    public readonly List<PixelPlayableCard> displayedCards = new();

    private PixelPlayableCard gameObjectReference = null;
    private GameObject overlay;
    private GenericUIButton forwardButton = null;
    private GenericUIButton backButton = null;

    private readonly int maxRows = 6;
    private readonly int maxCardsPerRow = 7;

    private readonly float leftAnchor = -1.75f;
    private readonly float centreAnchor = -0.52f;

    private readonly float tweenDurationModifier = 0.5f;
    private Vector3 offscreenPositionOffset = new(2f, 2f, -4f);

    private int numPages = 0;
    private int currentPageIndex = 0;

    private GamepadGridControl gamepadGrid;

    public IEnumerator DisplayUntilCancelled(List<CardInfo> cards,
        Func<bool> cancelCondition, Action cardsPlacedCallback = null)
    {
        //Debug.Log($"{cards.Exists(x => x == null)}");
        cards.RemoveAll(x => x == null);
        currentPageIndex = 0;
        numPages = 1 + (cards.Count - 1) / (maxCardsPerRow * maxRows);
        InitializeGamepadGrid();
        yield return SpawnAndPlaceCards(cards, GetNumRows(cards.Count), 0, isDeckReview: true);
        InitialiseButtons(cards, true, true);
        cardsPlacedCallback?.Invoke();
        SetCardsEnabled(enabled: true);
        if (numPages > 1)
        {
            forwardButton.gameObject.SetActive(true);
            backButton.gameObject.SetActive(true);
            EnableButtons(true);
        }
        yield return new WaitUntil(() => cancelCondition());
        SetCardsEnabled(enabled: false);
        yield return CleanUpCards();
        CleanUpButtons();
    }

    public IEnumerator SelectPixelCardFrom(
        List<CardInfo> cards, Action<PixelPlayableCard> cardSelectedCallback,
        Func<bool> cancelCondition = null, bool forPositiveEffect = true)
    {
        //Debug.Log($"{cards.Exists(x => x == null)}");
        cards.RemoveAll(x => x == null);

        currentPageIndex = 0;
        numPages = 1 + (cards.Count - 1) / (maxCardsPerRow * maxRows);
        if (cards.Count == 0)
            yield break;

        if (gameObjectReference == null)
        {
            PixelPlayableCard[] objs = Resources.FindObjectsOfTypeAll<PixelPlayableCard>();
            objs.Sort((a, b) => a.Anim.cardRenderer.sortingGroupOrder - b.Anim.cardRenderer.sortingGroupOrder);
            gameObjectReference = Instantiate(objs[0], base.transform);
            gameObjectReference.name = "TutorCardObjRef";
            gameObjectReference.transform.localPosition = new(-100f, -100f, 0); // go away
            gameObjectReference.transform.rotation.Set(0, 0, 0, 0);
        }

        if (overlay == null)
        {
            GameObject fade = PauseMenu.instance.menuParent.FindChild("GameFade");
            overlay = Instantiate(fade, base.transform);
            overlay.name = "TutorUIFade";
            var renderer = overlay.GetComponent<SpriteRenderer>();
            renderer.sortingLayerID = gameObjectReference.Anim.cardRenderer.sortingLayerID;
            renderer.sortingLayerName = gameObjectReference.Anim.cardRenderer.sortingLayerName;
            renderer.sortingGroupID = gameObjectReference.Anim.cardRenderer.sortingGroupID;
            renderer.sortingOrder = -7000;
        }

        InitializeGamepadGrid();

        yield return SpawnAndPlaceCards(cards, GetNumRows(cards.Count), 0, false, forPositiveEffect);
        InitialiseButtons(cards, false, forPositiveEffect);

        yield return new WaitForSeconds(0.15f);
        SetCardsEnabled(enabled: true);
        if (numPages > 1)
        {
            forwardButton.gameObject.SetActive(true);
            backButton.gameObject.SetActive(true);
            EnableButtons(true);
        }

        selectedCard = null;
        yield return new WaitUntil(() => selectedCard != null || (cancelCondition != null && cancelCondition()));
        SetCardsEnabled(enabled: false);
        if (selectedCard != null)
            displayedCards.Remove(selectedCard);

        yield return CleanUpCards();
        CleanUpButtons();
        forwardButton = backButton = null;

        if (selectedCard != null)
        {
            cards.Remove(selectedCard.Info);
            cardSelectedCallback?.Invoke(selectedCard);
        }
    }
    private void InitialiseButtons(List<CardInfo> cards, bool deckReview, bool forPositiveEffect)
    {
        if (forwardButton == null)
        {
            forwardButton = Instantiate(Singleton<HammerButton>.Instance.button, base.transform);
            forwardButton.name = "TutorForwardButton";
            forwardButton.cursorType = CursorType.MoveUp;
            forwardButton.defaultSprite = TextureHelper.GetImageAsSprite("forwardButton.png", typeof(PixelPlayableCardArray).Assembly, TextureHelper.SpriteType.PixelStandardButton);
            forwardButton.hoveringSprite = TextureHelper.GetImageAsSprite("forwardButton_hover.png", typeof(PixelPlayableCardArray).Assembly, TextureHelper.SpriteType.PixelStandardButton);
            forwardButton.disabledSprite = TextureHelper.GetImageAsSprite("forwardButton_disabled.png", typeof(PixelPlayableCardArray).Assembly, TextureHelper.SpriteType.PixelStandardButton);
            forwardButton.downSprite = TextureHelper.GetImageAsSprite("forwardButton_down.png", typeof(PixelPlayableCardArray).Assembly, TextureHelper.SpriteType.PixelStandardButton);
            forwardButton.transform.position = new(1.25f, -0.75f, 0f);
            forwardButton.CursorSelectStarted += delegate (MainInputInteractable x)
            {
                OnChangePage(true, cards, deckReview, forPositiveEffect);
            };

            backButton = Instantiate(Singleton<HammerButton>.Instance.button, base.transform);
            backButton.name = "TutorBackButton";
            backButton.cursorType = CursorType.MoveDown;
            backButton.defaultSprite = TextureHelper.GetImageAsSprite("backButton.png", typeof(PixelPlayableCardArray).Assembly, TextureHelper.SpriteType.PixelStandardButton);
            backButton.hoveringSprite = TextureHelper.GetImageAsSprite("backButton_hover.png", typeof(PixelPlayableCardArray).Assembly, TextureHelper.SpriteType.PixelStandardButton);
            backButton.disabledSprite = TextureHelper.GetImageAsSprite("backButton_disabled.png", typeof(PixelPlayableCardArray).Assembly, TextureHelper.SpriteType.PixelStandardButton);
            backButton.downSprite = TextureHelper.GetImageAsSprite("backButton_down.png", typeof(PixelPlayableCardArray).Assembly, TextureHelper.SpriteType.PixelStandardButton);
            backButton.transform.position = new(1.25f, -1f, 0f);
            backButton.CursorSelectStarted += delegate (MainInputInteractable x)
            {
                OnChangePage(false, cards, deckReview, forPositiveEffect);
            };
        }
        forwardButton.gameObject.SetActive(false);
        backButton.gameObject.SetActive(false);
        EnableButtons(false);
    }
    private void CleanUpButtons()
    {
        if (forwardButton.gameObject.active)
        {
            forwardButton.gameObject.SetActive(false);
            backButton.gameObject.SetActive(false);
        }
        //Destroy(forwardButton.gameObject);
        //Destroy(backButton.gameObject);
    }
    private void EnableButtons(bool enable)
    {
        forwardButton.SetEnabled(enable);
        backButton.SetEnabled(enable);
    }
    private void OnChangePage(bool forwards, List<CardInfo> cards, bool deckReview, bool forPositiveEffect)
    {
        EnableButtons(false);
        SetCardsEnabled(enabled: false);
        base.StartCoroutine(ChangePage(forwards, cards, deckReview, forPositiveEffect));
    }
    private IEnumerator ChangePage(bool forwards, List<CardInfo> cards, bool deckReview, bool forPositiveEffect)
    {
        yield return CleanUpCards();

        if (forwards)
            currentPageIndex = (currentPageIndex + 1) >= numPages ? 0 : currentPageIndex + 1;
        else
            currentPageIndex = (currentPageIndex - 1) < 0 ? (numPages - 1) : currentPageIndex - 1;

        yield return SpawnAndPlaceCards(cards, GetNumRows(cards.Count), currentPageIndex, deckReview, forPositiveEffect);
        SetCardsEnabled(enabled: true);
        EnableButtons(true);
    }
    protected Vector3 GetCardPosition(int cardIndex, int cardsToPlace, int numRows, int cardsPerRow)
    {
        int cardRowIndex = Mathf.FloorToInt(cardIndex / cardsPerRow);
        float xPos = GetXPos(cardIndex, cardRowIndex, cardsToPlace, cardsPerRow); // [-1.75, 0.7]
        float yPos = Mathf.Max(-0.801f, 0.451f - ((8.5f - numRows) * 0.1f * cardRowIndex)); // [0.451, -0.801]
        float zPos = 0f + 0.01f * cardRowIndex;
        return new Vector3(xPos, yPos, zPos);
    }
    private float GetXPos(int cardIndex, int cardRowIndex, int cardsToPlace, int cardsPerRow)
    {
        float cardGap = 0.41f;
        float anchor = leftAnchor;
        int positionInRow = cardsPerRow * cardRowIndex;
        int cardsLeftToPlace = cardsToPlace - cardsPerRow * cardRowIndex;
        float multiplier = cardIndex - positionInRow;

        // when there are <7 cards, we need to centre them
        if (PatchPlugin.act2TutorCenterRows.Value && cardsLeftToPlace > 0 && cardsLeftToPlace < cardsPerRow)
        {
            anchor = centreAnchor;
            multiplier = cardIndex - positionInRow - (cardsLeftToPlace - 1) / 2f;
        }

        return anchor + cardGap * multiplier;
    }
    protected IEnumerator CleanUpCards()
    {
        List<PixelPlayableCard> list = new(displayedCards);
        foreach (PixelPlayableCard item in list)
        {
            if (item != null)
            {
                Tween.Position(item.transform, item.transform.position + offscreenPositionOffset, 0.1f, 0f, Tween.EaseIn);
                item.Anim.PlayQuickRiffleSound();
                Destroy(item.gameObject, 0.1f);
                yield return new WaitForSeconds(CardPile.GetPauseBetweenCardTime(displayedCards.Count) * tweenDurationModifier * 0.5f);
            }
        }
        yield return new WaitForSeconds(0.1f);
        SetOverlayEnabled(false);
        SetBoardEnabled(true);
    }

    protected IEnumerator SpawnAndPlaceCards(List<CardInfo> cards, int numRows, int pageIndex, bool isDeckReview = false, bool forPositiveEffect = true)
    {
        SetOverlayEnabled(true);
        SetBoardEnabled(false);
        displayedCards.ForEach(delegate (PixelPlayableCard x)
        {
            if (x != null)
                Destroy(x.gameObject);
        });
        displayedCards.Clear();

        if (numRows <= 0)
        {
            PatchPlugin.Logger.LogDebug($"NumRows for PixelPlayableCardArray is 0, displaying no cards");
            yield break;
        }
        // can only show 42 cards per page
        int maxPerPage = maxRows * maxCardsPerRow;
        int startingIndex = maxPerPage * pageIndex;
        int cardsPerRow = Mathf.Min(maxCardsPerRow, cards.Count / numRows);
        int cardsToPlace = Mathf.Min(maxPerPage, cards.Count - startingIndex);

        for (int i = startingIndex; i < (startingIndex + cardsToPlace); i++)
        {
            // correct for the current page
            Vector3 cardPosition = GetCardPosition(i - startingIndex, cardsToPlace, numRows, cardsPerRow);
            PixelPlayableCard card = CreateAndPlaceCard(cards[i], cardPosition);
            if (card == null)
                yield break;

            yield return TriggerSpecialBehaviours(card, isDeckReview, forPositiveEffect);
            yield return new WaitForSeconds(CardPile.GetPauseBetweenCardTime(cards.Count) * tweenDurationModifier);
        }
    }

    protected void TweenInCard(Transform cardTransform, Vector3 cardPos)
    {
        cardTransform.localPosition = cardPos;
        Vector3 position = cardTransform.position;
        Vector3 vector2 = cardTransform.position = position + offscreenPositionOffset;
        Tween.Position(cardTransform, position, 0.15f, 0f, Tween.EaseOut);
    }

    private void InitializeGamepadGrid()
    {
        if (gamepadGrid == null)
        {
            gamepadGrid = base.gameObject.AddComponent<GamepadGridControl>();
            gamepadGrid.Rows = new List<GamepadGridControl.Row>();
        }
        gamepadGrid.Rows.Clear();
        // add a row for each possible row of cards, plus 1 for the buttons
        for (int i = 0; i < maxRows + 1; i++)
        {
            gamepadGrid.Rows.Add(new GamepadGridControl.Row());
        }
        gamepadGrid.enabled = false;
        gamepadGrid.enabled = true;
    }

    private PixelPlayableCard CreateAndPlaceCard(CardInfo info, Vector3 cardPos)
    {
        PixelPlayableCard component = Instantiate(gameObjectReference, base.transform);

        PixelCardAnimationController controller = component.Anim as PixelCardAnimationController;
        controller.cardRenderer.sortingGroupID = gameObjectReference.Anim.cardRenderer.sortingGroupID;
        controller.cardbackRenderer.sortingGroupID = gameObjectReference.Anim.cardRenderer.sortingGroupID;
        controller.cardRenderer.sortingOrder = -9000;
        controller.cardbackRenderer.sortingOrder = -8000;

        component.SetFaceDown(false, true);
        component.SetInfo(info);
        component.SetEnabled(enabled: false);
        component.transform.position = Vector3.zeroVector;

        gamepadGrid.Rows[0].interactables.Add(component);
        displayedCards.Add(component);
        TweenInCard(component.transform, cardPos);
        component.Anim.PlayQuickRiffleSound();
        return component;
    }

    private IEnumerator TriggerSpecialBehaviours(PixelPlayableCard card, bool isDeckReview, bool forPositiveEffect)
    {
        SpecialCardBehaviour[] components = card.GetComponents<SpecialCardBehaviour>();
        foreach (SpecialCardBehaviour specialCardBehaviour in components)
        {
            if (isDeckReview)
                specialCardBehaviour.OnShownInDeckReview();
            else
                yield return specialCardBehaviour.OnShownForCardSelect(forPositiveEffect);

        }
    }

    private void SetOverlayEnabled(bool enabled)
    {
        bool swap = false;
        while (overlay.activeSelf != enabled)
        {
            overlay.SetActive(swap);
            swap = !swap;
        }
    }

    private int GetNumRows(int numCards) => Mathf.Min(maxRows, numCards / maxCardsPerRow);
    private void SetCardsEnabled(bool enabled) => displayedCards.ForEach(x => x.SetEnabled(enabled));
    private void SetBoardEnabled(bool enabled)
    {
        IEnumerable<PixelPlayableCard> cards = PixelBoardManager.Instance.CardsOnBoard
            .Concat(TurnManager.Instance.Opponent.queuedCards)
            .Concat(PixelPlayerHand.Instance.CardsInHand)
            .Cast<PixelPlayableCard>();

        IEnumerable<HighlightedInteractable> slots = PixelBoardManager.Instance.AllSlots
            .Concat(PixelBoardManager.Instance.OpponentQueueSlots)
            .Cast<HighlightedInteractable>();

        PixelCombatBell bell = (PixelCombatBell)Resources.FindObjectsOfTypeAll(typeof(PixelCombatBell)).FirstOrDefault();

        bell?.SetEnabled(enabled);
        Singleton<HammerButton>.Instance.gameObject.SetActive(enabled);

        foreach (HighlightedInteractable slot in slots)
        {
            slot.SetEnabled(enabled);
            slot.ShowState(enabled ? HighlightedInteractable.State.Interactable : HighlightedInteractable.State.Hidden);
        }
        foreach (PixelPlayableCard card in cards)
        {
            card.gameObject.SetActive(enabled);
        }
        TurnManager.Instance.PlayerCanInitiateCombat = enabled;
    }
}