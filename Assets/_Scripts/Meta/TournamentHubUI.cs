using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// The "between fights" lobby. Shows round + gold, and exposes buttons to
/// open the shop or move to the budget-selector + slot machine flow.
///
/// Wire-up in the Editor:
///   1. Add this to a "TournamentHubUI" GameObject in the TournamentHub scene.
///   2. Wire the two TMP_Text fields (roundText, goldText).
///   3. On the "Go To Shop" Button.OnClick → call OpenShop().
///   4. On the "Continue" Button.OnClick → call OpenBudgetSelector().
///   5. Set the Scene Name fields to match your scene asset names.
/// </summary>
public class TournamentHubUI : MonoBehaviour
{
    [Header("Labels")]
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text statusText;

    [Header("Scenes")]
    [SerializeField] private string upgradeShopSceneName    = "UpgradeShop";
    [SerializeField] private string budgetSelectorSceneName = "BudgetSelector";
    [SerializeField] private string championSceneName       = "Champion";

    private void Start()
    {
        // If the player just won the final round, kick straight to the champion screen.
        if (GameManager.Instance != null && GameManager.Instance.IsTournamentOver())
        {
            SceneManager.LoadScene(championSceneName);
            return;
        }
        Refresh();
    }

    private void OnEnable()
    {
        // Cheap re-refresh in case gold was changed by the shop scene before
        // returning here. Start() runs once, but OnEnable runs whenever the
        // GameObject re-activates.
        Refresh();
    }

    private void Refresh()
    {
        if (GameManager.Instance == null)
        {
            if (statusText != null) statusText.text = "No GameManager — start from the boot scene.";
            return;
        }

        if (roundText != null)
            roundText.text = $"Round {GameManager.Instance.CurrentRound} / {GameManager.Instance.TotalRounds}";
        if (goldText != null)
            goldText.text = $"Gold: {GameManager.Instance.Gold}";
        if (statusText != null)
            statusText.text = "Buy upgrades, or jump into the next fight.";
    }

    public void OpenUpgradeShop()
    {
        if (!string.IsNullOrEmpty(upgradeShopSceneName))
            SceneManager.LoadScene(upgradeShopSceneName);
    }

    /// <summary>Back-compat alias for any Button.OnClick still wired to "OpenShop".</summary>
    public void OpenShop() => OpenUpgradeShop();

    public void OpenBudgetSelector()
    {
        if (!string.IsNullOrEmpty(budgetSelectorSceneName))
            SceneManager.LoadScene(budgetSelectorSceneName);
    }
}
