using DiskCardGame;
using InscryptionAPI.Items.Extensions;
using System.Collections.ObjectModel;
using UnityEngine;
using static InscryptionAPI.Boons.BoonManager;
using static InscryptionAPI.Slots.SlotModificationManager;

namespace InscryptionAPI.RuleBook;


public static class RuleBookManager
{
    public class RedirectInfo
    {
        public PageRangeType redirectType;
        public Color redirectTextColour;
        public string redirectPageId;

        public RedirectInfo(PageRangeType redirectType, Color redirectTextColour, string redirectPageId)
        {
            this.redirectType = redirectType;
            this.redirectTextColour = redirectTextColour;
            this.redirectPageId = redirectPageId;
        }
    }
    /// <summary>
    /// A custom class that associates a RuleBookPageInfo object with a Dictionary of RuleBookDescriptionRedirects. Use if 
    /// </summary>
    public class RuleBookPageInfoExt
    {
        public RuleBookPageInfo parentPageInfo;

        /// <summary>
        /// Tracks all rulebook redirects that this ability's description will have. Explanation of the variables is as follows:
        /// Key (string): the text that will be recoloured to indicate that it's clickable.
        /// Tuple.Item1 (PageRangeType): the type of page the redirect will go to. Use PageRangeType.Unique if you want to redirect to a custom rulebook page using its pageId.
        /// Tuple.Item2 (Color): the colour the Key text will be recoloured to.
        /// Tuple.Item3 (string): the id that the API will match against to find the redirect page. Eg, for ability redirects this will be the Ability id as a string.
        /// </summary>
        public Dictionary<string, RedirectInfo> RulebookDescriptionRedirects = new();

        public RuleBookPageInfoExt(RuleBookPageInfo parentPageInfo, Dictionary<string, RedirectInfo> redirects)
        {
            this.parentPageInfo = parentPageInfo;
            this.RulebookDescriptionRedirects = redirects;
        }
    }

    public static readonly List<RuleBookPageInfoExt> ConstructedPagesWithRedirects = new();
    public static readonly List<RuleBookPageInfoExt> CustomRedirectPages = new();

    internal static void AddRedirectPage(RuleBookPageInfo pageInfo, Dictionary<string, RedirectInfo> redirects)
    {
        ConstructedPagesWithRedirects.Add(new(pageInfo, redirects));
    }

    public class FullRuleBookRangeInfo
    {
        /// <summary>
        /// The GUID for the mod that added this rulebook section.
        /// </summary>
        public readonly string ModGUID;
        /// <summary>
        /// What PageRangeType this rulebook section will inherit from and use as a template.
        /// </summary>
        /// <remarks>
        /// Ability pages have a small square icon on the left side of the page.
        /// Stat icon pages have a small square icon on the left side of the rulebook name.
        /// Boon pages have a large icon on both sides of the page.
        /// Item pages have a large icon on the left side of the page.
        /// Unique is unused by the game, so should be avoided here unless you plan on using a unique page template.
        /// </remarks>
        public readonly PageRangeType PageTypeTemplate;

        /// <summary>
        /// The beginning section of the header, typically containing an appendix number and subsection number.
        /// An example of what HeaderPrefix should be: "APPENDIX XII, SUBSECTION X".
        /// </summary>
        /// <remarks>
        /// Unless you plan on using a custom appendix number or some other unique header, you should use FillHeaderPrefix .
        /// </remarks>
        public string HeaderPrefix;
        /// <summary>
        /// The subsection name for this rulebook range. Examples include "ABILITIES" and "VARIABLE STATS".
        /// </summary>
        public string SubSectionName;
        /// <summary>
        /// The full text of the header, combining the prefix and subsection name.
        /// </summary>
        public string FullHeaderText => HeaderPrefix + " - " + SubSectionName + " {0}";
        /// <summary>
        /// The function used to determine what the starting page number is; this number is added to the header.
        /// </summary>
        public Func<List<RuleBookPageInfo>, int> GetStartingNumberFunc;
        /// <summary>
        /// The function used to determine what index in the rulebook to begin inserting this rulebook range.
        /// </summary>
        public Func<PageRangeInfo, List<RuleBookPageInfo>, int> GetInsertPositionFunc;
        /// <summary>
        /// The function for filling out each RuleBookPageInfo.
        /// </summary>
        public Func<RuleBookInfo, PageRangeInfo, AbilityMetaCategory, List<RuleBookPageInfo>> CreatePagesFunc;

