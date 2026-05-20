using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// The Upgrades & Purchases screen. Reads the global UpgradeCatalog through
/// the UpgradeRegistry on GameManager, spawns one UpgradeCardUI per entry,
/// and refreshes every card after each purchase.
///
/// Wire-up in the Editor:
///   1. Add this component to an "UpgradeShopUI" GameObject in the UpgradeShop scene.
///   2. Drag your card prefab (a GameObject with UpgradeCardUI on it) into
///      "Card Prefab".
///   3. Drag the content container (Scroll View → Viewport → Content, or any
///      parent with a Vertical/Grid Layout Group) into "Card List Parent".
///   4. Optionally drag in Gold Label / Feedback Label / Back Button.
///   5. Wire the Back button's OnClick to BackToHub() (or rely on auto-wire).
/// </summary>
public class UpgradeShopUI : MonoBehaviour
{
    [Header("Spawning")]
    [SerializeField] private UpgradeCardUI cardPrefab;
    [SerializeField] private Transform     cardListParent;

    [Header("Labels & Buttons")]
    [SerializeField] private TMP_Text goldLabel;
    [SerializeField] private TMP_Text feedbackLabel;
    [SerializeField] private Button   backButton;

    [Header("Navigation")]
    [SerializeField] private string hubSceneName = "TournamentHub";

    [Header("Sorting")]
    [Tooltip("If true, cards are grouped by Upgrade.category in the order Animals → Slots → Economy.")]
    [SerializeField] private bool sortByCategory = true;

    private UpgradeRegistry registry;
    private readonly List<UpgradeCardUI> cards = new();

    private void Start()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(BackToHub);
            backButton.onClick.AddListener(BackToHub);
        }

        registry = GameManager.Instance != null
            ? GameManager.Instance.GetComponent<UpgradeRegistry>()
            : null;

        if (registry == null)
        {
            Feedback("Upgrade registry not found. Add UpgradeRegistry to the GameManager.");
            return;
        }

        registry.OnRegistryChanged += RefreshAll;
        BuildCards();
        RefreshAll();
    }

    private void OnDestroy()
    {
        if (registry != null) registry.OnRegistryChanged -= RefreshAll;
    }

    private void BuildCards()
    {
        if (cardPrefab == null || cardListParent == null || registry == null) return;

        UpgradeCatalog catalog = registry.Catalog;
        if (catalog == null || catalog.all == null) return;

        // Build a sorted copy so the asset's order is preserved on disk, but
        // we still get clean grouping in the UI.
        Upgrade[] list = (Upgrade[])catalog.all.Clone();
        if (sortByCategory) System.Array.Sort(list, CompareForDisplay);

        foreach (Upgrade u in list)
        {
            if (u == null) continue;
            UpgradeCardUI card = Instantiate(cardPrefab, cardListParent);
            card.Bind(u, registry, this);
            cards.Add(card);
        }
    }

    private static int CompareForDisplay(Upgrade a, Upgrade b)
    {
        if (a == null) return 1;
        if (b == null) return -1;
        int byCat = ((int)a.category).CompareTo((int)b.category);
        return byCat != 0 ? byCat : string.Compare(a.Headline, b.Headline, System.StringComparison.Ordinal);
    }

    // ── Refresh / Notify ─────────────────────────────────────────────────────

    private void RefreshAll()
    {
        if (goldLabel != null && GameManager.Instance != null)
            goldLabel.text = $"Gold: {GameManager.Instance.Gold}";
        foreach (UpgradeCardUI c in cards) c?.Refresh();
    }

    /// <summary>Called by an UpgradeCardUI after a successful purchase.</summary>
    public void NotifyPurchased(Upgrade u)
    {
        Feedback($"Purchased: {u.Headline} (Tier {registry.GetTier(u)})");
        RefreshAll();
    }

    private void Feedback(string msg)
    {
        if (feedbackLabel != null) feedbackLabel.text = msg;
        Debug.Log($"[UpgradeShopUI] {msg}");
    }

    // ── Navigation ───────────────────────────────────────────────────────────

    public void BackToHub()
    {
        if (!string.IsNullOrEmpty(hubSceneName)) SceneManager.LoadScene(hubSceneName);
    }
}
