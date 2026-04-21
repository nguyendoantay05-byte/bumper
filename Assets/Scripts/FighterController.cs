using UnityEngine;
using System.Collections;

/// <summary>
/// Lớp cha dùng chung cho người chơi và bot.
/// Xử lý di chuyển vật lý, giới hạn tốc độ, và phản lực khi va chạm.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public abstract class FighterController : MonoBehaviour
{
    [Header("General")]
    [SerializeField] private string fighterDisplayName = "Fighter";
    [SerializeField] private bool isPlayerControlled;

    [Header("Movement")]
    [SerializeField] protected float moveForce = 19.5f;
    [SerializeField] protected float maxSpeed = 6.9f;
    [SerializeField] protected float collisionBurstMaxSpeed = 21f;
    [SerializeField] protected float linearDrag = 2f;
    [SerializeField] protected float angularDrag = 5f;

    [Header("Collision")]
    [SerializeField] protected float collisionImpulse = 11.5f;
    [SerializeField] protected float defenderImpulseMultiplier = 1.28f;
    [SerializeField] protected float attackerImpulseMultiplier = 0.16f;
    [SerializeField] protected float impactSpeedBonus = 0.22f;
    [SerializeField] protected float impactBurstDuration = 0.18f;
    [SerializeField] protected float playerKnockbackMultiplier = 0.52f;
    [SerializeField] protected float botKnockbackMultiplier = 1.45f;

    protected Rigidbody2D rb;
    protected Collider2D cachedCollider;
    protected SpriteRenderer cachedRenderer;
    protected GameManager gameManager;
    protected ArenaBoundary arenaBoundary;

    private bool eliminated;
    private float impactBurstTimer;
    private float inputSpeedCapBonus;
    private FighterController lastImpactSource;
    private float lastImpactTime;

    public string FighterDisplayName => fighterDisplayName;
    public bool IsPlayerControlled => isPlayerControlled;
    public bool IsEliminated => eliminated;
    public Rigidbody2D Body => rb;
    public FighterController LastImpactSource => lastImpactSource;
    public float LastImpactTime => lastImpactTime;

    public Vector2 Position => rb != null ? rb.position : (Vector2)transform.position;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cachedCollider = GetComponent<Collider2D>();
        cachedRenderer = GetComponent<SpriteRenderer>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.linearDamping = linearDrag;
        rb.angularDamping = angularDrag;

        Vector3 currentPosition = transform.position;
        transform.position = new Vector3(currentPosition.x, currentPosition.y, 0f);

        if (cachedRenderer != null)
        {
            cachedRenderer.sortingOrder = 20;
        }

        gameManager = FindAnyObjectByType<GameManager>();
        arenaBoundary = FindAnyObjectByType<ArenaBoundary>();
    }

    protected virtual void FixedUpdate()
    {
        if (eliminated || gameManager == null || !gameManager.IsMatchRunning)
        {
            return;
        }

        HandleMovement();
        impactBurstTimer = Mathf.Max(0f, impactBurstTimer - Time.fixedDeltaTime);
        ClampVelocity();
    }

    /// <summary>
    /// Bot và người chơi sẽ cài hướng di chuyển ở đây.
    /// </summary>
    protected abstract void HandleMovement();

    protected void MoveInDirection(Vector2 direction)
    {
        MoveInDirection(direction, 1f);
    }

    protected void MoveInDirection(Vector2 direction, float strengthMultiplier)
    {
        if (rb == null)
        {
            return;
        }

        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        float clampedStrength = Mathf.Clamp(strengthMultiplier, 0f, 2.6f);
        Vector2 force = direction.normalized * moveForce * clampedStrength;
        rb.AddForce(force, ForceMode2D.Force);
    }

    protected void ClampVelocity()
    {
        if (rb == null)
        {
            return;
        }

        float allowedSpeed = impactBurstTimer > 0f ? collisionBurstMaxSpeed : maxSpeed + inputSpeedCapBonus;
        if (rb.linearVelocity.magnitude > allowedSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * allowedSpeed;
        }

        inputSpeedCapBonus = 0f;
    }

    /// <summary>
    /// Dùng khi va chạm với một fighter khác để tạo cảm giác đẩy rõ hơn.
    /// </summary>
    public void ApplyKnockback(Vector2 direction, float impulseMultiplier = 1f)
    {
        if (rb == null || eliminated)
        {
            return;
        }

        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        impactBurstTimer = impactBurstDuration;
        rb.AddForce(direction.normalized * collisionImpulse * impulseMultiplier, ForceMode2D.Impulse);
    }

    protected void SetInputSpeedCapBonus(float bonus)
    {
        inputSpeedCapBonus = Mathf.Max(inputSpeedCapBonus, bonus);
    }

    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (eliminated)
        {
            return;
        }

        FighterController other = collision.collider.GetComponentInParent<FighterController>();
        if (other == null || other == this || other.IsEliminated)
        {
            return;
        }

        Vector2 pushDirection = (other.Position - Position).normalized;
        Vector2 myVelocity = rb != null ? rb.linearVelocity : Vector2.zero;
        Vector2 otherVelocity = other.Body != null ? other.Body.linearVelocity : Vector2.zero;
        float myAttackScore = Vector2.Dot(myVelocity, pushDirection);
        float otherAttackScore = Vector2.Dot(otherVelocity, -pushDirection);
        float impactDelta = Mathf.Abs(myAttackScore - otherAttackScore);
        float impactBoost = 1f + Mathf.Clamp(impactDelta * impactSpeedBonus, 0f, 3.2f);
        float selfImpulse = defenderImpulseMultiplier * impactBoost;
        float otherImpulse = defenderImpulseMultiplier * impactBoost;

        if (IsPlayerControlled && !other.IsPlayerControlled)
        {
            selfImpulse *= playerKnockbackMultiplier;
            otherImpulse *= botKnockbackMultiplier;
        }
        else if (!IsPlayerControlled && other.IsPlayerControlled)
        {
            selfImpulse *= botKnockbackMultiplier;
            otherImpulse *= playerKnockbackMultiplier;
        }

        ApplyKnockback(-pushDirection, selfImpulse);
        other.ApplyKnockback(pushDirection, otherImpulse);
        RegisterImpactSource(other);
        other.RegisterImpactSource(this);

        FighterVisual3D myVisual = GetComponent<FighterVisual3D>();
        if (myVisual != null)
        {
            myVisual.PlayImpactPulse(Mathf.Clamp01(selfImpulse * 0.2f));
        }

        FighterVisual3D otherVisual = other.GetComponent<FighterVisual3D>();
        if (otherVisual != null)
        {
            otherVisual.PlayImpactPulse(Mathf.Clamp01(otherImpulse * 0.2f));
        }

        if (GetInstanceID() < other.GetInstanceID())
        {
            Vector3 impactPoint = collision.contactCount > 0
                ? collision.GetContact(0).point
                : (Vector2.Lerp(Position, other.Position, 0.5f));
            Color impactColor = isPlayerControlled || other.IsPlayerControlled
                ? new Color(0.22f, 0.78f, 1f, 0.9f)
                : new Color(1f, 0.52f, 0.34f, 0.9f);
            BumpEffect.Spawn(impactPoint, impactColor, Mathf.Lerp(1f, 1.45f, Mathf.Clamp01(impactDelta / 4.5f)));
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBump();
        }
    }

    public void SetDisplayName(string newName)
    {
        fighterDisplayName = string.IsNullOrWhiteSpace(newName) ? "Fighter" : newName.Trim();
    }

    public void RegisterImpactSource(FighterController source)
    {
        if (source == null || source == this)
        {
            return;
        }

        lastImpactSource = source;
        lastImpactTime = Time.time;
    }

    public void SetPlayerControlled(bool value)
    {
        isPlayerControlled = value;
    }

    public void Eliminate()
    {
        if (eliminated)
        {
            return;
        }

        eliminated = true;

        if (cachedCollider != null)
        {
            cachedCollider.enabled = false;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }
    }

    public void PlayFallIntoWater()
    {
        StartCoroutine(FallIntoWaterRoutine());
    }

    public void GrowAfterElimination(float scaleStep)
    {
        float clampedStep = Mathf.Clamp(scaleStep, 0.02f, 0.3f);
        transform.localScale += new Vector3(clampedStep, clampedStep, 0f);

        CircleCollider2D circleCollider = cachedCollider as CircleCollider2D;
        if (circleCollider != null)
        {
            circleCollider.radius = Mathf.Clamp(circleCollider.radius + clampedStep * 0.05f, 0.5f, 1.6f);
        }

        FighterVisual3D visual3D = GetComponent<FighterVisual3D>();
        if (visual3D != null)
        {
            visual3D.RefreshBaseScale();
            visual3D.PlayImpactPulse(Mathf.Clamp01(clampedStep * 4f));
        }
    }

    private IEnumerator FallIntoWaterRoutine()
    {
        WaterSplashEffect.Spawn(transform.position);

        Vector3 startScale = transform.localScale;
        Vector3 startPosition = transform.position;
        float duration = 0.42f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            transform.position = Vector3.Lerp(startPosition, startPosition + new Vector3(0f, -0.75f, 0f), t);
            transform.localScale = Vector3.Lerp(startScale, startScale * 0.35f, t);

            if (cachedRenderer != null)
            {
                Color color = cachedRenderer.color;
                color.a = Mathf.Lerp(1f, 0f, t);
                cachedRenderer.color = color;
            }

            yield return null;
        }

        gameObject.SetActive(false);
    }
}
