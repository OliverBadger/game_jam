using UnityEngine;

/// <summary>
/// A single purchasable shop entry. Currently supports two effect kinds:
///   • DropRateAdjust  — adds (or subtracts) percentage points from a target
///                       animal's drop rate.
///   • GoldGift        — instant gold (used for "starter pack" style items).
///
/// Create via right-click in the Project window → Create → MutantMashup/Shop Item.
/// </summary>
[CreateAssetMenu(fileName = "NewShopItem", menuName = "MutantMashup/Shop Item")]
public class ShopItem : ScriptableObject
{
    public enum EffectKind { DropRateAdjust, GoldGift }

    [Header("Display")]
    public string itemName;
    [TextArea(2, 4)] public string description;
    public Sprite icon;

    [Header("Pricing")]
    public int cost = 50;
    [Tooltip("If true, this item can only be bought once per tournament. The shop tracks purchases by itemName.")]
    public bool oneShot = true;

    [Header("Effect")]
    public EffectKind kind = EffectKind.DropRateAdjust;

    [Tooltip("Used by DropRateAdjust. Must match an AnimalData.animalName exactly (e.g. 'Lion').")]
    public string targetAnimalName;

    [Tooltip("Used by DropRateAdjust. Positive = better odds; negative = worse odds. Stacks across purchases.")]
    public float dropRateDelta;

    [Tooltip("Used by GoldGift.")]
    public int goldAmount;
}
