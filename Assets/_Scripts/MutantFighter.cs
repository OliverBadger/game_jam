using System;
using UnityEngine;

public class MutantFighter : MonoBehaviour
{
    [Header("Mutant Parts (Assign your Scriptable Objects here)")]
    public AnimalData headPart;
    public AnimalData bodyPart;
    public AnimalData legsPart;

    [Header("Final Combat Stats")]
    public int currentAttack;
    public int currentHealth;
    public int currentSpeed;
    
    [Header("Combo Info")]
    public float comboMultiplier = 1f;
    public string comboDescription = "No combo";

    // Event fired when parts are assigned/generated so visuals can update
    public event Action<AnimalData, AnimalData, AnimalData> OnPartsChanged;

    /// <summary>
    /// Assign parts at runtime and regenerate stats. This is intended to be used by selection managers
    /// or scene transfer code to set parts on a spawned fighter.
    /// </summary>
    public void SetParts(AnimalData head, AnimalData body, AnimalData legs)
    {
        headPart = head;
        bodyPart = body;
        legsPart = legs;
        GenerateMutantStats();
    }

    void Start()
    {
        GenerateMutantStats();
    }

    public void GenerateMutantStats()
    {
        // 1. Pull base stats from each part
        // Safe-guards: if a part is null, use 0 and log a warning
        if (headPart == null) Debug.LogWarning($"{gameObject.name}: headPart is null");
        if (bodyPart == null) Debug.LogWarning($"{gameObject.name}: bodyPart is null");
        if (legsPart == null) Debug.LogWarning($"{gameObject.name}: legsPart is null");

        currentAttack = headPart != null ? headPart.headAttack : 0;
        currentHealth = bodyPart != null ? bodyPart.bodyHealth : 0;
        currentSpeed = legsPart != null ? legsPart.legsSpeed : 0;

        // 2. Calculate and apply combo multipliers
        CalculateCombo();

        Debug.Log($"Generated Mutant with {currentAttack} Atk, {currentHealth} HP, and {currentSpeed} Spd! {comboDescription}");

        // Notify visuals or other listeners that parts (and stats) have been generated/changed
        OnPartsChanged?.Invoke(headPart, bodyPart, legsPart);
    }

    private void CalculateCombo()
    {
        comboMultiplier = 1f;
        comboDescription = "No combo";

        // Check for triple match (all three parts are the same animal)
        if (headPart == bodyPart && bodyPart == legsPart)
        {
            comboMultiplier = 1.5f;
            comboDescription = $"TRIPLE COMBO! All {headPart.animalName}! +50% stats";
            ApplyComboMultiplier();
            return;
        }

        // Check for double matches
        // Head + Body match
        if (headPart == bodyPart)
        {
            comboMultiplier = 1.2f;
            comboDescription = $"Head + Body bonus! {headPart.animalName} combo! +20%";
            ApplyComboMultiplier();
            return;
        }

        // Body + Legs match
        if (bodyPart == legsPart)
        {
            comboMultiplier = 1.2f;
            comboDescription = $"Body + Legs bonus! {bodyPart.animalName} combo! +20%";
            ApplyComboMultiplier();
            return;
        }

        // Head + Legs match
        if (headPart == legsPart)
        {
            comboMultiplier = 1.2f;
            comboDescription = $"Head + Legs bonus! {headPart.animalName} combo! +20%";
            ApplyComboMultiplier();
            return;
        }
    }

    private void ApplyComboMultiplier()
    {
        currentAttack = Mathf.RoundToInt(currentAttack * comboMultiplier);
        currentHealth = Mathf.RoundToInt(currentHealth * comboMultiplier);
        currentSpeed = Mathf.RoundToInt(currentSpeed * comboMultiplier);
    }
}
