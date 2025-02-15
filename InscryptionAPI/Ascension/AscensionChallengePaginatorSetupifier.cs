using DiskCardGame;

namespace InscryptionAPI.Ascension;

// adding this comment to say that setupifier is a ridiculous word, very fun
public class AscensionChallengePaginatorSetupifier : ManagedBehaviour
{
    public void Start()
    {
        AscensionMenuScreenTransition screen = gameObject.GetComponent<AscensionMenuScreenTransition>();
        if (screen != null)
        {
            List<AscensionIconInteractable> icons = new(screen.screenInteractables.FindAll(
                x => x.GetComponent<AscensionIconInteractable>() != null).ConvertAll(x => x.GetComponent<AscensionIconInteractable>())
                );

            if (icons.Count > 0 && gameObject.GetComponent<AscensionChallengePaginator>() == null)
            {
                ChallengeManager.SyncChallengeList();
                AscensionChallengePaginator manager = gameObject.gameObject.AddComponent<AscensionChallengePaginator>();
                manager.Initialize(null, screen);
            }
        }
    }
}
