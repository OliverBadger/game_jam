using UnityEngine;

/// <summary>
/// Attach this to the "VisualRoot" child of a fighter.
/// It modifies its own local position and rotation to create an idle bounce
/// and a forward lean during dashes. The physics Rigidbody2D on the parent
/// root is never touched, so combat physics and visuals are fully independent.
/// </summary>
public class WobblyBox : MonoBehaviour
{
    [SerializeField] private float bounceSpeed     = 6f;
    [SerializeField] private float bounceHeight    = 0.08f;
    [SerializeField] private float idleLeanAngle   = 12f;
    [SerializeField] private float dashLeanAngle   = 28f;
    [SerializeField] private float dashLeanSpeed   = 14f;

    private bool  inDashMode;
    private float dashDirection = 1f;   // +1 = dashing right, -1 = dashing left

    private void Update()
    {
        // Always bounce on the Y axis — this gives the idle "alive" feeling.
        float wave   = Mathf.Sin(Time.time * bounceSpeed);
        float bounceY = Mathf.Abs(wave) * bounceHeight;
        transform.localPosition = new Vector3(0f, bounceY, 0f);

        if (inDashMode)
        {
            // Lean forward in the direction of the dash (tilt into the charge).
            // dashDirection: +1 means opponent is to the right, so lean right (negative Z in Unity).
            float targetZ = -dashDirection * dashLeanAngle;
            float currentZ = transform.localEulerAngles.z;
            if (currentZ > 180f) currentZ -= 360f;   // Convert to signed -180..180
            float smoothZ = Mathf.LerpAngle(currentZ, targetZ, Time.deltaTime * dashLeanSpeed);
            transform.localRotation = Quaternion.Euler(0f, 0f, smoothZ);
        }
        else
        {
            // Idle sway: gentle left/right rock.
            transform.localRotation = Quaternion.Euler(0f, 0f, wave * idleLeanAngle);
        }
    }

    /// <summary>
    /// Called by BoxBattler when a dash starts or ends.
    /// </summary>
    /// <param name="dashing">True = enter dash lean, False = return to idle sway.</param>
    /// <param name="direction">+1 if dashing right, -1 if dashing left.</param>
    public void SetDashMode(bool dashing, float direction = 1f)
    {
        inDashMode    = dashing;
        dashDirection = direction;
    }
}