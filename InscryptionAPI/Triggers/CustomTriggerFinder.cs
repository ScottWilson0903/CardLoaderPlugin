using DiskCardGame;
using InscryptionAPI.Card;
using InscryptionAPI.Slots;
using Sirenix.Serialization.Utilities;
using System.Collections;

namespace InscryptionAPI.Triggers;

/// <summary>
/// Finds custom trigger recievers that exists on the board/in the hand
/// Always excludes facedowns by default, use <see cref="IActivateWhenFacedown"/> to alter this default
/// </summary>
public static class CustomTriggerFinder
{
    /// <summary>
    /// Same as normal coroutine, but also handles stack size, number of triggers this battle and destruction of a receiver after activation.
    /// </summary>
    /// <param name="receiver">The receiver that is triggering.</param>
    /// <param name="triggerCoroutine">The trigger coroutine called by that receiver.</param>
    /// <returns>.</returns>
    public static IEnumerator CustomTriggerSequence(this TriggerReceiver receiver, IEnumerator triggerCoroutine)
    {
        GlobalTriggerHandler self = GlobalTriggerHandler.Instance;
        if (self != null)
        {
            self.NumTriggersThisBattle += 1;
            self.StackSize += 1;
            receiver.Activating = true;
            yield return triggerCoroutine;
            self.StackSize -= 1;
            receiver.Activating = false;
            if (receiver.DestroyAfterActivation)
            {
                receiver.Destroy();
            }
        }
        yield break;
    }

    #region Triggering
    /// <summary>
    /// Triggers the triggers of type T on all cards, both in hand and on board.
    /// </summary>
    /// <typeparam name="T">The trigger type to search for.</typeparam>
    /// <param name="triggerFacedown">True if this should also trigger cards that are facedown, false otherwise.</param>
    /// <param name="respond">Check function that needs to return true for a trigger to get triggered.</param>
    /// <param name="trigger">Trigger function that triggers the coroutine for the trigger.</param>
    /// <returns>.</returns>
    public static IEnumerator TriggerAll<T>(bool triggerFacedown, Func<T, bool> respond, Func<T, IEnumerator> trigger)
    {
        var all = FindGlobalTriggers<T>(triggerFacedown);
        foreach (T trigg in all)
        {
            if ((trigg as TriggerReceiver) != null && respond(trigg))
            {
                yield return CustomTriggerSequence(trigg as TriggerReceiver, trigger(trigg));
            }
        }
        if (!triggerFacedown)
        {
        }
        yield break;
    }

    /// <summary>
    /// Triggers the triggers of type T on a card trigger handler.
    /// </summary>
    /// <typeparam name="T">The trigger type to search for.</typeparam>
    /// <param name="handler">The card trigger handler to search for triggers.</param>
    /// <param name="respond">Check function that needs to return true for a trigger to get triggered.</param>
    /// <param name="trigger">Trigger function that triggers the coroutine for the trigger.</param>
    /// <returns>.</returns>
    public static IEnumerator Trigger<T>(this CardTriggerHandler handler, Func<T, bool> respond, Func<T, IEnumerator> trigger)
    {
        var all = FindTriggersOnCard<T>(handler);
        foreach (T trigg in all)
        {
            if ((trigg as TriggerReceiver) != null && respond(trigg))
            {
                yield return CustomTriggerSequence(trigg as TriggerReceiver, trigger(trigg));
            }
        }
        yield break;
    }

    /// <summary>
    /// Triggers the triggers of type T on a card.
    /// </summary>
    /// <typeparam name="T">The trigger type to search for.</typeparam>
    /// <param name="card">The card trigger handler to search for triggers.</param>
    /// <param name="respond">Check function that needs to return true for a trigger to get triggered.</param>
    /// <param name="trigger">Trigger function that triggers the coroutine for the trigger.</param>
    /// <returns>.</returns>
    public static IEnumerator Trigger<T>(this PlayableCard card, Func<T, bool> respond, Func<T, IEnumerator> trigger)
    {
        yield return card.TriggerHandler.Trigger<T>(respond, trigger);
        yield break;
    }

