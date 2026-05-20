using System.Collections;
using UnityEngine;

/// <summary>
/// Drives the 2D physics combat for a fighter.
/// Stats are pulled from MutantFighter via the OnPartsChanged event — never
/// set manually. All public fields from the original are now private.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(MutantFighter))]
public class BoxBattler : MonoBehaviour
{
    // ── Inspector Tuning (private but visible in Inspector) ──────────────────
    [SerializeField] private float dashForce         = 18f;
    [SerializeField] private float knockbackForce    = 12f;
    [SerializeField] private float baseAttackCooldown = 3f;

    // ── Component References ─────────────────────────────────────────────────
    private MutantFighter mutant;
    private Rigidbody2D   rb;
    private WobblyBox     wobble;
    private FightManager  fightManager;

    // ── Combat Stats (set from MutantFighter via event) ──────────────────────
    private int   maxHealth;
    private int   currentHealth;
    private int   attackDamage;
    private int   speedStat;

    // ── Internal State ───────────────────────────────────────────────────────
    private float dashTimer;
    private bool  _isDashing;
    private bool  _isAlive       = true;
    // Prevents double-damage when two physics contacts register within one dash window.
    // Reset at the start of every dash so each attack can land exactly once.
    private bool  _hasHitThisDash;

    // ── Read-Only Properties (FightManager and BoxBattler cross-read these) ──
    public bool IsAlive        => _isAlive;
    public int  CurrentHealth  => currentHealth;
    public int  MaxHealth      => maxHealth;
    public int  AttackDamage   => attackDamage;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        mutant = GetComponent<MutantFighter>();
        rb     = GetComponent<Rigidbody2D>();

        // Subscribe now (before any Start fires) so we receive the event when
        // FightManager.Start() calls mutant.SetParts().
        mutant.OnPartsChanged += OnPartsReady;
    }

    private void Start()
    {
        // Cache scene-level references. All Awake() calls have run by now,
        // so MutantVisuals has already created the VisualRoot + WobblyBox.
        fightManager = FindFirstObjectByType<FightManager>();
        wobble       = GetComponentInChildren<WobblyBox>();

        // Stagger each fighter's first dash so they don't all charge at frame 0.
        dashTimer = Random.Range(0.5f, baseAttackCooldown);
    }

    private void OnDestroy()
    {
        if (mutant != null) mutant.OnPartsChanged -= OnPartsReady;
    }

    // ── Stat Initialisation ───────────────────────────────────────────────────

    private void OnPartsReady(AnimalData head, AnimalData body, AnimalData legs)
    {
        attackDamage  = mutant.CurrentAttack;
        maxHealth     = mutant.CurrentHealth;
        currentHealth = maxHealth;
        speedStat     = mutant.CurrentSpeed;
        Debug.Log($"[BoxBattler] {gameObject.name} ready — {maxHealth}HP / {attackDamage}ATK / {speedStat}SPD");
    }

    // ── Per-Frame Logic ───────────────────────────────────────────────────────

    private void Update()
    {
        if (!_isAlive) return;

        // Always face the opponent so scale-flip is smooth even as they shuffle.
        FaceOpponent();

        // Speed stat shortens the cooldown between dashes.
        // Formula: higher speed → larger divisor → shorter effective cooldown.
        float cooldownScale = 1f / (1f + speedStat * 0.05f);
        dashTimer -= Time.deltaTime / cooldownScale;

        if (dashTimer <= 0f && !_isDashing)
            StartCoroutine(PerformDash());
    }

    // ── Dashing ───────────────────────────────────────────────────────────────

    private IEnumerator PerformDash()
    {
        _isDashing        = true;
        _hasHitThisDash   = false;   // fresh attack — allow exactly one hit
        float direction   = GetOpponentHorizontalSign();
        wobble?.SetDashMode(true, direction);

        // Launch toward the opponent.
        Vector2 dashDir = GetDirectionToOpponent();
        rb.AddForce(dashDir * dashForce, ForceMode2D.Impulse);

        // Faster fighters have a shorter active-hit window.
        float dashDuration = 0.45f / (1f + speedStat * 0.03f);
        yield return new WaitForSeconds(dashDuration);

        _isDashing = false;
        wobble?.SetDashMode(false);
        dashTimer = baseAttackCooldown;
    }

    // ── Collision ─────────────────────────────────────────────────────────────

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // _hasHitThisDash ensures one hit per dash even if physics re-contacts.
        if (!_isAlive || !_isDashing || _hasHitThisDash) return;

        BoxBattler enemy = collision.gameObject.GetComponent<BoxBattler>();
        if (enemy == null || !enemy.IsAlive) return;

        _hasHitThisDash = true;   // lock out further hits this dash

        // Push enemy away with a slight upward arc for that bouncy feel.
        Vector2 pushDir = ((Vector2)enemy.transform.position - (Vector2)transform.position).normalized;
        pushDir.y += 0.4f;
        pushDir.Normalize();

        enemy.GetComponent<Rigidbody2D>().AddForce(pushDir * knockbackForce, ForceMode2D.Impulse);
        enemy.TakeDamage(attackDamage);
    }

    // ── Damage & Defeat ───────────────────────────────────────────────────────

    public void TakeDamage(int damage)
    {
        if (!_isAlive) return;
        currentHealth -= damage;
        Debug.Log($"[BoxBattler] {gameObject.name} took {damage} dmg! HP: {currentHealth}/{maxHealth}");
        if (currentHealth <= 0) Defeat();
    }

    private void Defeat()
    {
        _isAlive        = false;
        _isDashing      = false;
        _hasHitThisDash = false;
        StopAllCoroutines();
        rb.linearVelocity = Vector2.zero;
        rb.isKinematic    = true;
        wobble?.SetDashMode(false);
        enabled = false;
        Debug.Log($"[BoxBattler] {gameObject.name} has been defeated!");
    }

    public bool HasWon() => _isAlive && currentHealth > 0;

    // ── Helpers ───────────────────────────────────────────────────────────────

    private BoxBattler GetOpponent() => fightManager?.GetOpponentOf(this);

    private Vector2 GetDirectionToOpponent()
    {
        BoxBattler opp = GetOpponent();
        if (opp == null) return transform.right;
        return ((Vector2)opp.transform.position - (Vector2)transform.position).normalized;
    }

    private float GetOpponentHorizontalSign()
    {
        BoxBattler opp = GetOpponent();
        if (opp == null) return 1f;
        return opp.transform.position.x > transform.position.x ? 1f : -1f;
    }

    private void FaceOpponent()
    {
        BoxBattler opp = GetOpponent();
        if (opp == null) return;

        bool opponentIsRight = opp.transform.position.x > transform.position.x;
        Vector3 s = transform.localScale;
        s.x = opponentIsRight ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
        transform.localScale = s;
    }
}