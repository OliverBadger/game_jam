using UnityEngine;

[RequireComponent(typeof(MutantFighter))]
public class MutantVisuals : MonoBehaviour
{
    [Header("Anchors (child Transforms) - leave empty to auto-create")]
    public Transform headAnchor;
    public Transform bodyAnchor;
    public Transform legsAnchor;

    [Header("Sprite Renderers (optional - created if missing)")]
    public SpriteRenderer headRenderer;
    public SpriteRenderer bodyRenderer;
    public SpriteRenderer legsRenderer;

    private MutantFighter fighter;

    void Awake()
    {
        fighter = GetComponent<MutantFighter>();

        EnsureAnchorsAndRenderers();
    }

    void OnEnable()
    {
        if (fighter != null) fighter.OnPartsChanged += ApplyParts;
    }

    void OnDisable()
    {
        if (fighter != null) fighter.OnPartsChanged -= ApplyParts;
    }

    void EnsureAnchorsAndRenderers()
    {
        if (headAnchor == null) headAnchor = CreateChildAnchor("HeadAnchor");
        if (bodyAnchor == null) bodyAnchor = CreateChildAnchor("BodyAnchor");
        if (legsAnchor == null) legsAnchor = CreateChildAnchor("LegsAnchor");

        if (headRenderer == null) headRenderer = GetOrAddRenderer(headAnchor, 2);
        if (bodyRenderer == null) bodyRenderer = GetOrAddRenderer(bodyAnchor, 1);
        if (legsRenderer == null) legsRenderer = GetOrAddRenderer(legsAnchor, 0);
    }

    Transform CreateChildAnchor(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        return go.transform;
    }

    SpriteRenderer GetOrAddRenderer(Transform parent, int order)
    {
        SpriteRenderer sr = parent.GetComponent<SpriteRenderer>();
        if (sr == null) sr = parent.gameObject.AddComponent<SpriteRenderer>();
        sr.sortingOrder = order;
        return sr;
    }

    /// <summary>
    /// Apply sprites and offsets from the given AnimalData parts.
    /// Called by MutantFighter when parts are generated/changed.
    /// </summary>
    public void ApplyParts(AnimalData head, AnimalData body, AnimalData legs)
    {
        // Set sprites with safe fallbacks
        headRenderer.sprite = (head != null && head.headSprite != null) ? head.headSprite : null;
        bodyRenderer.sprite = (body != null && body.bodySprite != null) ? body.bodySprite : null;
        legsRenderer.sprite = (legs != null && legs.legsSprite != null) ? legs.legsSprite : null;

        // Apply offsets if provided
        if (head != null) headAnchor.localPosition = head.headOffset;
        if (body != null) bodyAnchor.localPosition = body.bodyOffset;
        if (legs != null) legsAnchor.localPosition = legs.legsOffset;
    }
}
