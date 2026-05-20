using UnityEngine;

[RequireComponent(typeof(MutantFighter))]
public class MutantVisuals : MonoBehaviour
{
    // All transforms and renderers are private — they are auto-created
    // at runtime so the Inspector stays uncluttered.
    private Transform     visualRoot;
    private Transform     headAnchor;
    private Transform     bodyAnchor;
    private Transform     legsAnchor;
    private SpriteRenderer headRenderer;
    private SpriteRenderer bodyRenderer;
    private SpriteRenderer legsRenderer;

    private MutantFighter fighter;

    private void Awake()
    {
        fighter = GetComponent<MutantFighter>();
        EnsureVisualHierarchy();
    }

    private void OnEnable()
    {
        if (fighter != null) fighter.OnPartsChanged += ApplyParts;
    }

    private void OnDisable()
    {
        if (fighter != null) fighter.OnPartsChanged -= ApplyParts;
    }

    // ── Hierarchy Builder ────────────────────────────────────────────────────

    private void EnsureVisualHierarchy()
    {
        // Find or create a "VisualRoot" child — WobblyBox lives here so the
        // wobble effect is purely visual and does NOT affect the physics body.
        Transform found = transform.Find("VisualRoot");
        visualRoot = found != null ? found : CreateChild(transform, "VisualRoot");

        // Auto-add WobblyBox to VisualRoot if it isn't there already.
        if (visualRoot.GetComponent<WobblyBox>() == null)
            visualRoot.gameObject.AddComponent<WobblyBox>();

        // Each body part has its own anchor under VisualRoot.
        // AnimalData stores per-part offsets so pixel-perfect 16-bit placement
        // can be configured directly on the ScriptableObject.
        headAnchor = FindOrCreateAnchor("HeadAnchor");
        bodyAnchor = FindOrCreateAnchor("BodyAnchor");
        legsAnchor = FindOrCreateAnchor("LegsAnchor");

        // Sorting order: legs behind body, body behind head.
        headRenderer = GetOrAddRenderer(headAnchor, 2);
        bodyRenderer = GetOrAddRenderer(bodyAnchor, 1);
        legsRenderer = GetOrAddRenderer(legsAnchor, 0);
    }

    private Transform FindOrCreateAnchor(string anchorName)
    {
        Transform t = visualRoot.Find(anchorName);
        return t != null ? t : CreateChild(visualRoot, anchorName);
    }

    private static Transform CreateChild(Transform parent, string childName)
    {
        GameObject go = new GameObject(childName);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        return go.transform;
    }

    private static SpriteRenderer GetOrAddRenderer(Transform parent, int sortOrder)
    {
        SpriteRenderer sr = parent.GetComponent<SpriteRenderer>();
        if (sr == null) sr = parent.gameObject.AddComponent<SpriteRenderer>();
        sr.sortingOrder = sortOrder;
        return sr;
    }

    // ── Sprite Application ────────────────────────────────────────────────────

    /// <summary>
    /// Called by MutantFighter.OnPartsChanged whenever parts are set.
    /// Applies sprites and the pixel-art offsets defined in each AnimalData.
    /// </summary>
    public void ApplyParts(AnimalData head, AnimalData body, AnimalData legs)
    {
        headRenderer.sprite = (head != null) ? head.headSprite : null;
        bodyRenderer.sprite = (body != null) ? body.bodySprite : null;
        legsRenderer.sprite = (legs != null) ? legs.legsSprite : null;

        if (head  != null) headAnchor.localPosition  = head.headOffset;
        if (body  != null) bodyAnchor.localPosition  = body.bodyOffset;
        if (legs  != null) legsAnchor.localPosition  = legs.legsOffset;
    }
}
