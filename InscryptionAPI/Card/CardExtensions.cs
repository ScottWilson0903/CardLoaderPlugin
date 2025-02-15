using DiskCardGame;
using InscryptionAPI.Helpers;
using UnityEngine;

namespace InscryptionAPI.Card;

public static partial class CardExtensions
{
    /// <summary>
    /// Gets the first card matching the given name, or null if it does not exist.
    /// </summary>
    /// <param name="cards">An enumeration of Inscryption cards.</param>
    /// <param name="name">The name to search for (case sensitive).</param>
    /// <returns>The first matching card, or null if no match.</returns>
    public static CardInfo CardByName(this IEnumerable<CardInfo> cards, string name) => cards.FirstOrDefault(c => c.name.Equals(name));

    private static Sprite GetPortrait(Texture2D portrait, TextureHelper.SpriteType spriteType, FilterMode? filterMode = null)
    {
        return portrait.ConvertTexture(spriteType, filterMode ?? FilterMode.Point);
    }

    #region Adders

    /// <summary>
    /// Adds any number of abilities to the card. Abilities can be added multiple times.
    /// </summary>
    /// <param name="info">CardInfo to access.</param>
    /// <param name="abilities">The abilities to add.</param>
    /// <returns>The same CardInfo so a chain can continue.</returns>
    public static CardInfo AddAbilities(this CardInfo info, params Ability[] abilities)
    {
        info.abilities ??= new();
        info.abilities.AddRange(abilities);
        return info;
    }

    /// <summary>
    /// Adds any number of appearance behaviors to the card. Duplicate appearance behaviors are ignored.
    /// </summary>
    /// <param name="info">CardInfo to access.</param>
    /// <param name="appearances">The appearances to add.</param>
    /// <returns>The same CardInfo so a chain can continue.</returns>
    public static CardInfo AddAppearances(this CardInfo info, params CardAppearanceBehaviour.Appearance[] appearances)
    {
        info.appearanceBehaviour ??= new();
        foreach (var app in appearances)
            if (!info.appearanceBehaviour.Contains(app))
                info.appearanceBehaviour.Add(app);
        return info;
    }

    /// <summary>
    /// Adds any number of decals to the card. Duplicate decals are ignored.
    /// </summary>
    /// <param name="info">CardInfo to access.</param>
    /// <param name="decals">The decals to add.</param>
    /// <returns>The same CardInfo so a chain can continue.</returns>
    public static CardInfo AddDecal(this CardInfo info, params Texture[] decals)
    {
        info.decals ??= new();
        foreach (var dec in decals)
            if (!info.decals.Contains(dec))
                info.decals.Add(dec);

        return info;
    }

    /// <summary>
    /// Adds any number of decals to the card. Duplicate decals are ignored.
    /// </summary>
    /// <param name="info">CardInfo to access.</param>
    /// <param name="decals">The paths to the .png files containing the decals (relative to the Plugins directory).</param>
    /// <returns>The same CardInfo so a chain can continue.</returns>
    public static CardInfo AddDecal(this CardInfo info, params string[] decals)
    {
        return decals == null
            ? info
            : info.AddDecal(decals.Select(d => TextureHelper.GetImageAsTexture(d)).ToArray());

    }

    /// <summary>
    /// Adds any number of metacategories to the card. Duplicate metacategories are ignored.
    /// </summary>
    /// <param name="info">CardInfo to access.</param>
    /// <param name="categories">The categories to add.</param>
    /// <returns>The same CardInfo so a chain can continue.</returns>
    public static CardInfo AddMetaCategories(this CardInfo info, params CardMetaCategory[] categories)
    {
        info.metaCategories ??= new();
        foreach (var app in categories)
            if (!info.metaCategories.Contains(app))
                info.metaCategories.Add(app);
        return info;
    }

    /// <summary>
    /// Adds any number of special abilities to the card. Duplicate special abilities are ignored.
    /// </summary>
    /// <param name="info">CardInfo to access.</param>
    /// <param name="abilities">The abilities to add.</param>
    /// <returns>The same CardInfo so a chain can continue.</returns>
    public static CardInfo AddSpecialAbilities(this CardInfo info, params SpecialTriggeredAbility[] abilities)
    {
        info.specialAbilities ??= new();
        foreach (var app in abilities)
            if (!info.specialAbilities.Contains(app))
                info.specialAbilities.Add(app);
        return info;
    }

    /// <summary>
    /// Adds any number of traits to the card. Duplicate traits are ignored.
    /// </summary>
    /// <param name="info">CardInfo to access.</param>
    /// <param name="traits">The traits to add.</param>
    /// <returns>The same CardInfo so a chain can continue.</returns>
    public static CardInfo AddTraits(this CardInfo info, params Trait[] traits)
    {
        info.traits ??= new();
        foreach (var app in traits)
            if (!info.traits.Contains(app))
                info.traits.Add(app);
        return info;
    }

