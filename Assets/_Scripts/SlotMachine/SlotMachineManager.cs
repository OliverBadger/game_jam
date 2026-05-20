using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Core slot machine controller. Owns the three reels, the per-fight spin/action
/// budget (read from GameManager), and the lock-in handshake that hands the final
/// head/body/legs over to GameManager before the Fight scene loads.
///
/// Wire-up in the Editor:
///   1. Drop this on a "SlotMachineManager" GameObject in the SlotMachine scene.
///   2. Populate "All Animals" with the six AnimalData assets.
///   3. Assign the three SlotReel components (headReel, bodyReel, legsReel).
///   4. Set "Fight Scene Name" to your fight scene (e.g. "Fight").
/// </summary>
public class SlotMachineManager : MonoBehaviour
{
    public enum ReelKind { Head = 0, Body = 1, Legs = 2 }

    [Header("Animal Pool")]
    [Tooltip("Every animal that can appear on a reel. Drop rates respect GameManager shop modifiers.")]
    [SerializeField] private AnimalData[] allAnimals;

    [Header("Reels")]
    [SerializeField] private SlotReel headReel;
    [SerializeField] private SlotReel bodyReel;
    [SerializeField] private SlotReel legsReel;

    [Header("Scene Routing")]
    [SerializeField] private string fightSceneName = "Fight";

    [Header("Timing")]
    [Tooltip("How long the visual reel spin lasts before the result is shown.")]
    [SerializeField] private float spinDuration = 0.9f;

    // Runtime budget (initialised from GameManager.SpinsThisFight / ActionsThisFight).
    private int spinsRemaining;
    private int actionsRemaining;

    // Current results, one per reel (null until the first spin).
    private readonly AnimalData[] results = new AnimalData[3];
    private readonly bool[]       holds   = new bool[3];
    private bool spinning;

    // ── Events for UI binding ────────────────────────────────────────────────
    public event Action OnStateChanged;      // fires after any state-changing op
    public event Action OnSpinStarted;
    public event Action OnSpinFinished;

    // ── Public read-only state ───────────────────────────────────────────────
    public int  SpinsRemaining   => spinsRemaining;
    public int  ActionsRemaining => actionsRemaining;
    public bool IsSpinning       => spinning;
    public AnimalData GetResult(int reelIdx) => InRange(reelIdx) ? results[reelIdx] : null;
    public bool       IsHeld   (int reelIdx) => InRange(reelIdx) && holds[reelIdx];

    public AnimalData ResultHead => results[(int)ReelKind.Head];
    public AnimalData ResultBody => results[(int)ReelKind.Body];
    public AnimalData ResultLegs => results[(int)ReelKind.Legs];

    /// <summary>True once every reel has a result AND no spin is in flight.</summary>
    public bool CanLockIn => !spinning && results[0] != null && results[1] != null && results[2] != null;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void Start()
    {
        // Pull this fight's budget from GameManager. If the game was started
        // directly from the slot machine scene (no boot), fall back to sane defaults
        // so designers can iterate without the full flow.
        if (GameManager.Instance != null)
        {
            // Effective getters fold in any owned upgrade tiers.
            spinsRemaining   = GameManager.Instance.GetEffectiveSpinsThisFight();
            actionsRemaining = GameManager.Instance.GetEffectiveActionsThisFight();
        }
        else
        {
            Debug.LogWarning("[SlotMachineManager] No GameManager found — using debug defaults (3 spins, 5 actions).");
            spinsRemaining   = 3;
            actionsRemaining = 5;
        }

        ValidateReels();
        OnStateChanged?.Invoke();
    }

    private void ValidateReels()
    {
        if (headReel == null || bodyReel == null || legsReel == null)
            Debug.LogError("[SlotMachineManager] One or more reels are not assigned.");

        if (allAnimals == null || allAnimals.Length == 0)
            Debug.LogError("[SlotMachineManager] allAnimals array is empty.");
    }

    // ── Spin ─────────────────────────────────────────────────────────────────

    /// <summary>Spin every non-held reel. Costs one spin from the budget.</summary>
    public void Spin()
    {
        if (spinning) return;
        if (spinsRemaining <= 0)
        {
            Debug.Log("[SlotMachineManager] No spins remaining.");
            return;
        }

        spinsRemaining--;
        spinning = true;
        OnSpinStarted?.Invoke();
        OnStateChanged?.Invoke();

        // Roll a target animal for each non-held reel, then ask each reel to
        // animate from its current state to that target.
        AnimalData newHead = holds[0] ? results[0] : RollAnimal();
        AnimalData newBody = holds[1] ? results[1] : RollAnimal();
        AnimalData newLegs = holds[2] ? results[2] : RollAnimal();

        StartSingleReel(headReel, ReelKind.Head, newHead, holds[0]);
        StartSingleReel(bodyReel, ReelKind.Body, newBody, holds[1]);
        StartSingleReel(legsReel, ReelKind.Legs, newLegs, holds[2]);

        // After spinDuration, commit results and fire finish event.
        Invoke(nameof(FinishSpin), spinDuration);
    }

