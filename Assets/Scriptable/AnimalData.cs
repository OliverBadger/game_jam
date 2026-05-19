using UnityEngine;

// This adds a shortcut to Unity's Right-Click -> Create menu
[CreateAssetMenu(fileName = "NewAnimal", menuName = "MutantMashup/Animal Data")]
public class AnimalData : ScriptableObject
{
    public string animalName;
    
    [Tooltip("Base percentage chance to drop. Can be modified by shop upgrades later.")]
    public float baseDropRate;

    [Header("Base Stats")]
    public int headAttack;
    public int bodyHealth;
    public int legsSpeed;

    [Header("Flavor & Art")]
    [TextArea(2, 4)]
    public string archetypeDescription;
    
    // Slot for your 16-bit pixel art
    public Sprite headSprite; 
    public Sprite bodySprite;
    public Sprite legsSprite;
    
    [Header("Visual Offsets (local)")]
    [Tooltip("Local position offset for this part when attached to a fighter. Useful for pixel-perfect 16-bit placement.")]
    public Vector3 headOffset;
    public Vector3 bodyOffset;
    public Vector3 legsOffset;
}