    /// <summary>
    /// Adds any number of tribes to the card. Duplicate tribes are ignored.
    /// </summary>
    /// <param name="info">CardInfo to access.</param>
    /// <param name="tribes">The tribes to add.</param>
    /// <returns>The same CardInfo so a chain can continue.</returns>
    public static CardInfo AddTribes(this CardInfo info, params Tribe[] tribes)
    {
        info.tribes ??= new();
        foreach (var app in tribes)
            if (!info.tribes.Contains(app))
                info.tribes.Add(app);
        return info;
    }

    #endregion

    #region Removers

    /// <summary>
    /// Removes all stacks of any number of abilities from the card.
    /// </summary>
    /// <param name="info">Card to access.</param>
    /// <param name="abilities">The abilities to remove.</param>
    /// <returns>The same card info so a chain can continue.</returns>
    public static CardInfo RemoveAbilities(this CardInfo info, params Ability[] abilities)
    {
        if (info.abilities?.Count > 0)
        {
            foreach (Ability ab in abilities)
            {
                info.abilities.RemoveAll(a => a == ab);
            }
        }
        return info;
    }
    /// <summary>
    /// Removes any number of abilities from the card. Will remove one instance of each passed ability; multiple instances can be passed.
    /// </summary>
    /// <param name="info">Card to access.</param>
    /// <param name="abilities">The abilities to remove.</param>
    /// <returns>The same card info so a chain can continue.</returns>
    public static CardInfo RemoveAbilitiesSingle(this CardInfo info, params Ability[] abilities)
    {
        if (info.abilities?.Count > 0)
        {
            foreach (Ability ab in abilities)
            {
                info.abilities.Remove(ab);
            }
        }
        return info;
    }

    /// <summary>
    /// Removes any number of special abilities from the card.
    /// </summary>
    /// <param name="info">CardInfo to access.</param>
    /// <param name="abilities">The special abilities to remove.</param>
    /// <returns>The same CardInfo so a chain can continue.</returns>
    public static CardInfo RemoveSpecialAbilities(this CardInfo info, params SpecialTriggeredAbility[] abilities)
    {
        if (info.specialAbilities?.Count > 0)
        {
            foreach (var ab in abilities)
            {
                info.specialAbilities.RemoveAll(a => a == ab);
            }
        }
        return info;
    }

    /// <summary>
    /// Removes any number of special abilities from the card. Will remove one instance of each passed ability; multiple instances can be passed.
    /// </summary>
    /// <param name="info">CardInfo to access.</param>
    /// <param name="abilities">The special abilities to remove.</param>
    /// <returns>The same CardInfo so a chain can continue.</returns>
    public static CardInfo RemoveSpecialAbilitiesSingle(this CardInfo info, params SpecialTriggeredAbility[] abilities)
    {
        if (info.specialAbilities?.Count > 0)
        {
            foreach (var ab in abilities)
            {
                info.specialAbilities.Remove(ab);
            }
        }
        return info;
    }

    /// <summary>
    /// Removes any number of appearance behaviors from the card.
    /// </summary>
    /// <param name="info">CardInfo to access.</param>
    /// <param name="appearances">The appearances to remove.</param>
    /// <returns>The same CardInfo so a chain can continue.</returns>
    public static CardInfo RemoveAppearances(this CardInfo info, params CardAppearanceBehaviour.Appearance[] appearances)
    {
        if (info.appearanceBehaviour?.Count > 0)
        {
            foreach (var app in appearances)
            {
                info.appearanceBehaviour.RemoveAll(a => a == app);
            }
        }
        return info;
    }

    /// <summary>
    /// Removes any number of appearance behaviors from the card. Will remove one instance of each passed appearance; multiple instances can be passed.
    /// </summary>
    /// <param name="info">CardInfo to access.</param>
    /// <param name="appearances">The appearances to remove.</param>
    /// <returns>The same CardInfo so a chain can continue.</returns>
    public static CardInfo RemoveAppearancesSingle(this CardInfo info, params CardAppearanceBehaviour.Appearance[] appearances)
    {
        if (info.appearanceBehaviour?.Count > 0)
        {
            foreach (var app in appearances)
            {
                info.appearanceBehaviour.Remove(app);
            }
        }
        return info;
    }

    /// <summary>
    /// Removes any number of decals from the card.
    /// </summary>
    /// <param name="info">CardInfo to access.</param>
    /// <param name="decals">The decals to remove.</param>
    /// <returns>The same CardInfo so a chain can continue.</returns>
    public static CardInfo RemoveDecals(this CardInfo info, params Texture[] decals)
    {
        if (info.decals?.Count > 0)
        {
            foreach (var dec in decals)
            {
                info.decals.RemoveAll(d => d == dec);
            }
        }
        return info;
    }