    private void StartSingleReel(SlotReel reel, ReelKind kind, AnimalData target, bool isHeld)
    {
        if (reel == null) return;
        if (isHeld)
        {
            // Held reels just sit still showing their current animal.
            reel.ShowResult(target, kind);
            return;
        }
        reel.PlaySpin(allAnimals, target, kind, spinDuration);
    }

    private void FinishSpin()
    {
        // Commit results — each reel was animating toward these targets.
        if (headReel != null) results[(int)ReelKind.Head] = headReel.CurrentAnimal;
        if (bodyReel != null) results[(int)ReelKind.Body] = bodyReel.CurrentAnimal;
        if (legsReel != null) results[(int)ReelKind.Legs] = legsReel.CurrentAnimal;

        spinning = false;
        OnSpinFinished?.Invoke();
        OnStateChanged?.Invoke();
    }

    // ── Hold ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Toggle the hold flag for a reel. Costs 1 action. A held reel will not
    /// re-roll on the next Spin.
    /// </summary>
    public void ToggleHold(int reelIdx)
    {
        if (!InRange(reelIdx) || spinning) return;
        if (results[reelIdx] == null)
        {
            // Nothing to hold until the player has spun at least once.
            Debug.Log("[SlotMachineManager] Spin before holding.");
            return;
        }
        if (actionsRemaining <= 0)
        {
            Debug.Log("[SlotMachineManager] No actions remaining.");
            return;
        }

        holds[reelIdx] = !holds[reelIdx];
        actionsRemaining--;
        SyncReelHoldVisuals();
        OnStateChanged?.Invoke();
    }

    // ── Nudge ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Cycle a single reel by +1 or -1 step through the animal pool. Costs 1 action.
    /// Useful when the player is one slot away from a combo.
    /// </summary>
    public void Nudge(int reelIdx, int direction)
    {
        if (!InRange(reelIdx) || spinning) return;
        if (results[reelIdx] == null)
        {
            Debug.Log("[SlotMachineManager] Spin before nudging.");
            return;
        }
        if (actionsRemaining <= 0)
        {
            Debug.Log("[SlotMachineManager] No actions remaining.");
            return;
        }
        if (allAnimals == null || allAnimals.Length == 0) return;

        int current = IndexOf(results[reelIdx]);
        int step    = direction >= 0 ? 1 : -1;
        // (current + step + N) % N is the safe wrap-around for negative steps.
        int next    = ((current + step) % allAnimals.Length + allAnimals.Length) % allAnimals.Length;

        results[reelIdx] = allAnimals[next];
        actionsRemaining--;

        SlotReel reel = GetReel((ReelKind)reelIdx);
        reel?.ShowResult(results[reelIdx], (ReelKind)reelIdx);
        OnStateChanged?.Invoke();
    }

    private int IndexOf(AnimalData animal)
    {
        if (allAnimals == null) return 0;
        for (int i = 0; i < allAnimals.Length; i++)
            if (allAnimals[i] == animal) return i;
        return 0;
    }

    // ── Lock In ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Commit the current head/body/legs to GameManager and load the fight scene.
    /// </summary>
    public void LockInAndFight()
    {
        if (!CanLockIn)
        {
            Debug.Log("[SlotMachineManager] Cannot lock in yet — spin every reel first.");
            return;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.SetPlayerParts(results[0], results[1], results[2]);

        if (string.IsNullOrEmpty(fightSceneName))
        {
            Debug.LogError("[SlotMachineManager] fightSceneName is empty.");
            return;
        }

        SceneManager.LoadScene(fightSceneName);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private bool InRange(int idx) => idx >= 0 && idx < 3;

    private SlotReel GetReel(ReelKind kind) => kind switch
    {
        ReelKind.Head => headReel,
        ReelKind.Body => bodyReel,
        ReelKind.Legs => legsReel,
        _             => null
    };

    private void SyncReelHoldVisuals()
    {
        headReel?.SetHeldVisual(holds[0]);
        bodyReel?.SetHeldVisual(holds[1]);
        legsReel?.SetHeldVisual(holds[2]);
    }

    /// <summary>Weighted roll across allAnimals using shop-modified drop rates.</summary>
    private AnimalData RollAnimal()
    {
        float total = 0f;
        foreach (AnimalData a in allAnimals)
        {
            if (a == null) continue;
            total += GetRate(a);
        }
        if (total <= 0f)
        {
            // Every drop rate has been zeroed out (e.g. by aggressive shop nerfs).
            // Return any non-null entry so the reel doesn't show a blank.
            foreach (AnimalData a in allAnimals) if (a != null) return a;
            return null;
        }

        float roll = UnityEngine.Random.Range(0f, total);
        float cum  = 0f;
        foreach (AnimalData a in allAnimals)
        {
            if (a == null) continue;
            cum += GetRate(a);
            if (roll <= cum) return a;
        }
        // Float-rounding fallback.
        for (int i = allAnimals.Length - 1; i >= 0; i--)
            if (allAnimals[i] != null) return allAnimals[i];
        return null;
    }

    private float GetRate(AnimalData a)
    {
        return GameManager.Instance != null
            ? GameManager.Instance.GetEffectiveDropRate(a)
            : a.baseDropRate;
    }
}