        public Action<RuleBookPage, string, object[]> FillPageAction;

        public int GetStartingNumber(List<RuleBookPageInfo> pages) => GetStartingNumberFunc(pages);
        public int GetInsertPosition(PageRangeInfo currentPageRange, List<RuleBookPageInfo> pages) => GetInsertPositionFunc(currentPageRange, pages);
        public List<RuleBookPageInfo> CreatePages(RuleBookInfo instance, PageRangeInfo currentPageRange, AbilityMetaCategory metaCategory) => CreatePagesFunc(instance, currentPageRange, metaCategory);
        public void FillRuleBookPage(RuleBookPage instance, string pageId, params object[] otherArgs) => FillPageAction(instance, pageId, otherArgs);

        public FullRuleBookRangeInfo(
            string modGuid, PageRangeType type, string headerPrefix, string subsectionName,
            Func<List<RuleBookPageInfo>, int> getStartingNumberFunc,
            Func<PageRangeInfo, List<RuleBookPageInfo>, int> getInsertPositionFunc,
            Func<RuleBookInfo, PageRangeInfo, AbilityMetaCategory, List<RuleBookPageInfo>> createPagesFunc,
            Action<RuleBookPage, string, object[]> fillPageAct)
        {
            this.ModGUID = modGuid;
            this.PageTypeTemplate = type;
            this.HeaderPrefix = headerPrefix ?? HeaderPrefixSimple("I");
            this.SubSectionName = subsectionName;
            this.GetStartingNumberFunc = getStartingNumberFunc ?? ((List<RuleBookPageInfo> x) => 1);
            this.GetInsertPositionFunc = getInsertPositionFunc;
            this.CreatePagesFunc = createPagesFunc;
            this.FillPageAction = fillPageAct;
        }

        public FullRuleBookRangeInfo Clone()
        {
            return new(
                this.ModGUID, this.PageTypeTemplate,
                this.HeaderPrefix, this.SubSectionName,
                this.GetStartingNumberFunc, this.GetInsertPositionFunc,
                this.CreatePagesFunc,
                this.FillPageAction
                );
        }
    }

    internal readonly static ObservableCollection<FullRuleBookRangeInfo> NewRuleBookInfos = new();

    public static List<FullRuleBookRangeInfo> AllRuleBookInfos { get; private set; }
    public static event Func<List<FullRuleBookRangeInfo>, List<FullRuleBookRangeInfo>> ModifyRuleBookInfos;

    static RuleBookManager()
    {
        NewRuleBookInfos.CollectionChanged += static (_, _) =>
        {
            SyncRuleBookList();
        };
    }

    public static void SyncRuleBookList()
    {
        AllRuleBookInfos = NewRuleBookInfos.Select(x => x.Clone()).ToList();
        AllRuleBookInfos = ModifyRuleBookInfos?.Invoke(AllRuleBookInfos) ?? AllRuleBookInfos;
    }

