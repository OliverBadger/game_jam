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
        float wave    = Mathf.Sin(Time.time * bounceSpeed);
        float bounceY = Mathf.Abs(wave) * bounceHeight;
        transform.localPosition = new Vector3(0f, bounceY, 0f);

        // When the parent fighter's scale.x is negative (facing left), the
        // coordinate system is mirrored.  A raw localRotation of Z = +28° would
        // visually appear as −28°, making the fighter lean backward instead of
        // forward.  We compensate by flipping the angle with the parent sign.
        float parentSign = transform.parent != null
            ? Mathf.Sign(transform.parent.localScale.x)
            : 1f;

        if (inDashMode)
        {
            // targetZ is the angle we WANT to appear in visual/world space.
            // Divide by parentSign so the local rotation produces that visual result.
            float visualTargetZ = -dashDirection * dashLeanAngle;
            float localTargetZ  = visualTargetZ * parentSign;

            float currentZ = transform.localEulerAngles.z;
            if (currentZ > 180f) currentZ -= 360f;   // convert to signed −180..180
            float smoothZ = Mathf.LerpAngle(currentZ, localTargetZ, Time.deltaTime * dashLeanSpeed);
            transform.localRotation = Quaternion.Euler(0f, 0f, smoothZ);
        }
        else
        {
            // Idle sway: compensated so the visual rock is always relative to
            // the character's local forward, regardless of which way they face.
            transform.localRotation = Quaternion.Euler(0f, 0f, wave * idleLeanAngle * parentSign);
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