using UnityEngine;

public class FightManager : MonoBehaviour
{
    [Header("Fighter References")]
    public BoxBattler playerFighter;
    public BoxBattler opponentFighter;

    [Header("Fight State")]
    public bool fightActive = false;
    private BoxBattler winner;

    void Start()
    {
        // Find fighters if not assigned in inspector
        if (playerFighter == null || opponentFighter == null)
        {
            FindFighters();
        }

        // Start the fight
        StartFight();
    }

    void FindFighters()
    {
        // This is a simple approach - tag your fighters or organize them in the hierarchy
        BoxBattler[] fighters = FindObjectsByType<BoxBattler>(FindObjectsInactive.Exclude);
        if (fighters.Length >= 2)
        {
            playerFighter = fighters[0];
            opponentFighter = fighters[1];
        }
    }

    void StartFight()
    {
        if (playerFighter == null || opponentFighter == null)
        {
            Debug.LogError("FightManager: Could not find both fighters!");
            return;
        }

        fightActive = true;
        Debug.Log($"=== FIGHT START ===");
        Debug.Log($"{playerFighter.gameObject.name} ({playerFighter.currentHealth}HP, {playerFighter.attackDamage}ATK, {playerFighter.speedStat}SPD)");
        Debug.Log($"vs");
        Debug.Log($"{opponentFighter.gameObject.name} ({opponentFighter.currentHealth}HP, {opponentFighter.attackDamage}ATK, {opponentFighter.speedStat}SPD)");
    }

    void Update()
    {
        if (!fightActive) return;

        // Check if either fighter is defeated
        bool playerAlive = playerFighter.isAlive;
        bool opponentAlive = opponentFighter.isAlive;

        if (!playerAlive && !opponentAlive)
        {
            // Both somehow died at the same time (tie)
            EndFight(null);
        }
        else if (!playerAlive)
        {
            // Opponent wins
            EndFight(opponentFighter);
        }
        else if (!opponentAlive)
        {
            // Player wins
            EndFight(playerFighter);
        }
    }

    void EndFight(BoxBattler winner)
    {
        fightActive = false;
        this.winner = winner;

        if (winner == null)
        {
            Debug.Log("=== FIGHT ENDED ===");
            Debug.Log("TIE! Both fighters fell!");
        }
        else
        {
            Debug.Log("=== FIGHT ENDED ===");
            Debug.Log($"WINNER: {winner.gameObject.name}!");
            Debug.Log($"Final stats - Health: {winner.currentHealth}/{winner.maxHealth}, Remaining damage dealt: {winner.attackDamage}");
        }

        // You can add rewards, UI updates, or progression logic here
    }

    /// <summary>
    /// Check if player won
    /// </summary>
    public bool PlayerWon()
    {
        return winner == playerFighter;
    }

    /// <summary>
    /// Get the winner
    /// </summary>
    public BoxBattler GetWinner()
    {
        return winner;
    }

    /// <summary>
    /// Called when you want to restart the fight or move to next opponent
    /// </summary>
    public void ResetFight()
    {
        fightActive = false;
        winner = null;
        // You'll need to reload the scene or reset fighter positions/health
    }
}