    /// <summary>
    /// Creates a custom rulebook range. Simplified version of the full New() method that only requires the minimumrequired parameters.
    /// </summary>
    /// <param name="modGuid">The GUID of the mod adding this FullRuleBookRangeInfo</param>
    /// <param name="pageType">The PageRangeType we want to inherit from. This will determine the style of pages.</param>
    /// <param name="subsectionName">The name of the rulebook subsection, eg. "Abilities" or "Boons".</param>
    /// <param name="getInsertPositionFunc">The Func to determine what index in the rulebook to insert a new section.</param>
    /// <param name="createPagesFunc">The Func that is called to create the RuleBookPage objects that will be used to fill the rulebook.</param>
    /// <param name="headerPrefix">The first half of the header that will appear on rulebook pages. Leave null to use the default prefix.</param>
    /// <param name="getStartingNumberFunc">The Func to determine the first page number in this range. Leave null to use the default starting number (1).</param>
    /// <param name="fillPageAction">The Action to call when filling in the data for each rulebook page.</param>
    /// <returns>A new FullRuleBookRangeInfo containing the information to create a custom rulebook range.</returns>
    public static FullRuleBookRangeInfo New(
    string modGuid,
    PageRangeType pageType,
    string subsectionName,
    Func<PageRangeInfo, List<RuleBookPageInfo>, int> getInsertPositionFunc,
    Func<RuleBookInfo, PageRangeInfo, AbilityMetaCategory, List<RuleBookPageInfo>> createPagesFunc,
    string headerPrefix = null,
    Func<List<RuleBookPageInfo>, int> getStartingNumberFunc = null,
    Action<RuleBookPage, string, object[]> fillPageAction = null)
    {
        FullRuleBookRangeInfo info = new(
            modGuid, pageType,
            headerPrefix, subsectionName,
            getStartingNumberFunc,
            getInsertPositionFunc, createPagesFunc,
            fillPageAction);
        NewRuleBookInfos.Add(info);
        return info;
    }

    /// <summary>
    /// Creates a FullRuleBookRangeInfo representing a custom subsection of the rulebook.
    /// </summary>
    /// <param name="modGuid">The GUID of the mod calling this method.</param>
    /// <param name="pageType">The PageRangeType we want to inherit from. This will determine what page style you'll have access to.</param>
    /// <param name="headerPrefix">The first half of the header that will appear on rulebook pages.</param>
    /// <param name="subsectionName">The name of the rulebook subsection, eg. "Abilities" or "Boons".</param>
    /// <param name="getStartingNumberFunc">The </param>
    /// <param name="getInsertPositionFunc"></param>
    /// <param name="createPagesFunc"></param>
    /// <returns></returns>
    public static FullRuleBookRangeInfo New(
        string modGuid, PageRangeType pageType, string headerPrefix, string subsectionName,
        Func<List<RuleBookPageInfo>, int> getStartingNumberFunc,
        Func<PageRangeInfo, List<RuleBookPageInfo>, int> getInsertPositionFunc,
        Func<RuleBookInfo, PageRangeInfo, AbilityMetaCategory, List<RuleBookPageInfo>> createPagesFunc)
    {

        FullRuleBookRangeInfo info = new(
            modGuid, pageType,
            headerPrefix, subsectionName,
            getStartingNumberFunc,
            getInsertPositionFunc, createPagesFunc,
            null);
        NewRuleBookInfos.Add(info);
        return info;
    }
    /// <summary>
    /// Sets the FillPage Action for the given FullRuleBookRangeInfo.
    /// </summary>
    /// <param name="info">The FullRuleBookRangeInfo we want to modify.</param>
    /// <param name="action"></param>
    /// <returns>The same FullRuleBookRangeInfo so a chain can continue.</returns>
    public static FullRuleBookRangeInfo SetFillPage(this FullRuleBookRangeInfo info, Action<RuleBookPage, string, object[]> action)
    {
        info.FillPageAction = action;
        return info;
    }
    /// <summary>
    /// Sets the HeaderPrefix for the provided FullRuleBookRangeInfo.
    /// </summary>
    /// <param name="info">The FullRuleBookRangeInfo we want to modify.</param>
    /// <param name="headerPrefix">The string we want our header prefix to become.</param>
    /// <remarks>
    /// If you only want to modify the subsection numeral, use SetPrefixSimple instead.
    /// </remarks>
    /// <returns>The same FullRuleBookRangeInfo so a chain can continue.</returns>
    public static FullRuleBookRangeInfo SetHeaderPrefix(this FullRuleBookRangeInfo info, string headerPrefix)
    {
        info.HeaderPrefix = headerPrefix;
        return info;
    }
    /// <summary>
    /// Sets the FullRuleBookRangeInfo's HeaderPrefix using the HeaderPrefixSimple method (see there more for information).
    /// </summary>
    /// <param name="info">The FullRuleBookRangeInfo we want to modify.</param>
    /// <param name="romanNumeral">A string ideally representing a roman numeral, used for the subsection number.</param>
    /// <returns>The same FullRuleBookRangeInfo so a chain can continue.</returns>
    public static FullRuleBookRangeInfo SetHeaderPrefixSimple(this FullRuleBookRangeInfo info, string romanNumeral)
    {
        info.HeaderPrefix = HeaderPrefixSimple(romanNumeral);
        return info;
    }
    /// <summary>
    /// Creates a header prefix with the provided string using DEFAULT_HEADER_PREFIX as a template.
    /// </summary>
    /// <param name="romanNumeral">A ideally representing a roman numeral for the subsection number.</param>
    /// <returns>A fully formatted header in the format of "APPENDIX XII, SUBSECTION {0}", where {0} is romanNumeral.</returns>
    public static string HeaderPrefixSimple(string romanNumeral) => string.Format(DEFAULT_HEADER_PREFIX, romanNumeral);

