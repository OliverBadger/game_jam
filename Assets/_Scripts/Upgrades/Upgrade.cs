using UnityEngine;

/// <summary>
/// Data-only description of a single tiered upgrade.
///
/// The player buys tiers in order (1, then 2, then 3, ...). Each tier carries
/// its own cost and effect magnitude, so progression curves are tuned entirely
/// from the Inspector — no code edits needed to rebalance.
///
/// Effect kinds — semantics of Tier.effectValue:
///   • DropRateAdjust      — percentage-point delta on targetAnimalName (stacks).
///   • ActionBudgetBoost   — flat actions added to every fight (stacks).
///   • SpinBudgetBoost     — flat spins added to every fight (stacks).
///   • GoldRewardBoost     — multiplier delta on fight gold (0.25 = +25%, stacks).
///   • GoldGift            — one-time coins paid out the moment that tier is bought.
///
/// Create with: right-click in Project → Create → MutantMashup/Upgrade.
/// </summary>
[CreateAssetMenu(fileName = "NewUpgrade", menuName = "MutantMashup/Upgrade")]
public class Upgrade : ScriptableObject
{
    public enum Kind
    {
        DropRateAdjust,
        ActionBudgetBoost,
        SpinBudgetBoost,
        GoldRewardBoost,
        GoldGift
    }

    public enum Category { Animals, Slots, Economy }

    [Header("Display")]
    public string  displayName;
    [TextArea(2, 4)] public string description;
    public Sprite  icon;
    public Category category = Category.Animals;

    [Header("Effect")]
    public Kind kind = Kind.DropRateAdjust;

    [Tooltip("Used only by DropRateAdjust. Must match an AnimalData.animalName exactly.")]
    public string targetAnimalName;

    [Tooltip("Each entry = one purchasable tier, applied in order. Min 1 entry.")]
    public Tier[] tiers;

    [System.Serializable]
    public class Tier
    {
        public int   cost = 50;
        [Tooltip("Magnitude of the effect this tier grants. Meaning depends on Kind — see Upgrade.cs comment.")]
        public float effectValue = 1f;
        [TextArea(1, 3)]
        public string flavorText;
    }

    public int    MaxTier  => tiers != null ? tiers.Length : 0;
    public string Headline => string.IsNullOrEmpty(displayName) ? name : displayName;

    /// <summary>Human-readable summary of what one tier grants, for the card UI.</summary>
    public string DescribeTier(int tierIndex)
    {
        if (tiers == null || tierIndex < 0 || tierIndex >= tiers.Length) return "";
        float v = tiers[tierIndex].effectValue;
        return kind switch
        {
            Kind.DropRateAdjust    => $"{targetAnimalName} drop {(v >= 0 ? "+" : "")}{v}%",
            Kind.ActionBudgetBoost => $"+{(int)v} action{((int)v == 1 ? "" : "s")} / fight",
            Kind.SpinBudgetBoost   => $"+{(int)v} spin{((int)v == 1 ? "" : "s")} / fight",
            Kind.GoldRewardBoost   => $"+{Mathf.RoundToInt(v * 100f)}% fight gold",
            Kind.GoldGift          => $"+{(int)v} gold (one-time)",
            _                      => ""
        };
    }
}
