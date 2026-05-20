using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Lives on the GameManager GameObject in SampleScene (your bootstrap / first scene).
/// Assigns three test AnimalData parts to the GameManager so the Fight scene has real
/// stats to work with — no slot machine needed during development.
///
/// HOW TO USE:
///   1. Select the GameManager GameObject in SampleScene.
///   2. Add this component.
///   3. Drag AnimalData ScriptableObjects into the three Test Part slots.
///   4. Make sure Fight Scene Name matches the exact name of your fight scene.
///   5. Press Play from SampleScene — it sets parts then loads the Fight scene.
/// </summary>
public class TestBootstrap : MonoBehaviour
{
    [Header("Test Parts — drag AnimalData assets here")]
    [SerializeField] private AnimalData testHead;
    [SerializeField] private AnimalData testBody;
    [SerializeField] private AnimalData testLegs;

    [Header("Scene to load")]
    [SerializeField] private string fightSceneName = "Fight";

    private void Start()
    {
        // Only set parts if the GameManager exists and hasn't been given parts yet
        // (e.g. a previous scene already called SetPlayerParts).
        if (GameManager.Instance != null && !GameManager.Instance.HasParts)
        {
            if (testHead != null && testBody != null && testLegs != null)
            {
                GameManager.Instance.SetPlayerParts(testHead, testBody, testLegs);
                Debug.Log($"[TestBootstrap] Parts set: {testHead.animalName} / {testBody.animalName} / {testLegs.animalName}");
            }
            else
            {
                Debug.LogWarning("[TestBootstrap] One or more test parts are not assigned. " +
                                 "The player fighter will have 0 stats — assign AnimalData in the Inspector.");
            }
        }

        if (!string.IsNullOrEmpty(fightSceneName))
            SceneManager.LoadScene(fightSceneName);
    }
}