    /// <summary>
    /// Removes any number of decals behaviors from the card. Will remove one instance of each passed decals; multiple instances can be passed.
    /// </summary>
    /// <param name="info">CardInfo to access.</param>
    /// <param name="decals">The decals to remove.</param>
    /// <returns>The same CardInfo so a chain can continue.</returns>
    public static CardInfo RemoveDecalsSingle(this CardInfo info, params Texture[] decals)
    {
        if (info.decals?.Count > 0)
        {
            foreach (var dec in decals)
            {
                info.decals.Remove(dec);
            }
        }
        return info;
    }

    /// <summary>
    /// Removes any number of metacategories from the card.
    /// </summary>
    /// <param name="info">CardInfo to access.</param>
    /// <param name="categories">The categories to remove.</param>
    /// <returns>The same CardInfo so a chain can continue.</returns>
    public static CardInfo RemoveMetaCategories(this CardInfo info, params CardMetaCategory[] categories)
    {
        if (info.metaCategories?.Count > 0)
        {
            foreach (var cat in categories)
            {
                info.metaCategories.RemoveAll(c => c == cat);
            }
        }
        return info;
    }

    /// <summary>
    /// Removes any number of metacategories behaviors from the card. Will remove one instance of each passed appearance; multiple instances can be passed.
    /// </summary>
    /// <param name="info">CardInfo to access.</param>
    /// <param name="categories">The categories to remove.</param>
    /// <returns>The same CardInfo so a chain can continue.</returns>
    public static CardInfo RemoveMetaCategoriesSingle(this CardInfo info, params CardMetaCategory[] categories)
    {
        if (info.metaCategories?.Count > 0)
        {
            foreach (var cat in categories)
            {
                info.metaCategories.Remove(cat);
            }
        }
        return info;
    }

    /// <summary>
    /// Removes any number of traits from the card.
    /// </summary>
    /// <param name="info">Card to access.</param>
    /// <param name="traits">The traits to remove.</param>
    /// <returns>The same card info so a chain can continue.</returns>
    public static CardInfo RemoveTraits(this CardInfo info, params Trait[] traits)
    {
        if (info.traits?.Count > 0)
        {
            foreach (Trait tr in traits)
            {
                if (info.HasTrait(tr))
                    info.traits.Remove(tr);
            }
        }
        return info;
    }

    /// <summary>
    /// Removes any number of traits from the card.
    /// </summary>
    /// <param name="info">Card to access.</param>
    /// <param name="tribes">The tribes to remove.</param>
    /// <returns>The same card info so a chain can continue.</returns>
    public static CardInfo RemoveTribes(this CardInfo info, params Tribe[] tribes)
    {
        if (info.traits?.Count > 0)
        {
            foreach (Tribe tr in tribes)
            {
                if (info.IsOfTribe(tr))
                    info.tribes.Remove(tr);
            }
        }
        return info;
    }

    /// <summary>
    /// Removes any number of CardMetaCategories from the card.
    /// </summary>
    /// <param name="info">Card to access.</param>
    /// <param name="cardMetaCategories">The CardMetaCategories to remove.</param>
    /// <returns>The same card info so a chain can continue.</returns>
    public static CardInfo RemoveCardMetaCategories(this CardInfo info, params CardMetaCategory[] cardMetaCategories)
    {
        if (info.metaCategories?.Count > 0)
        {
            foreach (CardMetaCategory cm in cardMetaCategories)
            {
                if (info.HasCardMetaCategory(cm))
                    info.metaCategories.Remove(cm);
            }
        }
        return info;
    }

    #endregion

    #region ModPrefixesAndTags

    // Sets the mod GUID that was derived from the call stack.
    internal static CardInfo SetModTag(this CardInfo info, string modGuid) => info.SetExtendedProperty("CallStackModGUID", modGuid);

    /// <summary>
    /// Gets the GUID of the mod that created this card.
    /// </summary>
    /// <param name="info">CardInfo to access.</param>
    /// <returns>The GUID of the mod that created this card or null if it wasn't found/is from the base game.</returns>
    public static string GetModTag(this CardInfo info) => info.GetExtendedProperty("CallStackModGUID");

    // Sets the mod prefix for the card.
    internal static CardInfo SetModPrefix(this CardInfo info, string modPrefix) => info.SetExtendedProperty("ModPrefix", modPrefix);

    /// <summary>
    /// Gets the card name prefix for this card.
    /// </summary>
    /// <param name="info">CardInfo to access.</param>
    /// <returns>The mod prefix for this card, or null if it wasn't found/is from the base game.</returns>
    public static string GetModPrefix(this CardInfo info) => info.GetExtendedProperty("ModPrefix");

    /// <summary>
    /// Checks whether the card's mod prefix is equal to the given string.
    /// </summary>
    /// <param name="info">CardInfo to access.</param>
    /// <param name="prefixToMatch">The prefix to check for.</param>
    /// <returns>True if the CardInfo's mod prefix equals prefixToMatch.</returns>
    public static bool ModPrefixIs(this CardInfo info, string prefixToMatch) => info.GetExtendedProperty("ModPrefix") == prefixToMatch;

    #endregion
}
