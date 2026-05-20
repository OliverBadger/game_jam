using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Persistent store of every tier the player owns, plus the gateway for
/// purchases. Lives on the GameManager GameObject so DontDestroyOnLoad
/// carries it through scene changes.
///
/// Wire-up in the Editor:
///   1. Add this component to the GameManager GameObject in SampleScene
///      (the same one that hosts GameManager and TestBootstrap).
///   2. Drag your UpgradeCatalog asset into the "Catalog" slot.
///   3. GameManager.cs auto-finds this via GetComponent — no other wiring needed.
/// </summary>
public class UpgradeRegistry : MonoBehaviour
{
    [SerializeField] private UpgradeCatalog catalog;

    // Key = Upgrade.name (the ScriptableObject filename). Value = tiers purchased.
    // We deliberately do NOT serialize this — it's per-tournament runtime state.
    private readonly Dictionary<string, int> tiersOwned = new();

    /// <summary>Fired any time a purchase succeeds so the UI can refresh.</summary>
    public event Action OnRegistryChanged;

    public UpgradeCatalog Catalog => catalog;

    // ── Query ────────────────────────────────────────────────────────────────

    public int GetTier(Upgrade u)
    {
        if (u == null) return 0;
        return tiersOwned.TryGetValue(u.name, out int t) ? t : 0;
    }

    public bool IsMaxed(Upgrade u) => u != null && GetTier(u) >= u.MaxTier;
    public bool CanBuyNext(Upgrade u) => u != null && !IsMaxed(u);

    /// <summary>Cost of the NEXT tier the player would buy. -1 if maxed.</summary>
    public int NextTierCost(Upgrade u)
    {
        if (u == null || IsMaxed(u)) return -1;
        int next = GetTier(u);    // tier index for "next purchase"
        if (next < 0 || next >= u.tiers.Length) return -1;
        return u.tiers[next].cost;
    }

    public Upgrade.Tier NextTierData(Upgrade u)
    {
        if (u == null || IsMaxed(u)) return null;
        return u.tiers[GetTier(u)];
    }

    // ── Purchase ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Spend gold and grant the next tier of this upgrade. Idempotent on failure
    /// (no gold deducted, no tier change).
    /// </summary>
    public bool PurchaseNext(Upgrade u)
    {
        if (!CanBuyNext(u))
        {
            Debug.Log($"[UpgradeRegistry] {u?.name} is already maxed.");
            return false;
        }
        if (GameManager.Instance == null)
        {
            Debug.LogError("[UpgradeRegistry] No GameManager — cannot spend gold.");
            return false;
        }

        int cost = NextTierCost(u);
        if (!GameManager.Instance.SpendGold(cost)) return false;

        // Increment first so the immediate-effect lookup uses the right tier index.
        int prevTier = GetTier(u);
        tiersOwned[u.name] = prevTier + 1;

        ApplyImmediateEffect(u, u.tiers[prevTier].effectValue);
        OnRegistryChanged?.Invoke();
        return true;
    }

    private void ApplyImmediateEffect(Upgrade u, float value)
    {
        // Only one-shot effects fire here. Passive effects (budget boosts, gold
        // multiplier) are queried on-demand by their consumers, so we only need
        // to ensure the tier counter is correct.
        switch (u.kind)
        {
            case Upgrade.Kind.DropRateAdjust:
                if (!string.IsNullOrEmpty(u.targetAnimalName) && GameManager.Instance != null)
                    GameManager.Instance.AdjustDropRateModifier(u.targetAnimalName, value);
                break;

            case Upgrade.Kind.GoldGift:
                GameManager.Instance?.AddGold(Mathf.RoundToInt(value));
                break;
        }
    }

    // ── Aggregate Passive Effects ────────────────────────────────────────────
    // These walk the catalog once per call. Cheap because the catalog is tiny
    // (<30 entries even at full scope) and they're called once per scene load.

    public int GetTotalActionBonus() => SumOwned(Upgrade.Kind.ActionBudgetBoost, v => (int)v);
    public int GetTotalSpinBonus()   => SumOwned(Upgrade.Kind.SpinBudgetBoost,   v => (int)v);

    /// <summary>Multiplier ≥ 1. e.g. owning two tiers of +25% returns 1.5.</summary>
    public float GetGoldRewardMultiplier()
    {
        if (catalog == null || catalog.all == null) return 1f;
        float bonus = 0f;
        foreach (Upgrade u in catalog.all)
        {
            if (u == null || u.kind != Upgrade.Kind.GoldRewardBoost) continue;
            int tier = GetTier(u);
            for (int i = 0; i < tier && i < u.tiers.Length; i++)
                bonus += u.tiers[i].effectValue;
        }
        return 1f + bonus;
    }

    private int SumOwned(Upgrade.Kind kind, Func<float, int> coerce)
    {
        if (catalog == null || catalog.all == null) return 0;
        int total = 0;
        foreach (Upgrade u in catalog.all)
        {
            if (u == null || u.kind != kind) continue;
            int tier = GetTier(u);
            for (int i = 0; i < tier && i < u.tiers.Length; i++)
                total += coerce(u.tiers[i].effectValue);
        }
        return total;
    }

    // ── Reset ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by GameManager.ResetTournament(). Wipes purchases so a new
    /// tournament starts the upgrade tree fresh.
    /// </summary>
    public void ResetForNewTournament()
    {
        tiersOwned.Clear();
        OnRegistryChanged?.Invoke();
    }
}
