using UnityEngine;

/// <summary>
/// Master list of every Upgrade available in the game.
///
/// Why an SO and not just an array field on the shop?
///   • The Registry (lives on GameManager) and the Shop UI (lives in the shop
///     scene) both need to enumerate every upgrade. Putting the array in one
///     spot and referencing it from both keeps them in sync.
///   • Designers tune the entire roster from a single asset — no scene re-open.
///
/// Create with: right-click in Project → Create → MutantMashup/Upgrade Catalog.
/// </summary>
[CreateAssetMenu(fileName = "UpgradeCatalog", menuName = "MutantMashup/Upgrade Catalog")]
public class UpgradeCatalog : ScriptableObject
{
    public Upgrade[] all;
}
