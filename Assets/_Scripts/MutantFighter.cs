using System;
using UnityEngine;

public class MutantFighter : MonoBehaviour
{
    // Parts are private — set by FightManager (player) or OpponentGenerator (AI).
    // They are NOT shown in the Inspector to keep the UI clean.
    private AnimalData headPart;
    private AnimalData bodyPart;
    private AnimalData legsPart;

    // Final stats are read-only from the outside.
    private int _currentAttack;
    private int _currentHealth;
    private int _currentSpeed;
    private float _comboMultiplier = 1f;
    private string _comboDescription = "No combo";

    public int   CurrentAttack      => _currentAttack;
    public int   CurrentHealth      => _currentHealth;
    public int   CurrentSpeed       => _currentSpeed;
    public float ComboMultiplier    => _comboMultiplier;
    public string ComboDescription  => _comboDescription;

    // Fired whenever parts change so MutantVisuals (and BoxBattler) can react.
    public event Action<AnimalData, AnimalData, AnimalData> OnPartsChanged;

    /// <summary>
    /// Assign all three parts at once. Called by FightManager for the player
    /// (using GameManager data) or by OpponentGenerator for AI fighters.
    /// </summary>
    public void SetParts(AnimalData head, AnimalData body, AnimalData legs)
    {
        headPart = head;
        bodyPart = body;
        legsPart = legs;
        GenerateMutantStats();
    }

    // Start is intentionally omitted — parts MUST be assigned by calling SetParts().
    // FightManager.Start() (execution order -10) always does this before BoxBattler (order 0)
    // reads the stats, so there is nothing to validate here at the MonoBehaviour level.

    public void GenerateMutantStats()
    {
        if (headPart == null) Debug.LogWarning($"[MutantFighter] {gameObject.name}: headPart is null");
        if (bodyPart == null) Debug.LogWarning($"[MutantFighter] {gameObject.name}: bodyPart is null");
        if (legsPart == null) Debug.LogWarning($"[MutantFighter] {gameObject.name}: legsPart is null");

        // Base stats: head drives attack, body drives health, legs drive speed.
        _currentAttack = headPart != null ? headPart.headAttack : 0;
        _currentHealth = bodyPart != null ? bodyPart.bodyHealth : 0;
        _currentSpeed  = legsPart != null ? legsPart.legsSpeed  : 0;

        CalculateAndApplyCombo();

        Debug.Log($"[MutantFighter] {gameObject.name}: {_currentAttack}ATK / {_currentHealth}HP / {_currentSpeed}SPD — {_comboDescription}");

        OnPartsChanged?.Invoke(headPart, bodyPart, legsPart);
    }

    // ── Combo Logic ──────────────────────────────────────────────────────────
    // Combos only multiply the STATS BELONGING TO THE MATCHING ANIMAL.
    // Example: Bear head + Bear body → x2 to attack (from bear head) AND health
    //          (from bear body). The lion legs speed is untouched.
    // Triple match → x3 ALL stats (jackpot!).
    private void CalculateAndApplyCombo()
    {
        _comboMultiplier   = 1f;
        _comboDescription  = "No Combo";

        // Triple match — all three slots the same animal
        if (headPart != null && headPart == bodyPart && bodyPart == legsPart)
        {
            _currentAttack = Mathf.RoundToInt(_currentAttack * 3f);
            _currentHealth = Mathf.RoundToInt(_currentHealth * 3f);
            _currentSpeed  = Mathf.RoundToInt(_currentSpeed  * 3f);
            _comboMultiplier  = 3f;
            _comboDescription = $"TRIPLE {headPart.animalName?.ToUpper() ?? "???"}! x3 ALL STATS — JACKPOT!";
            return;
        }

        // Adjacent double: Head + Body (multiplies attack and health)
        if (headPart != null && headPart == bodyPart)
        {
            _currentAttack = Mathf.RoundToInt(_currentAttack * 2f);
            _currentHealth = Mathf.RoundToInt(_currentHealth * 2f);
            _comboMultiplier  = 2f;
            _comboDescription = $"{headPart.animalName} Head+Body Combo! x2 ATK+HP";
            return;
        }

        // Adjacent double: Body + Legs (multiplies health and speed)
        if (bodyPart != null && bodyPart == legsPart)
        {
            _currentHealth = Mathf.RoundToInt(_currentHealth * 2f);
            _currentSpeed  = Mathf.RoundToInt(_currentSpeed  * 2f);
            _comboMultiplier  = 2f;
            _comboDescription = $"{bodyPart.animalName} Body+Legs Combo! x2 HP+SPD";
            return;
        }

        // Non-adjacent double: Head + Legs (multiplies attack and speed)
        if (headPart != null && headPart == legsPart)
        {
            _currentAttack = Mathf.RoundToInt(_currentAttack * 2f);
            _currentSpeed  = Mathf.RoundToInt(_currentSpeed  * 2f);
            _comboMultiplier  = 2f;
            _comboDescription = $"{headPart.animalName} Head+Legs Combo! x2 ATK+SPD";
            return;
        }
    }
}
