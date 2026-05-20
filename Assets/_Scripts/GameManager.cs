using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Persistent singleton that survives all scene loads.
/// Carries player parts, gold, tournament state, and shop upgrades.
/// Add this to a "GameManager" GameObject in your first scene — it will
/// never be destroyed. Every other scene simply calls GameManager.Instance
/// to read or write data.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static GameManager Instance { get; private set; }

    // ─── Player Character Parts ───────────────────────────────────────────────
    [SerializeField] private AnimalData playerHead;
    [SerializeField] private AnimalData playerBody;
    [SerializeField] private AnimalData playerLegs;

    // ─── Economy ──────────────────────────────────────────────────────────────
    [SerializeField] private int gold = 100;

    // ─── Tournament Progression ───────────────────────────────────────────────
    [SerializeField] private int currentRound = 1;
    [SerializeField] private int totalRounds  = 5;

    // ─── Fight Budget (chosen by player before each fight) ────────────────────
    // The player picks a risk/reward configuration, e.g.:
    //   Conservative: 2 actions, 1 spin, +40 gold bonus
    //   Standard:     5 actions, 3 spins, +20 gold bonus
    //   Aggressive:   7 actions, 5 spins, +0 gold bonus
    [SerializeField] private int actionsThisFight  = 5;
    [SerializeField] private int spinsThisFight    = 3;
    [SerializeField] private int goldBonusThisFight = 20;

    // ─── Shop: Drop Rate Modifiers ────────────────────────────────────────────
    // Each entry maps an animal name -> additive float modifier on drop rate.
    // e.g. "Turtle" -> -10f reduces turtle appearance chance by 10 percentage points.
    private readonly Dictionary<string, float> dropRateModifiers = new();

    // ─── Public Read-Only Properties ─────────────────────────────────────────
    public AnimalData PlayerHead  => playerHead;
    public AnimalData PlayerBody  => playerBody;
    public AnimalData PlayerLegs  => playerLegs;

    public int Gold          => gold;
    public int CurrentRound  => currentRound;
    public int TotalRounds   => totalRounds;

    public int ActionsThisFight   => actionsThisFight;
    public int SpinsThisFight     => spinsThisFight;
    public int GoldBonusThisFight => goldBonusThisFight;

    /// <summary>True once the player has locked in all three parts from the slot machine.</summary>
    public bool HasParts => playerHead != null && playerBody != null && playerLegs != null;

    // ─── Lifecycle ───────────────────────────────────────────────────────────
    private void Awake()
    {
        // Classic singleton guard: if another instance exists when a scene loads,
        // destroy this duplicate and keep the original.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ─── Part Management ─────────────────────────────────────────────────────

    /// <summary>
    /// Called by the Slot Machine scene once the player locks in their three parts.
    /// </summary>
    public void SetPlayerParts(AnimalData head, AnimalData body, AnimalData legs)
    {
        playerHead  = head;
        playerBody  = body;
        playerLegs  = legs;
        Debug.Log($"[GameManager] Parts locked: {head?.animalName} / {body?.animalName} / {legs?.animalName}");
    }

    // ─── Economy ─────────────────────────────────────────────────────────────

    public void AddGold(int amount)
    {
        gold += amount;
        Debug.Log($"[GameManager] +{amount} gold. Total: {gold}");
    }

    /// <returns>False if the player cannot afford it; the gold is NOT deducted.</returns>
    public bool SpendGold(int amount)
    {
        if (gold < amount)
        {
            Debug.Log($"[GameManager] Cannot afford {amount}. Have {gold}.");
            return false;
        }
        gold -= amount;
        Debug.Log($"[GameManager] -{amount} gold. Total: {gold}");
        return true;
    }

    // ─── Tournament ──────────────────────────────────────────────────────────

    public void AdvanceRound()
    {
        currentRound++;
        Debug.Log($"[GameManager] Round {currentRound}/{totalRounds}");
    }

    public bool IsTournamentOver() => currentRound > totalRounds;

    // ─── Fight Budget ────────────────────────────────────────────────────────

    /// <summary>
    /// Player chooses their risk/reward config before each fight.
    /// </summary>
    public void SetFightBudget(int actions, int spins, int goldBonus)
    {
        actionsThisFight   = actions;
        spinsThisFight     = spins;
        goldBonusThisFight = goldBonus;
    }

    // ─── Shop Drop Rate Modifiers ─────────────────────────────────────────────

    /// <summary>
    /// Apply a persistent modifier to an animal's drop rate.
    /// Positive values increase the chance; negative values decrease it.
    /// </summary>
    public void SetDropRateModifier(string animalName, float modifier)
    {
        dropRateModifiers[animalName] = modifier;
        Debug.Log($"[GameManager] Drop rate modifier: {animalName} {(modifier >= 0 ? "+" : "")}{modifier}%");
    }

    /// <summary>
    /// Get the effective (shop-modified) drop rate for a given AnimalData asset.
    /// Clamped to a minimum of 0 so an animal can be effectively removed from the pool.
    /// </summary>
    public float GetEffectiveDropRate(AnimalData animal)
    {
        float modifier = dropRateModifiers.TryGetValue(animal.animalName, out float m) ? m : 0f;
        return Mathf.Max(0f, animal.baseDropRate + modifier);
    }

    // ─── Tournament Reset ──────────────────────────────────────────────────────

    /// <summary>
    /// Called by FightManager when the player loses or ties.
    /// Resets the round counter and clears the player's parts so they must
    /// spin the slot machine again. Gold is intentionally preserved —
    /// the player should keep their spending power for the shop.
    /// </summary>
    public void ResetTournament()
    {
        currentRound = 1;
        playerHead   = null;
        playerBody   = null;
        playerLegs   = null;
        // Reset the fight budget to the default "standard" option.
        actionsThisFight   = 5;
        spinsThisFight     = 3;
        goldBonusThisFight = 20;
        Debug.Log("[GameManager] Tournament reset. Gold preserved.");
    }
}