    private const string DEFAULT_HEADER_PREFIX = "APPENDIX XII, SUBSECTION {0}";
    public const string HTML_REPLACE_STRING = "<color=#{0}><u>{1}</u></color>";

    public static string ParseRedirectTextColours(Dictionary<string, RedirectInfo> dictionary, string description)
    {
        string desc = description;
        if (dictionary.Count > 0)
        {
            foreach (string key in dictionary.Keys)
            {
                RedirectInfo triple = dictionary[key];
                string hexCode = ColorUtility.ToHtmlStringRGB(triple.redirectTextColour);

                desc = desc.Replace(key, string.Format(HTML_REPLACE_STRING, hexCode, key));
            }
        }
        return desc;
    }

    /// <summary>
    /// Returns a RuleBookPageInfo's pageId without the API identifier.
    /// </summary>
    public static string GetUnformattedPageId(string pageId)
    {
        int start = pageId.IndexOf(RuleBookManagerPatches.API_ID);
        if (start != -1)
        {
            int end = pageId.IndexOf(']');
            if (end != -1) return pageId.Remove(start, end - start + 1);
        }

        return pageId;
    }

    /// <summary>
    /// Opens the rulebook to the page with the given pageId.
    /// </summary>
    /// <param name="instance">RuleBookController.Instance.</param>
    /// <param name="pageId">The value to compare each rulebook pages' id against. If this starts with "[API_" then it will check against a page's unformatted id.</param>
    /// <param name="offsetView">Whether to offset the camera view down when opening the rulebook.</param>
    public static void OpenToCustomPage(this RuleBookController instance, string pageId, bool offsetView = false)
    {
        int pageIndex = -1;
        instance.SetShown(shown: true, offsetView);
        if (pageId.StartsWith(RuleBookManagerPatches.API_ID))
            pageIndex = instance.PageData.IndexOf(instance.PageData.Find(x => !string.IsNullOrEmpty(x.pageId) && x.pageId == pageId));
        else
            pageIndex = instance.PageData.IndexOf(instance.PageData.Find(x => !string.IsNullOrEmpty(x.pageId) && GetUnformattedPageId(x.pageId) == pageId));

        instance.StopAllCoroutines();
        instance.StartCoroutine(instance.flipper.FlipToPage(pageIndex, 0.2f));
    }

    public static bool ItemShouldBeAdded(ConsumableItemData item, AbilityMetaCategory metaCategory)
    {
        return item.rulebookCategory == metaCategory || item.GetFullConsumableItemData()?.rulebookMetaCategories.Contains(metaCategory) == true;
    }
    public static bool BoonShouldBeAdded(FullBoon fullBoon, AbilityMetaCategory metaCategory)
    {
        return fullBoon?.boon?.icon != null && fullBoon.appearInRulebook && fullBoon.metaCategories.Contains(metaCategory);
    }
    public static bool SlotModShouldBeAdded(Info info, ModificationMetaCategory category) => info.RulebookName != null && info.MetaCategories.Contains(category);
}