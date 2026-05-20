using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Glue layer between Unity's UI and SlotMachineManager. Subscribes to
/// OnStateChanged to keep button interactability and labels in sync.
///
/// Wire-up in the Editor:
///   1. Drop this on a "SlotMachineUI" GameObject under the Canvas.
///   2. Drag SlotMachineManager into the "Manager" slot.
///   3. Wire each Button's OnClick to the corresponding method here
///      (SpinPressed / LockInPressed / HoldHeadPressed / NudgeBodyUpPressed / etc.).
///   4. Drag the four TMP_Text fields (spins, actions, gold, combo) into their slots.
/// </summary>
public class SlotMachineUI : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private SlotMachineManager manager;

    [Header("Action Buttons")]
    [SerializeField] private Button spinButton;
    [SerializeField] private Button lockInButton;

    [SerializeField] private Button holdHeadButton;
    [SerializeField] private Button holdBodyButton;
    [SerializeField] private Button holdLegsButton;

    [SerializeField] private Button nudgeHeadUpButton;
    [SerializeField] private Button nudgeHeadDownButton;
    [SerializeField] private Button nudgeBodyUpButton;
    [SerializeField] private Button nudgeBodyDownButton;
    [SerializeField] private Button nudgeLegsUpButton;
    [SerializeField] private Button nudgeLegsDownButton;

    [Header("Labels")]
    [SerializeField] private TMP_Text spinsLabel;
    [SerializeField] private TMP_Text actionsLabel;
    [SerializeField] private TMP_Text goldLabel;
    [SerializeField] private TMP_Text comboPreviewLabel;
    [SerializeField] private TMP_Text statPreviewLabel;

    private void OnEnable()
    {
        if (manager != null) manager.OnStateChanged += Refresh;
    }

    private void OnDisable()
    {
        if (manager != null) manager.OnStateChanged -= Refresh;
    }

    private void Start()
    {
        // Wire each button to the matching method without forcing the user to do
        // it manually in Inspector. They can still override in Inspector if they want.
        BindIfPresent(spinButton,           SpinPressed);
        BindIfPresent(lockInButton,         LockInPressed);
        BindIfPresent(holdHeadButton,       HoldHeadPressed);
        BindIfPresent(holdBodyButton,       HoldBodyPressed);
        BindIfPresent(holdLegsButton,       HoldLegsPressed);
        BindIfPresent(nudgeHeadUpButton,    NudgeHeadUpPressed);
        BindIfPresent(nudgeHeadDownButton,  NudgeHeadDownPressed);
        BindIfPresent(nudgeBodyUpButton,    NudgeBodyUpPressed);
        BindIfPresent(nudgeBodyDownButton,  NudgeBodyDownPressed);
        BindIfPresent(nudgeLegsUpButton,    NudgeLegsUpPressed);
        BindIfPresent(nudgeLegsDownButton,  NudgeLegsDownPressed);
        Refresh();
    }

    private static void BindIfPresent(Button b, UnityEngine.Events.UnityAction call)
    {
        if (b == null) return;
        b.onClick.RemoveListener(call);   // dedupe in case of script reload
        b.onClick.AddListener(call);
    }

    // ── Button Handlers (assignable in Inspector OnClick) ────────────────────
    public void SpinPressed()           => manager?.Spin();
    public void LockInPressed()         => manager?.LockInAndFight();
    public void HoldHeadPressed()       => manager?.ToggleHold(0);
    public void HoldBodyPressed()       => manager?.ToggleHold(1);
    public void HoldLegsPressed()       => manager?.ToggleHold(2);
    public void NudgeHeadUpPressed()    => manager?.Nudge(0,  1);
    public void NudgeHeadDownPressed()  => manager?.Nudge(0, -1);
    public void NudgeBodyUpPressed()    => manager?.Nudge(1,  1);
    public void NudgeBodyDownPressed()  => manager?.Nudge(1, -1);
    public void NudgeLegsUpPressed()    => manager?.Nudge(2,  1);
    public void NudgeLegsDownPressed()  => manager?.Nudge(2, -1);

    // ── Refresh ──────────────────────────────────────────────────────────────

    private void Refresh()
    {
        if (manager == null) return;

        int spins   = manager.SpinsRemaining;
        int actions = manager.ActionsRemaining;
        bool busy   = manager.IsSpinning;

        if (spinsLabel   != null) spinsLabel.text   = $"Spins: {spins}";
        if (actionsLabel != null) actionsLabel.text = $"Actions: {actions}";
        if (goldLabel    != null && GameManager.Instance != null)
            goldLabel.text = $"Gold: {GameManager.Instance.Gold}";

        if (spinButton   != null) spinButton.interactable   = !busy && spins   > 0;
        if (lockInButton != null) lockInButton.interactable = manager.CanLockIn;

        SetInteractable(holdHeadButton, !busy && manager.GetResult(0) != null && actions > 0);
        SetInteractable(holdBodyButton, !busy && manager.GetResult(1) != null && actions > 0);
        SetInteractable(holdLegsButton, !busy && manager.GetResult(2) != null && actions > 0);

        bool canNudgeH = !busy && manager.GetResult(0) != null && actions > 0;
        bool canNudgeB = !busy && manager.GetResult(1) != null && actions > 0;
        bool canNudgeL = !busy && manager.GetResult(2) != null && actions > 0;
        SetInteractable(nudgeHeadUpButton,   canNudgeH);
        SetInteractable(nudgeHeadDownButton, canNudgeH);
        SetInteractable(nudgeBodyUpButton,   canNudgeB);
        SetInteractable(nudgeBodyDownButton, canNudgeB);
        SetInteractable(nudgeLegsUpButton,   canNudgeL);
        SetInteractable(nudgeLegsDownButton, canNudgeL);

        RefreshComboPreview();
    }

    private static void SetInteractable(Button b, bool v)
    {
        if (b != null) b.interactable = v;
    }

    // ── Combo + Stat Preview ─────────────────────────────────────────────────
    // Mirrors MutantFighter.CalculateAndApplyCombo() so the player can see the
    // payoff before they commit. If you change that logic, mirror it here too.

    private void RefreshComboPreview()
    {
        if (comboPreviewLabel == null && statPreviewLabel == null) return;

        AnimalData h = manager.ResultHead;
        AnimalData b = manager.ResultBody;
        AnimalData l = manager.ResultLegs;

        int   atk = h != null ? h.headAttack : 0;
        int   hp  = b != null ? b.bodyHealth : 0;
        int   spd = l != null ? l.legsSpeed  : 0;
        string comboText = "No combo";

        if (h != null && h == b && b == l)
        {
            atk = Mathf.RoundToInt(atk * 3f);
            hp  = Mathf.RoundToInt(hp  * 3f);
            spd = Mathf.RoundToInt(spd * 3f);
            comboText = $"TRIPLE {h.animalName?.ToUpper()}! x3 ALL — JACKPOT!";
        }
        else if (h != null && h == b)
        {
            atk = Mathf.RoundToInt(atk * 2f);
            hp  = Mathf.RoundToInt(hp  * 2f);
            comboText = $"{h.animalName} Head+Body x2 ATK+HP";
        }
        else if (b != null && b == l)
        {
            hp  = Mathf.RoundToInt(hp  * 2f);
            spd = Mathf.RoundToInt(spd * 2f);
            comboText = $"{b.animalName} Body+Legs x2 HP+SPD";
        }
        else if (h != null && h == l)
        {
            atk = Mathf.RoundToInt(atk * 2f);
            spd = Mathf.RoundToInt(spd * 2f);
            comboText = $"{h.animalName} Head+Legs x2 ATK+SPD";
        }

        if (comboPreviewLabel != null) comboPreviewLabel.text = comboText;
        if (statPreviewLabel  != null) statPreviewLabel.text  = $"ATK {atk}   HP {hp}   SPD {spd}";
    }
}