    /// <summary>
    /// Triggers the triggers of type T on all cards on board.
    /// </summary>
    /// <typeparam name="T">The trigger type to search for.</typeparam>
    /// <param name="triggerFacedown">True if this should also trigger cards that are facedown, false otherwise.</param>
    /// <param name="respond">Check function that needs to return true for a trigger to get triggered.</param>
    /// <param name="trigger">Trigger function that triggers the coroutine for the trigger.</param>
    /// <returns>.</returns>
    public static IEnumerator TriggerOnBoard<T>(bool triggerFacedown, Func<T, bool> respond, Func<T, IEnumerator> trigger)
    {
        var all = FindTriggersOnBoard<T>(triggerFacedown);
        foreach (T trigg in all)
        {
            if ((trigg as TriggerReceiver) != null && respond(trigg))
            {
                yield return CustomTriggerSequence(trigg as TriggerReceiver, trigger(trigg));
            }
        }
        yield break;
    }

    /// <summary>
    /// Triggers the triggers of type T on all cards in hand.
    /// </summary>
    /// <typeparam name="T">The trigger type to search for.</typeparam>
    /// <param name="respond">Check function that needs to return true for a trigger to get triggered.</param>
    /// <param name="trigger">Trigger function that triggers the coroutine for the trigger.</param>
    /// <returns>.</returns>
    public static IEnumerator TriggerInHand<T>(Func<T, bool> respond, Func<T, IEnumerator> trigger)
    {
        var all = FindTriggersInHand<T>();
        foreach (T trigg in all)
        {
            if ((trigg as TriggerReceiver) != null && respond(trigg))
            {
                yield return CustomTriggerSequence(trigg as TriggerReceiver, trigger(trigg));
            }
        }
        yield break;
    }
    #endregion

    #region DataCollection
    /// <summary>
    /// Collects data from all cards, both in hand and on board.
    /// </summary>
    /// <typeparam name="T">The trigger type to search for.</typeparam>
    /// <typeparam name="T2">The object type to collect.</typeparam>
    /// <param name="collectFromFacedown">True if this should also collect from cards that are facedown, false otherwise.</param>
    /// <param name="respond">Check function that needs to return true for a trigger to get triggered.</param>
    /// <param name="collect">Collect function that returns the information that will be collected.</param>
    /// <returns>The list of all collected information.</returns>
    public static List<(TriggerReceiver, T2)> CollectDataAll<T, T2>(bool collectFromFacedown, Func<T, bool> respond, Func<T, T2> collect)
    {
        List<(TriggerReceiver, T2)> ret = new();
        var all = FindGlobalTriggers<T>(collectFromFacedown);
        foreach (T trigg in all)
        {
            if ((trigg as TriggerReceiver) != null && respond(trigg))
            {
                ret.Add((trigg as TriggerReceiver, collect(trigg)));
            }
        }
        return ret;
    }

    /// <summary>
    /// Collects data from all cards on board.
    /// </summary>
    /// <typeparam name="T">The trigger type to search for.</typeparam>
    /// <typeparam name="T2">The object type to collect.</typeparam>
    /// <param name="collectFromFacedown">True if this should also collect from cards that are facedown, false otherwise.</param>
    /// <param name="respond">Check function that needs to return true for a trigger to get triggered.</param>
    /// <param name="collect">Collect function that returns the information that will be collected.</param>
    /// <returns>The list of all collected information.</returns>
    public static List<(TriggerReceiver, T2)> CollectDataOnBoard<T, T2>(bool collectFromFacedown, Func<T, bool> respond, Func<T, T2> collect)
    {
        List<(TriggerReceiver, T2)> ret = new();
        var all = FindTriggersOnBoard<T>(collectFromFacedown);
        foreach (T trigg in all)
        {
            if ((trigg as TriggerReceiver) != null && respond(trigg))
            {
                ret.Add((trigg as TriggerReceiver, collect(trigg)));
            }
        }
        return ret;
    }

