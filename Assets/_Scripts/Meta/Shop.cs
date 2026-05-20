using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Spawns one button per ShopItem under a vertical layout, handles the purchase,
/// and routes the effect to GameManager. Persistent state (purchased one-shots,
/// drop-rate adjustments) lives on GameManager so it survives scene loads.
///
/// Wire-up in the Editor:
///   1. Add this to a "Shop" GameObject in the Shop scene.
///   2. Set "Item Button Prefab" to a Button prefab whose root has a TMP_Text
///      child for the label.
///   3. Set "Item List Parent" to a Canvas child with a VerticalLayoutGroup.
///   4. Populate "Items" with the ShopItem ScriptableObjects you want to sell.
///   5. Wire the "Back" Button.OnClick → BackToHub().
/// </summary>
public class Shop : MonoBehaviour
{
    [Header("Catalog")]
    [SerializeField] private ShopItem[] items;

    [Header("Spawned UI")]
    [SerializeField] private Button   itemButtonPrefab;
    [SerializeField] private Transform itemListParent;
    [SerializeField] private TMP_Text goldLabel;
    [SerializeField] private TMP_Text feedbackLabel;

    [Header("Navigation")]
    [SerializeField] private string hubSceneName = "TournamentHub";

    // Cache: button → item so we can refresh interactability/labels after a purchase.
    private readonly List<(Button button, ShopItem item, TMP_Text label)> spawned = new();

    private void Start()
    {
        BuildList();
        Refresh();
    }

    private void BuildList()
    {
        if (itemButtonPrefab == null || itemListParent == null)
        {
            Debug.LogError("[Shop] Item Button Prefab or Item List Parent is not assigned.");
            return;
        }

        foreach (ShopItem item in items)
        {
            if (item == null) continue;
            Button btn   = Instantiate(itemButtonPrefab, itemListParent);
            TMP_Text lbl = btn.GetComponentInChildren<TMP_Text>();
            btn.onClick.AddListener(() => Purchase(item));
            spawned.Add((btn, item, lbl));
        }
    }

    private void Refresh()
    {
        if (goldLabel != null && GameManager.Instance != null)
            goldLabel.text = $"Gold: {GameManager.Instance.Gold}";

        foreach (var entry in spawned)
        {
            bool owned     = entry.item.oneShot
                             && GameManager.Instance != null
                             && GameManager.Instance.HasPurchased(entry.item.name);
            bool canAfford = GameManager.Instance != null && GameManager.Instance.Gold >= entry.item.cost;

            entry.button.interactable = !owned && canAfford;

            if (entry.label != null)
            {
                string ownedTag = owned ? " (Owned)" : "";
                entry.label.text = $"<b>{entry.item.itemName}</b>{ownedTag}\n" +
                                   $"{entry.item.description}\n" +
                                   $"<color=#FFD24A>{entry.item.cost}g</color>";
            }
        }
    }

    private void Purchase(ShopItem item)
    {
        if (GameManager.Instance == null) return;
        if (item.oneShot && GameManager.Instance.HasPurchased(item.name))
        {
            Feedback("Already owned.");
            return;
        }
        if (!GameManager.Instance.SpendGold(item.cost))
        {
            Feedback("Not enough gold.");
            return;
        }

        switch (item.kind)
        {
            case ShopItem.EffectKind.DropRateAdjust:
                GameManager.Instance.AdjustDropRateModifier(item.targetAnimalName, item.dropRateDelta);
                Feedback($"{item.itemName} applied: {item.targetAnimalName} {(item.dropRateDelta >= 0 ? "+" : "")}{item.dropRateDelta}%");
                break;

            case ShopItem.EffectKind.GoldGift:
                GameManager.Instance.AddGold(item.goldAmount);
                Feedback($"+{item.goldAmount}g");
                break;
        }

        if (item.oneShot) GameManager.Instance.MarkPurchased(item.name);
        Refresh();
    }

    private void Feedback(string msg)
    {
        if (feedbackLabel != null) feedbackLabel.text = msg;
        Debug.Log($"[Shop] {msg}");
    }

    public void BackToHub()
    {
        if (!string.IsNullOrEmpty(hubSceneName)) SceneManager.LoadScene(hubSceneName);
    }
}
