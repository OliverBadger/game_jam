using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Pre-fight risk/reward picker. Three preset buttons set the spin/action
/// budget on the GameManager and load the slot machine scene.
///
/// Defaults (configurable in Inspector):
///   Conservative  → 2 actions, 1 spin,  +40 gold bonus on win
///   Standard      → 5 actions, 3 spins, +20 gold bonus on win
///   Aggressive    → 7 actions, 5 spins,  +0 gold bonus on win
/// </summary>
public class BudgetSelectorUI : MonoBehaviour
{
    [Serializable]
    public class BudgetPreset
    {
        public string label    = "Standard";
        public int    actions  = 5;
        public int    spins    = 3;
        public int    goldBonus = 20;
    }

    [Header("Presets (3 expected)")]
    [SerializeField] private BudgetPreset conservative = new BudgetPreset { label = "Conservative", actions = 2, spins = 1, goldBonus = 40 };
    [SerializeField] private BudgetPreset standard     = new BudgetPreset { label = "Standard",     actions = 5, spins = 3, goldBonus = 20 };
    [SerializeField] private BudgetPreset aggressive   = new BudgetPreset { label = "Aggressive",   actions = 7, spins = 5, goldBonus = 0  };

    [Header("Buttons + Labels")]
    [SerializeField] private Button conservativeButton;
    [SerializeField] private Button standardButton;
    [SerializeField] private Button aggressiveButton;
    [SerializeField] private TMP_Text conservativeLabel;
    [SerializeField] private TMP_Text standardLabel;
    [SerializeField] private TMP_Text aggressiveLabel;

    [Header("Scene")]
    [SerializeField] private string slotMachineSceneName = "SlotMachine";

    private void Start()
    {
        WireButton(conservativeButton, () => ChoosePreset(conservative));
        WireButton(standardButton,     () => ChoosePreset(standard));
        WireButton(aggressiveButton,   () => ChoosePreset(aggressive));

        SetLabel(conservativeLabel, conservative);
        SetLabel(standardLabel,     standard);
        SetLabel(aggressiveLabel,   aggressive);
    }

    private static void WireButton(Button b, UnityEngine.Events.UnityAction call)
    {
        if (b == null) return;
        b.onClick.RemoveAllListeners();
        b.onClick.AddListener(call);
    }

    private static void SetLabel(TMP_Text label, BudgetPreset p)
    {
        if (label == null) return;
        string bonus = p.goldBonus > 0 ? $"+{p.goldBonus}g" : "no bonus";
        label.text = $"{p.label}\n{p.spins} spins · {p.actions} actions · {bonus}";
    }

    public void ChoosePreset(BudgetPreset preset)
    {
        if (preset == null) return;
        if (GameManager.Instance != null)
            GameManager.Instance.SetFightBudget(preset.actions, preset.spins, preset.goldBonus);
        else
            Debug.LogWarning("[BudgetSelectorUI] No GameManager.Instance — preset not persisted.");

        if (!string.IsNullOrEmpty(slotMachineSceneName))
            SceneManager.LoadScene(slotMachineSceneName);
    }
}
