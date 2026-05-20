using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Orchestrates a fight between the player fighter and a generated opponent.
/// Execution order -10 ensures Start() fires before BoxBattler (order 0),
/// so parts are assigned via SetParts() before BoxBattler reads them.
/// </summary>
[DefaultExecutionOrder(-10)]
public class FightManager : MonoBehaviour
{
    // Scene References — drag fighters in here, or tag them "Player"/"Opponent".
    [SerializeField] private BoxBattler        playerFighter;
    [SerializeField] private BoxBattler        opponentFighter;
    [SerializeField] private OpponentGenerator opponentGenerator;

    // Rewards & Navigation
    [SerializeField] private int    goldRewardOnWin   = 50;
    // Scene to load on WIN (next round / slot machine).
    [SerializeField] private string nextSceneName     = "SlotMachine";
    // Scene to load on LOSS (restart loop or game-over screen).
    [SerializeField] private string lossSceneName     = "SlotMachine";
    // Scene to load when the FINAL round is beaten.
    [SerializeField] private string championSceneName = "Champion";
    // Pause (seconds) before loading the next scene so results are visible.
    [SerializeField] private float  resultDelay       = 2.5f;

    // Internal State
    private bool       fightActive;
    private BoxBattler winner;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

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

        if      (!playerAlive && !opponentAlive) EndFight(null);
        else if (!playerAlive)                   EndFight(opponentFighter);
        else if (!opponentAlive)                 EndFight(playerFighter);
    }

    // -------------------------------------------------------------------------
    // Setup
    // -------------------------------------------------------------------------

    private void AutoAssignFighters()
    {
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
            Debug.LogError("[FightManager] Could not find both fighters. " +
                           "Assign them in the Inspector or tag them 'Player' / 'Opponent'.");
    }

    private void SetupPlayerFighter()
    {
        if (playerFighter == null) return;

        if (GameManager.Instance == null || !GameManager.Instance.HasParts)
        {
            Debug.LogWarning("[FightManager] No GameManager parts found — " +
                             "player fighter has empty/default stats. Fine for direct scene testing.");
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
            Debug.LogWarning("[FightManager] OpponentGenerator not assigned — " +
                             "opponent uses whatever parts are pre-set on its MutantFighter.");
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

    // -------------------------------------------------------------------------
    // End Fight
    // -------------------------------------------------------------------------

    private void EndFight(BoxBattler fightWinner)
    {
        // fightActive = false is the guard — Update() returns early from now on,
        // so this method can never be called more than once per fight.
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
            // Clear player parts so the slot machine assigns fresh ones.
            GameManager.Instance?.ResetTournament();
        }
        else
        {
            // Tie — treated as a loss.
            Debug.Log("=== TIE — both fighters fell! ===");
            GameManager.Instance?.ResetTournament();
        }

        StartCoroutine(NavigateAfterDelay(winner == playerFighter));
    }

    /// <summary>
    /// Waits for resultDelay seconds so the player can see what happened,
    /// then loads the appropriate scene.
    /// </summary>
    private IEnumerator NavigateAfterDelay(bool playerWon)
    {
        yield return new WaitForSeconds(resultDelay);

        if (!playerWon)
        {
            SceneManager.LoadScene(lossSceneName);
            yield break;
        }

        // Player won — check if the whole tournament is complete.
        bool tournamentDone = GameManager.Instance != null
                              && GameManager.Instance.IsTournamentOver();
        SceneManager.LoadScene(tournamentDone ? championSceneName : nextSceneName);
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the opposing BoxBattler for the given fighter.
    /// Used by BoxBattler to aim its dash.
    /// </summary>
    public BoxBattler GetOpponentOf(BoxBattler fighter)
    {
        if (fighter == playerFighter)   return opponentFighter;
        if (fighter == opponentFighter) return playerFighter;
        return null;
    }

    public bool       PlayerWon() => winner == playerFighter;
    public BoxBattler GetWinner() => winner;

    /// <summary>Manually trigger scene load (useful for UI buttons).</summary>
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