    /// <summary>
    /// Collects data from all cards in hand.
    /// </summary>
    /// <typeparam name="T">The trigger type to search for.</typeparam>
    /// <typeparam name="T2">The object type to collect.</typeparam>
    /// <param name="respond">Check function that needs to return true for a trigger to get triggered.</param>
    /// <param name="collect">Collect function that returns the information that will be collected.</param>
    /// <returns>The list of all collected information.</returns>
    public static List<(TriggerReceiver, T2)> CollectDataInHand<T, T2>(Func<T, bool> respond, Func<T, T2> collect)
    {
        List<(TriggerReceiver, T2)> ret = new();
        var all = FindTriggersInHand<T>();
        foreach (T trigg in all)
        {
            if ((trigg as TriggerReceiver) != null && respond(trigg))
            {
                ret.Add((trigg as TriggerReceiver, collect(trigg)));
            }
        }
        return ret;
    }

    /// <summary>
    /// Collects data from all triggers on a card.
    /// </summary>
    /// <typeparam name="T">The trigger type to search for.</typeparam>
    /// <typeparam name="T2">The object type to collect.</typeparam>
    /// <param name="self">The card to collect from.</param>
    /// <param name="respond">Check function that needs to return true for a trigger to get triggered.</param>
    /// <param name="collect">Collect function that returns the information that will be collected.</param>
    /// <returns>The list of all collected information.</returns>
    public static List<(TriggerReceiver, T2)> CollectData<T, T2>(this PlayableCard self, Func<T, bool> respond, Func<T, T2> collect)
    {
        return self.TriggerHandler.CollectData<T, T2>(respond, collect);
    }

    /// <summary>
    /// Collects data from all triggers on a card trigger handler.
    /// </summary>
    /// <typeparam name="T">The trigger type to search for.</typeparam>
    /// <typeparam name="T2">The object type to collect.</typeparam>
    /// <param name="self">The trigger handler to collect from.</param>
    /// <param name="respond">Check function that needs to return true for a trigger to get triggered.</param>
    /// <param name="collect">Collect function that returns the information that will be collected.</param>
    /// <returns>The list of all collected information.</returns>
    public static List<(TriggerReceiver, T2)> CollectData<T, T2>(this CardTriggerHandler self, Func<T, bool> respond, Func<T, T2> collect)
    {
        List<(TriggerReceiver, T2)> ret = new();
        var all = FindTriggersOnCard<T>(self);
        foreach (T trigg in all)
        {
            if ((trigg as TriggerReceiver) != null && respond(trigg))
            {
                ret.Add((trigg as TriggerReceiver, collect(trigg)));
            }
        }
        return ret;
    }
    #endregion

    #region Calling
    /// <summary>
    /// Calls triggers from all cards, both on board and in hand.
    /// </summary>
    /// <typeparam name="T">The trigger type to search for.</typeparam>
    /// <param name="triggerFacedown">True if this should also call cards that are facedown, false otherwise.</param>
    /// <param name="respond">Check function that needs to return true for a trigger to get triggered.</param>
    /// <param name="call">Call function that triggers the event you want to trigger.</param>
    /// <returns>The list of all called triggers.</returns>
    public static List<TriggerReceiver> CallAll<T>(bool triggerFacedown, Func<T, bool> respond, Action<T> call)
    {
        List<TriggerReceiver> called = new();
        var all = FindGlobalTriggers<T>(triggerFacedown);
        foreach (T trigg in all)
        {
            if ((trigg as TriggerReceiver) != null && respond(trigg))
            {
                called.Add(trigg as TriggerReceiver);
                call(trigg);
            }
        }
        if (!triggerFacedown)
        {
        }
        return called;
    }

    /// <summary>
    /// Calls triggers from all cards on board.
    /// </summary>
    /// <typeparam name="T">The trigger type to search for.</typeparam>
    /// <param name="triggerFacedown">True if this should also call cards that are facedown, false otherwise.</param>
    /// <param name="respond">Check function that needs to return true for a trigger to get triggered.</param>
    /// <param name="call">Call function that triggers the event you want to trigger.</param>
    /// <returns>The list of all called triggers.</returns>
    public static List<TriggerReceiver> CallOnBoard<T>(bool triggerFacedown, Func<T, bool> respond, Action<T> call)
    {
        List<TriggerReceiver> called = new();
        var all = FindTriggersOnBoard<T>(triggerFacedown);
        foreach (T trigg in all)
        {
            if ((trigg as TriggerReceiver) != null && respond(trigg))
            {
                called.Add(trigg as TriggerReceiver);
                call(trigg);
            }
        }
        if (!triggerFacedown)
        {
        }
        return called;
    }

