using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A single slot reel that displays the part sprite for ONE slot (head, body, or legs).
/// Works with either a SpriteRenderer (world-space slot machine art) or a UI Image
/// (Canvas-based slot machine). Drag whichever one you use into the matching slot.
///
/// Wire-up in the Editor:
///   1. Add this component to each of three reel GameObjects (HeadReel, BodyReel, LegsReel).
///   2. Drag the part sprite renderer/image into "Display Renderer" or "Display Image".
///   3. Optionally drag a "Held Indicator" GameObject (e.g. a "HELD" badge) that
///      becomes visible while the reel is held.
/// </summary>
public class SlotReel : MonoBehaviour
{
    [Header("Display target — assign ONE of these")]
    [SerializeField] private SpriteRenderer displayRenderer;
    [SerializeField] private Image          displayImage;

    [Header("Optional")]
    [Tooltip("Shown while the reel is held. Toggled by SetHeldVisual().")]
    [SerializeField] private GameObject heldIndicator;
    [Tooltip("How many frames per second the reel flickers through random sprites during a spin.")]
    [SerializeField] private float spinFps = 18f;

    private Coroutine spinRoutine;
    private AnimalData _currentAnimal;

    public AnimalData CurrentAnimal => _currentAnimal;

    private void Awake()
    {
        if (heldIndicator != null) heldIndicator.SetActive(false);
    }

    // ── Spin Animation ───────────────────────────────────────────────────────

    /// <summary>
    /// Plays a flicker animation through the animal pool, then settles on the target.
    /// </summary>
    public void PlaySpin(AnimalData[] pool, AnimalData target, SlotMachineManager.ReelKind kind, float duration)
    {
        if (spinRoutine != null) StopCoroutine(spinRoutine);
        spinRoutine = StartCoroutine(SpinRoutine(pool, target, kind, duration));
    }

    private IEnumerator SpinRoutine(AnimalData[] pool, AnimalData target, SlotMachineManager.ReelKind kind, float duration)
    {
        float elapsed   = 0f;
        float frameTime = spinFps > 0f ? 1f / spinFps : 0.05f;

        while (elapsed < duration)
        {
            // Pick a random non-null entry to keep the flicker visually noisy.
            AnimalData flicker = pool != null && pool.Length > 0
                ? pool[Random.Range(0, pool.Length)]
                : null;
            ApplySprite(flicker, kind);
            yield return new WaitForSeconds(frameTime);
            elapsed += frameTime;
        }

        ShowResult(target, kind);
        spinRoutine = null;
    }

    /// <summary>Snap the reel to a specific animal without animation.</summary>
    public void ShowResult(AnimalData animal, SlotMachineManager.ReelKind kind)
    {
        _currentAnimal = animal;
        ApplySprite(animal, kind);
    }

    private void ApplySprite(AnimalData animal, SlotMachineManager.ReelKind kind)
    {
        Sprite s = null;
        if (animal != null)
        {
            // Each reel only ever shows its OWN slot's sprite (a head reel never
            // shows a body sprite, even if the animal data is a bear).
            s = kind switch
            {
                SlotMachineManager.ReelKind.Head => animal.headSprite,
                SlotMachineManager.ReelKind.Body => animal.bodySprite,
                SlotMachineManager.ReelKind.Legs => animal.legsSprite,
                _                                => null
            };
        }

        if (displayRenderer != null) displayRenderer.sprite = s;
        if (displayImage    != null) displayImage.sprite    = s;

        // Hide the UI image quad when sprite is null so we don't get a stretched
        // default white box on empty slots.
        if (displayImage != null) displayImage.enabled = s != null;
    }

    // ── Held Indicator ───────────────────────────────────────────────────────

    public void SetHeldVisual(bool held)
    {
        if (heldIndicator != null) heldIndicator.SetActive(held);
    }
}
