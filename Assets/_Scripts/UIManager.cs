using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles all in-fight HUD: health bars, round/gold display, and the result panel.
/// Drag this onto a UIManager GameObject in the Fight scene, then wire up every
/// [SerializeField] slot in the Inspector.
/// </summary>
public class UIManager : MonoBehaviour
{
    // ── Player HUD ────────────────────────────────────────────────────────────
    [Header("Player HUD")]
    [SerializeField] private Slider   playerHealthBar;
    [SerializeField] private TMP_Text playerHealthText;
    [SerializeField] private TMP_Text playerNameText;

    // ── Opponent HUD ──────────────────────────────────────────────────────────
    [Header("Opponent HUD")]
    [SerializeField] private Slider   opponentHealthBar;
    [SerializeField] private TMP_Text opponentHealthText;
    [SerializeField] private TMP_Text opponentNameText;

    // ── Top Bar ───────────────────────────────────────────────────────────────
    [Header("Top Bar")]
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private TMP_Text goldText;

    // ── Result Panel (hidden until fight ends) ────────────────────────────────
    [Header("Result Panel")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text   resultText;

    // ── Internal ──────────────────────────────────────────────────────────────
    private FightManager fightManager;
    private BoxBattler   playerFighter;
    private BoxBattler   opponentFighter;
    private bool         _resultShown;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        fightManager    = FindFirstObjectByType<FightManager>();

        if (fightManager != null)
        {
            playerFighter   = fightManager.GetPlayerFighter();
            opponentFighter = fightManager.GetOpponentFighter();
        }

        if (resultPanel != null)
            resultPanel.SetActive(false);

        RefreshStaticText();
    }

    private void Update()
    {
        RefreshHealthBars();
        RefreshGold();

        if (fightManager != null && fightManager.FightHasEnded)
            ShowResult();
    }

    // ── Health Bars ───────────────────────────────────────────────────────────

    private void RefreshHealthBars()
    {
        UpdateBar(playerHealthBar,   playerHealthText,   playerFighter);
        UpdateBar(opponentHealthBar, opponentHealthText, opponentFighter);
    }

    private static void UpdateBar(Slider bar, TMP_Text label, BoxBattler fighter)
    {
        if (fighter == null) return;

        int   hp    = Mathf.Max(0, fighter.CurrentHealth);
        int   max   = fighter.MaxHealth;
        float ratio = max > 0 ? (float)hp / max : 0f;

        if (bar   != null) bar.value  = ratio;
        if (label != null) label.text = $"{hp} / {max}";
    }

    // ── Static Text ───────────────────────────────────────────────────────────

    private void RefreshStaticText()
    {
        if (roundText != null && GameManager.Instance != null)
            roundText.text = $"Round {GameManager.Instance.CurrentRound} / {GameManager.Instance.TotalRounds}";

        if (playerNameText   != null) playerNameText.text   = "YOU";
        if (opponentNameText != null) opponentNameText.text = "OPPONENT";
    }

    private void RefreshGold()
    {
        if (goldText != null && GameManager.Instance != null)
            goldText.text = $"Gold: {GameManager.Instance.Gold}";
    }

    // ── Result Panel ──────────────────────────────────────────────────────────

    private void ShowResult()
    {
        if (_resultShown) return;
        _resultShown = true;

        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (resultText == null) return;

        BoxBattler winner = fightManager.GetWinner();
        if      (winner == playerFighter)   resultText.text = "YOU WIN!";
        else if (winner == opponentFighter) resultText.text = "YOU LOSE...";
        else                                resultText.text = "TIE...";
    }
}