    /// <summary>
    /// Calls triggers from all cards in hand.
    /// </summary>
    /// <typeparam name="T">The trigger type to search for.</typeparam>
    /// <param name="respond">Check function that needs to return true for a trigger to get triggered.</param>
    /// <param name="call">Call function that triggers the event you want to trigger.</param>
    /// <returns>The list of all called triggers.</returns>
    public static List<TriggerReceiver> CallInHand<T>(Func<T, bool> respond, Action<T> call)
    {
        List<TriggerReceiver> called = new();
        var all = FindTriggersInHand<T>();
        foreach (T trigg in all)
        {
            if ((trigg as TriggerReceiver) != null && respond(trigg))
            {
                called.Add(trigg as TriggerReceiver);
                call(trigg);
            }
        }
        return called;
    }

    /// <summary>
    /// Calls all triggers on a card.
    /// </summary>
    /// <typeparam name="T">The trigger type to search for.</typeparam>
    /// <param name="self">The card to call.</param>
    /// <param name="respond">Check function that needs to return true for a trigger to get triggered.</param>
    /// <param name="call">Call function that triggers the event you want to trigger.</param>
    /// <returns>The list of all called triggers.</returns>
    public static List<TriggerReceiver> Call<T>(this PlayableCard self, Func<T, bool> respond, Action<T> call)
    {
        return self.TriggerHandler.Call(respond, call);
    }

    /// <summary>
    /// Calls all triggers on a card trigger handler.
    /// </summary>
    /// <typeparam name="T">The trigger type to search for.</typeparam>
    /// <param name="self">The card trigger handler to call.</param>
    /// <param name="respond">Check function that needs to return true for a trigger to get triggered.</param>
    /// <param name="call">Call function that triggers the event you want to trigger.</param>
    /// <returns>The list of all called triggers.</returns>
    public static List<TriggerReceiver> Call<T>(this CardTriggerHandler self, Func<T, bool> respond, Action<T> call)
    {
        List<TriggerReceiver> called = new();
        var all = FindTriggersOnCard<T>(self);
        foreach (T trigg in all)
        {
            if ((trigg as TriggerReceiver) != null && respond(trigg))
            {
                called.Add(trigg as TriggerReceiver);
                call(trigg);
            }
        }
        return called;
    }
    #endregion

    /// <summary>
    /// Finds all trigger recievers, on the board and in the hand
    /// </summary>
    /// <param name="excluding">Card to exclude from the hand search.</param>
    /// <typeparam name="T">The trigger type to search for</typeparam>
    /// <returns>All trigger recievers of type T.</returns>
    public static IEnumerable<T> FindGlobalTriggers<T>(bool findFacedown, PlayableCard excluding = null)
    {
        var result = Enumerable.Empty<T>();

        if (BoardManager.Instance)
        {
            result = result.Concat(FindTriggersOnBoard<T>(findFacedown));
        }

        if (PlayerHand.Instance)
        {
            result = result.Concat(FindTriggersInHandExcluding<T>(excluding));
        }

        return result;
    }

    /// <summary>
    /// Finds all trigger recievers, on the board and in the hand
    /// </summary>
    /// <param name="excluding">Card to exclude from the hand search.</param>
    /// <typeparam name="T">The trigger type to search for</typeparam>
    /// <returns>All trigger recievers of type T.</returns>
    public static IEnumerable<T> FindGlobalTriggers<T>(PlayableCard excluding = null)
    {
        return FindGlobalTriggers<T>(true, excluding);
    }

    /// <summary>
    /// Find all trigger recievers in the hand
    /// </summary>
    /// <typeparam name="T">The trigger type to search for</typeparam>
    /// <returns>All trigger recievers of type T in the hand.</returns>
    public static IEnumerable<T> FindTriggersInHand<T>()
    {
        List<PlayableCard> handCache = new(PlayerHand.Instance.CardsInHand);
        return handCache.Where(c => c != null && c.InHand).SelectMany(FindTriggersOnCard<T>);
    }

