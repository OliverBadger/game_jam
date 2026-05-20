using UnityEngine;

/// <summary>
/// Generates a random opponent by doing a weighted roll for each part slot
/// (head, body, legs) using each animal's drop rate, modified by any shop upgrades
/// stored in the GameManager.
///
/// HOW TO USE IN THE EDITOR:
///   1. Add this component to the FightManager GameObject (or a child).
///   2. In the Inspector, populate the "All Animals" array with all six
///      AnimalData ScriptableObjects (Turtle, Ostrich, Wolf, Crocodile, Bear, Lion).
///   3. FightManager calls GenerateOpponent(mutantFighter) during scene startup.
/// </summary>
public class OpponentGenerator : MonoBehaviour
{
    [SerializeField] private AnimalData[] allAnimals;

    /// <summary>
    /// Rolls three random parts and assigns them to the given MutantFighter.
    /// Each roll is independent, so you can get e.g. Lion head / Turtle body / Wolf legs.
    /// </summary>
    public void GenerateOpponent(MutantFighter opponent)
    {
        if (allAnimals == null || allAnimals.Length == 0)
        {
            Debug.LogError("[OpponentGenerator] allAnimals array is empty! Assign AnimalData assets in the Inspector.");
            return;
        }

        AnimalData head = RollPart();
        AnimalData body = RollPart();
        AnimalData legs = RollPart();

        opponent.SetParts(head, body, legs);
        Debug.Log($"[OpponentGenerator] Generated opponent: {head?.animalName} head / {body?.animalName} body / {legs?.animalName} legs");
    }

    private AnimalData RollPart()
    {
        float totalWeight = 0f;
        foreach (AnimalData animal in allAnimals)
        {
            if (animal == null) continue;   // skip null slots in the Inspector array
            totalWeight += GetRate(animal);
        }

        if (totalWeight <= 0f)
        {
            Debug.LogWarning("[OpponentGenerator] All effective drop rates are 0. Returning first non-null animal as fallback.");
            foreach (AnimalData animal in allAnimals)
                if (animal != null) return animal;
            return null;
        }

        float roll       = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (AnimalData animal in allAnimals)
        {
            if (animal == null) continue;
            cumulative += GetRate(animal);
            if (roll <= cumulative)
                return animal;
        }

        // Floating-point safety fallback — return the last non-null entry.
        for (int i = allAnimals.Length - 1; i >= 0; i--)
            if (allAnimals[i] != null) return allAnimals[i];
        return null;
    }

    private float GetRate(AnimalData animal)
    {
        return GameManager.Instance != null
            ? GameManager.Instance.GetEffectiveDropRate(animal)
            : animal.baseDropRate;
    }
}
