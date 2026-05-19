using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MutantFighter))]
public class BoxBattler : MonoBehaviour
{
    [Header("Fighter References")]
    private MutantFighter mutant;
    private Rigidbody rb;
    
    [Header("Combat Stats (Read from MutantFighter)")]
    public int maxHealth;
    public int currentHealth;
    public int attackDamage;
    public int speedStat;
    public float baseAttackCooldown = 3f;
    
    [Header("Physics")]
    public float dashForce = 15f;
    public float knockbackForce = 10f;
    
    [Header("Dash State")]
    private float dashTimer;
    public bool isDashing = false;
    public bool isAlive = true;
    void Start()
    {
        // Get references
        mutant = GetComponent<MutantFighter>();
        rb = GetComponent<Rigidbody>();
        
        // Pull final stats from the mutant
        InitializeStats();
    }

    void InitializeStats()
    {
        attackDamage = mutant.currentAttack;
        maxHealth = mutant.currentHealth;
        currentHealth = maxHealth;
        speedStat = mutant.currentSpeed;
        
        // Reset dash timer
        dashTimer = 0f;
        
        Debug.Log($"{gameObject.name} initialized: {maxHealth}HP, {attackDamage} ATK, {speedStat} SPD");
    }

    void Update()
    {
        if (!isAlive) return;

        // 1. Calculate Dash Frequency based on Speed
        // Higher speed = more frequent attacks (lower cooldown multiplier)
        float cooldownModifier = 1f / (1f + (speedStat * 0.05f)); // Speed increases attack frequency
        dashTimer -= Time.deltaTime / cooldownModifier;

        if (dashTimer <= 0f && !isDashing)
        {
            StartCoroutine(PerformDash());
        }
    }

    IEnumerator PerformDash()
    {
        isDashing = true;
        
        // 2. The Forward Boost
        // Adds a sudden impulse of force forward
        rb.AddForce(transform.forward * dashForce, ForceMode.Impulse);

        // 3. Dash Duration (Active Damage Window)
        // Faster attackers have shorter dash duration
        float dashDuration = 0.5f / (1f + (speedStat * 0.03f));
        yield return new WaitForSeconds(dashDuration);
        
        isDashing = false;
        
        // Reset timer (Base cooldown before speed modifies it)
        dashTimer = baseAttackCooldown; 
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isAlive) return;

        // Check if we hit another fighter
        BoxBattler enemy = collision.gameObject.GetComponent<BoxBattler>();

        if (enemy != null && enemy.isAlive)
        {
            // If I am dashing, I hit them!
            if (this.isDashing)
            {
                Debug.Log($"{gameObject.name} smashed into {enemy.gameObject.name}!");
                
                // Calculate knockback direction (away from me)
                Vector3 pushDirection = (enemy.transform.position - transform.position).normalized;
                
                // Add a slight upward angle so they bounce back in a silly way
                pushDirection.y = 0.5f; 

                // Apply the Knockback force to the enemy
                enemy.GetComponent<Rigidbody>().AddForce(pushDirection * knockbackForce, ForceMode.Impulse);

                // Deal damage equal to this fighter's attack stat
                enemy.TakeDamage(this.attackDamage);
            }
        }
    }

    /// <summary>
    /// Reduces health and checks if fighter is defeated
    /// </summary>
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"{gameObject.name} took {damage} damage! Health: {currentHealth}/{maxHealth}");
        
        if (currentHealth <= 0)
        {
            Defeat();
        }
    }

    /// <summary>
    /// Called when health reaches 0 - fighter is knocked out
    /// </summary>
    public void Defeat()
    {
        isAlive = false;
        isDashing = false;
        
        Debug.Log($"{gameObject.name} has been defeated!");
        
        // Disable physics and movement
        rb.isKinematic = true;
        enabled = false;
    }

    /// <summary>
    /// Returns winner status
    /// </summary>
    public bool HasWon()
    {
        return isAlive && currentHealth > 0;
    }
}