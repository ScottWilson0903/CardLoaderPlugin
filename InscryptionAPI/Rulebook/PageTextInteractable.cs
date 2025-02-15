using DiskCardGame;

namespace InscryptionAPI.RuleBook;

public class PageTextInteractable : AlternateInputInteractable
{
    public override CursorType CursorType => CursorType.Inspect;

    public PageRangeType redirectType;
    public string redirectId;

    /*public Vector3 relativeToRulebookPosition;
    public override void ManagedLateUpdate()
    {
        base.ManagedLateUpdate();
        // cx, cy - center of square coordinates
        // x, y - coordinates of a corner point of the square
        // theta is the angle of rotation

        // translate point to origin
        float cx = RuleBookRedirectManager.Instance.currentPageLengths[0] / 2f;
        float cy = RuleBookRedirectManager.Instance.currentPageLengths[1] / 2f;
        float x = RuleBookRedirectManager.Instance.currentPageTopLeft.x;
        float y = RuleBookRedirectManager.Instance.currentPageTopLeft.y;
        float theta = RuleBookRedirectManager.Instance.currentPageRotation;
        float tempX = x - cx;
        float tempY = y - cy;

        // now apply rotation
        float rotatedX = tempX * Mathf.Cos(theta) - tempY * Mathf.Sin(theta);
        float rotatedY = tempX * Mathf.Sin(theta) + tempY * Mathf.Cos(theta);

        // translate back
        x = rotatedX + cx;
        y = rotatedY + cy;
    }*/

    public void SetRedirect(PageRangeType rangeType, string pageId)
    {
        redirectType = rangeType;
        redirectId = pageId;
    }

    public override void OnAlternateSelectStarted()
    {
        RuleBookController.Instance.SetShown(true, redirectType != PageRangeType.Boons);
        RuleBookPageInfo info = RuleBookController.Instance.PageData.Find(x =>
            !string.IsNullOrEmpty(x.pageId)
            && RuleBookManager.GetUnformattedPageId(x.pageId) == redirectId
            && (redirectType != PageRangeType.Abilities || x.abilityPage)
        );
        int pageIndex = RuleBookController.Instance.PageData.IndexOf(info);
        InscryptionAPIPlugin.Logger.LogDebug($"[PageTextSelected] Type:{redirectType} Id:{redirectId} Idx:[{pageIndex}] Null:{info == null}");

        RuleBookRedirectManager.Instance.StopAllCoroutines();
        RuleBookRedirectManager.Instance.StartCoroutine(RuleBookController.Instance.flipper.FlipToPage(pageIndex, 0.2f));
    }
}
