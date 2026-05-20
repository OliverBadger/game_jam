using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One card in the upgrade shop. Bound to a specific Upgrade at runtime by
/// UpgradeShopUI.Bind(); refreshes itself when told to.
///
/// Card prefab layout (suggested — every field is optional, the card no-ops
/// if a slot is unassigned):
///   ┌──────────────────────────────┐
///   │ [Icon]  Display Name         │
///   │         description …        │
///   │         effect summary       │
///   │         Tier 1/3   [25g BUY] │
///   └──────────────────────────────┘
/// </summary>
public class UpgradeCardUI : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private Image    iconImage;
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text descriptionLabel;
    [SerializeField] private TMP_Text effectLabel;
    [SerializeField] private TMP_Text tierLabel;
    [SerializeField] private TMP_Text costLabel;
    [SerializeField] private TMP_Text flavorLabel;

    [Header("Interactions")]
    [SerializeField] private Button   buyButton;

    private Upgrade         upgrade;
    private UpgradeRegistry registry;
    private UpgradeShopUI   shop;

    public void Bind(Upgrade u, UpgradeRegistry r, UpgradeShopUI owner)
    {
        upgrade  = u;
        registry = r;
        shop     = owner;

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(HandleBuyPressed);
        }
        Refresh();
    }

    public void Refresh()
    {
        if (upgrade == null) return;

        int   owned   = registry != null ? registry.GetTier(upgrade) : 0;
        bool  maxed   = registry != null && registry.IsMaxed(upgrade);
        int   nextCost = registry != null ? registry.NextTierCost(upgrade) : -1;
        bool  canAfford = GameManager.Instance != null && nextCost >= 0
                          && GameManager.Instance.Gold >= nextCost;

        if (iconImage != null) iconImage.sprite = upgrade.icon;
        if (iconImage != null) iconImage.enabled = upgrade.icon != null;

        if (nameLabel        != null) nameLabel.text        = upgrade.Headline;
        if (descriptionLabel != null) descriptionLabel.text = upgrade.description;
        if (tierLabel        != null) tierLabel.text        = $"Tier {owned} / {upgrade.MaxTier}";

        if (effectLabel != null)
        {
            // Show what the NEXT tier would grant; if maxed, show what's owned.
            int previewTier = maxed ? owned - 1 : owned;
            effectLabel.text = upgrade.DescribeTier(previewTier);
        }

        if (flavorLabel != null)
        {
            int previewTier = maxed ? owned - 1 : owned;
            flavorLabel.text = (upgrade.tiers != null && previewTier >= 0 && previewTier < upgrade.tiers.Length)
                ? upgrade.tiers[previewTier].flavorText
                : "";
        }

        if (costLabel != null)
            costLabel.text = maxed ? "MAXED" : $"{nextCost}g";

        if (buyButton != null)
            buyButton.interactable = !maxed && canAfford;
    }

    private void HandleBuyPressed()
    {
        if (registry == null || upgrade == null) return;
        if (registry.PurchaseNext(upgrade))
            shop?.NotifyPurchased(upgrade);
    }
}
