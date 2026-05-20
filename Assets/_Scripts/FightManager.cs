using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Orchestrates a fight between the player fighter and a generated opponent.
///
/// Execution order -10 ensures this Start() fires before BoxBattler (order 0),
/// so parts are assigned before BoxBattler reads them via the OnPartsChanged event.
/// </summary>
[DefaultExecutionOrder(-10)]
public class FightManager : MonoBehaviour
{
    // ── Scene References ─────────────────────────────────────────────────────
    // Drag both fighter GameObjects into these fields in the Inspector.
    // If either is left empty, the script falls back to finding them by tag
    // ("Player" for the player fighter, "Opponent" for the AI fighter).
    [SerializeField] private BoxBattler playerFighter;
    [SerializeField] private BoxBattler opponentFighter;
    [SerializeField] private OpponentGenerator opponentGenerator;

    // ── Rewards ──────────────────────────────────────────────────────────────
    [SerializeField] private int   goldRewardOnWin = 50;
    [SerializeField] private string nextSceneName  = "SlotMachine";

    // ── State ────────────────────────────────────────────────────────────────
    private bool       fightActive;
    private BoxBattler winner;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void Start()
    {
        AutoAssignFighters();
        SetupPlayerFighter();
        SetupOpponentFighter();
        StartFight();
    }

    private void Update()
    {
        if (!fightActive) return;

        bool playerAlive   = playerFighter   != null && playerFighter.IsAlive;
        bool opponentAlive = opponentFighter != null && opponentFighter.IsAlive;

        if (!playerAlive && !opponentAlive) EndFight(null);
        else if (!playerAlive)             EndFight(opponentFighter);
        else if (!opponentAlive)           EndFight(playerFighter);
    }

    // ── Setup ─────────────────────────────────────────────────────────────────

    private void AutoAssignFighters()
    {
        // Try tag-based lookup as a fallback if not assigned in Inspector.
        if (playerFighter == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerFighter = p.GetComponent<BoxBattler>();
        }
        if (opponentFighter == null)
        {
            GameObject o = GameObject.FindGameObjectWithTag("Opponent");
            if (o != null) opponentFighter = o.GetComponent<BoxBattler>();
        }

        if (playerFighter == null || opponentFighter == null)
            Debug.LogError("[FightManager] Could not find both fighters. Assign them in the Inspector or tag them 'Player' / 'Opponent'.");
    }

    private void SetupPlayerFighter()
    {
        if (playerFighter == null) return;

        if (GameManager.Instance == null || !GameManager.Instance.HasParts)
        {
            Debug.LogWarning("[FightManager] No GameManager or no parts set — player fighter will use default/empty parts. " +
                             "This is fine for direct scene testing.");
            return;
        }

        playerFighter.GetComponent<MutantFighter>().SetParts(
            GameManager.Instance.PlayerHead,
            GameManager.Instance.PlayerBody,
            GameManager.Instance.PlayerLegs
        );
    }

    private void SetupOpponentFighter()
    {
        if (opponentFighter == null || opponentGenerator == null)
        {
            Debug.LogWarning("[FightManager] OpponentGenerator not assigned — opponent uses whatever parts are pre-set on its prefab.");
            return;
        }

        opponentGenerator.GenerateOpponent(opponentFighter.GetComponent<MutantFighter>());
    }

    private void StartFight()
    {
        if (playerFighter == null || opponentFighter == null) return;
        fightActive = true;
        Debug.Log("=== FIGHT START ===");
    }

    // ── End Fight ─────────────────────────────────────────────────────────────

    private void EndFight(BoxBattler fightWinner)
    {
        fightActive = false;
        winner      = fightWinner;

        if (winner == playerFighter)
        {
            int bonusGold = GameManager.Instance?.GoldBonusThisFight ?? 0;
            int totalGold = goldRewardOnWin + bonusGold;
            GameManager.Instance?.AddGold(totalGold);
            GameManager.Instance?.AdvanceRound();
            Debug.Log($"=== PLAYER WINS! +{totalGold} gold ===");
        }
        else if (winner == opponentFighter)
        {
            Debug.Log("=== OPPONENT WINS ===");
        }
        else
        {
            Debug.Log("=== TIE — both fighters fell! ===");
        }

        // TODO: Show your results UI here, then call LoadNextScene() when ready.
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by BoxBattler to get the opposing fighter for directional dashing.
    /// </summary>
    public BoxBattler GetOpponentOf(BoxBattler fighter)
    {
        if (fighter == playerFighter)   return opponentFighter;
        if (fighter == opponentFighter) return playerFighter;
        return null;
    }

    public bool       PlayerWon()  => winner == playerFighter;
    public BoxBattler GetWinner()  => winner;

    public void LoadNextScene()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }

    public void ResetFight()
    {
        fightActive = false;
        winner      = null;
    }
}
