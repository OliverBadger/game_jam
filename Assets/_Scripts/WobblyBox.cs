using UnityEngine;

public class WobblyBox : MonoBehaviour
{
    [Header("Wobble Settings")]
    public Transform visualModel; // Assign the actual box model here
    public float bounceSpeed = 5f;
    public float bounceHeight = 0.5f;
    public float rotationAngle = 35f;

    void Update()
    {
        // Creates a smooth wave between -1 and 1 based on time
        float wave = Mathf.Sin(Time.time * bounceSpeed);
        
        // Bouncing up and down (Parabola effect)
        float currentHeight = Mathf.Abs(wave) * bounceHeight; 
        visualModel.localPosition = new Vector3(0, currentHeight, 0);

        // Rotating left to right (up to 35 degrees)
        float currentRotation = wave * rotationAngle;
        visualModel.localRotation = Quaternion.Euler(0, 0, currentRotation);
    }
}