    /// <summary>
    /// Find all trigger recievers in the hand
    /// </summary>
    /// <typeparam name="T">The trigger type to search for</typeparam>
    /// <returns>All trigger recievers of type T in the hand.</returns>
    public static IEnumerable<T> FindTriggersOnBoard<T>()
    {
        return FindTriggersOnBoard<T>(true);
    }

    /// <summary>
    /// Find all trigger recievers in the hand
    /// </summary>
    /// <typeparam name="T">The trigger type to search for</typeparam>
    /// <returns>All trigger recievers of type T in the hand.</returns>
    public static IEnumerable<T> FindTriggersOnBoard<T>(bool findFacedown)
    {
        List<PlayableCard> cardsOnBoardCache = new(BoardManager.Instance.CardsOnBoard);
        List<NonCardTriggerReceiver> triggerReceiverCache = new(GlobalTriggerHandler.Instance.nonCardReceivers);
        List<SlotModificationBehaviour> slotModificationCache = new(SlotModificationManager.Instance?.SlotReceivers?.Select(kvp => kvp.Value.Item2) ?? Enumerable.Empty<SlotModificationBehaviour>());
        return triggerReceiverCache.Where(x => !x.SafeIsUnityNull()).Where(x => x.TriggerBeforeCards).OfType<T>()
            .Concat(slotModificationCache.Where(x => !x.SafeIsUnityNull()).OfType<T>())
            .Concat(cardsOnBoardCache.Where(x => x != null && x.OnBoard && (!x.FaceDown || findFacedown)).SelectMany(FindTriggersOnCard<T>))
            .Concat(triggerReceiverCache.Where(x => !x.SafeIsUnityNull()).Where(x => !x.TriggerBeforeCards).OfType<T>())
            .Concat(cardsOnBoardCache.Where(x => x != null && x.OnBoard && x.FaceDown && !findFacedown).SelectMany(FindTriggersOnCard<T>)
            .Where(x => x is IActivateWhenFacedown && (x as IActivateWhenFacedown).ShouldTriggerCustomWhenFaceDown(typeof(T))));
    }

    /// <summary>
    /// Find all trigger recievers in the hand
    /// </summary>
    /// <param name="card">Card to exclude from the search.</param>
    /// <typeparam name="T">The trigger type to search for</typeparam>
    /// <returns>All trigger recievers of type T in the hand.</returns>
    public static IEnumerable<T> FindTriggersInHandExcluding<T>(PlayableCard card)
    {
        List<PlayableCard> handCache = new(PlayerHand.Instance.CardsInHand);
        return handCache.Where(x => x != null && x.InHand && x != card).SelectMany(FindTriggersOnCard<T>);
    }

    /// <summary>
    /// Finds all trigger recievers on a card
    /// </summary>
    /// <param name="card">The card to search.</param>
    /// <typeparam name="T">The type of reciever to search for</typeparam>
    /// <returns>All trigger recievers of type T on the card.</returns>
    public static IEnumerable<T> FindTriggersOnCard<T>(this PlayableCard card)
    {
        foreach (var recv in card.TriggerHandler.GetAllReceivers().OfType<T>())
        {
            yield return recv;
        }
    }

    /// <summary>
    /// Finds all trigger recievers on a card
    /// </summary>
    /// <param name="card">The card to search.</param>
    /// <typeparam name="T">The type of reciever to search for</typeparam>
    /// <returns>All trigger recievers of type T on the card.</returns>
    public static IEnumerable<T> FindTriggersOnCard<T>(this CardTriggerHandler card)
    {
        foreach (var recv in card.GetAllReceivers().OfType<T>())
        {
            yield return recv;
        }
    }

    /// <summary>
    /// Finds all trigger recievers on a cards in the opponent's queue
    /// </summary>
    /// <typeparam name="T">The type of reciever to search for</typeparam>
    /// <returns>All trigger recievers of type T in the opponent's queue.</returns>
    public static IEnumerable<T> FindTriggersInQueue<T>() =>
        TurnManager.Instance?.Opponent?.Queue.Where(card => card.TryGetComponent(out T _)).Select(card => card.GetComponent<T>()) ?? new T[] { };
}
