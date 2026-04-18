using UnityEngine;

/// <summary>
/// Tạo cảm giác 3D giả cho fighter bằng cách nghiêng nhẹ các lớp visual theo vận tốc.
/// </summary>
public class FighterVisual3D : MonoBehaviour
{
    [SerializeField] private float tiltAmount = 7f;
    [SerializeField] private float offsetAmount = 0.08f;

    private FighterController fighter;
    private bool showArrow;
    private Vector3 topCapBase;
    private Vector3 glossBase;
    private Vector3 rimGlowBase;
    private Vector3 rightBumperBase;
    private Vector3 leftBumperBase;
    private Vector3 rightBumperShadowBase;
    private Vector3 leftBumperShadowBase;
    private Vector3 softShadowBase;
    private Vector3 shadowBase;
    private Vector3 arrowBase;
    private Vector3 baseScale;
    private Vector2 lastVelocity;
    private float impactPulse;

    public void Configure(bool isPlayer)
    {
        showArrow = isPlayer;
    }

    private void Awake()
    {
        fighter = GetComponent<FighterController>();
        topCapBase = GetLocalPosition("TopCap");
        glossBase = GetLocalPosition("Gloss");
        rimGlowBase = GetLocalPosition("RimGlow");
        rightBumperBase = GetLocalPosition("RightBumper");
        leftBumperBase = GetLocalPosition("LeftBumper");
        rightBumperShadowBase = GetLocalPosition("RightBumperShadow");
        leftBumperShadowBase = GetLocalPosition("LeftBumperShadow");
        softShadowBase = GetLocalPosition("SoftShadow");
        shadowBase = GetLocalPosition("Shadow");
        arrowBase = GetLocalPosition("PlayerArrow");
        baseScale = transform.localScale;
    }

    private void LateUpdate()
    {
        if (fighter == null || fighter.Body == null)
        {
            return;
        }

        Vector2 velocity = fighter.Body.linearVelocity;
        Vector2 normalized = velocity.sqrMagnitude > 0.001f ? velocity.normalized : Vector2.zero;
        Vector2 acceleration = (velocity - lastVelocity) / Mathf.Max(Time.deltaTime, 0.0001f);
        float accelerationAmount = Mathf.Clamp01(acceleration.magnitude / 22f);
        lastVelocity = velocity;

        ApplyLayerMotion("TopCap", topCapBase, normalized, 0.12f);
        ApplyLayerMotion("Gloss", glossBase, normalized, 0.16f);
        ApplyLayerMotion("RimGlow", rimGlowBase, normalized, 0.1f);
        ApplyLayerMotion("RightBumper", rightBumperBase, normalized, 0.08f);
        ApplyLayerMotion("LeftBumper", leftBumperBase, normalized, 0.05f);
        ApplyLayerMotion("RightBumperShadow", rightBumperShadowBase, normalized, 0.04f);
        ApplyLayerMotion("LeftBumperShadow", leftBumperShadowBase, normalized, 0.03f);
        ApplyShadowMotion(normalized);
        PulseHighlight();
        ApplyBodyLean(normalized, accelerationAmount);
        ApplyArrow(normalized);
    }

    private void ApplyLayerMotion(string childName, Vector3 basePosition, Vector2 direction, float strength)
    {
        Transform child = transform.Find(childName);
        if (child == null)
        {
            return;
        }

        float xOffset = direction.x * offsetAmount * strength;
        float yOffset = direction.y * offsetAmount * strength;
        child.localPosition = new Vector3(basePosition.x + xOffset, basePosition.y + yOffset, basePosition.z);
        child.localRotation = Quaternion.Euler(0f, 0f, -direction.x * tiltAmount * strength);
    }

    private void ApplyShadowMotion(Vector2 direction)
    {
        Transform softShadow = transform.Find("SoftShadow");
        Transform shadow = transform.Find("Shadow");
        if (shadow == null)
        {
            return;
        }

        if (softShadow != null)
        {
            softShadow.localPosition = new Vector3(softShadowBase.x + direction.x * 0.08f, softShadowBase.y - direction.y * 0.04f, softShadowBase.z);
        }

        shadow.localPosition = new Vector3(shadowBase.x + direction.x * 0.06f, shadowBase.y - direction.y * 0.03f, shadowBase.z);
    }

    private void PulseHighlight()
    {
        impactPulse = Mathf.MoveTowards(impactPulse, 0f, Time.deltaTime * 4.5f);
        float speedPulse = fighter != null && fighter.Body != null
            ? Mathf.Clamp01(fighter.Body.linearVelocity.magnitude / 11f)
            : 0f;

        Transform gloss = transform.Find("Gloss");
        if (gloss != null)
        {
            SpriteRenderer renderer = gloss.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                Color color = renderer.color;
                color.a = 0.22f + Mathf.Sin(Time.time * 4.2f) * 0.05f + impactPulse * 0.18f + speedPulse * 0.12f;
                renderer.color = color;
            }
        }

        Transform rimGlow = transform.Find("RimGlow");
        if (rimGlow != null)
        {
            SpriteRenderer renderer = rimGlow.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                Color color = renderer.color;
                color.a = 0.08f + Mathf.Sin(Time.time * 3f) * 0.03f + impactPulse * 0.08f + speedPulse * 0.18f;
                renderer.color = color;
            }
        }
    }

    private void ApplyBodyLean(Vector2 direction, float accelerationAmount)
    {
        float speedAmount = fighter != null && fighter.Body != null
            ? Mathf.Clamp01(fighter.Body.linearVelocity.magnitude / 12f)
            : 0f;
        float squash = 1f + accelerationAmount * 0.14f + speedAmount * 0.1f + impactPulse * 0.1f;
        float stretch = 1f - accelerationAmount * 0.11f - speedAmount * 0.08f - impactPulse * 0.08f;
        transform.localScale = new Vector3(baseScale.x * squash, baseScale.y * stretch, baseScale.z);
    }

    private void ApplyArrow(Vector2 direction)
    {
        Transform arrow = transform.Find("PlayerArrow");
        if (arrow == null)
        {
            return;
        }

        SpriteRenderer renderer = arrow.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.enabled = showArrow;
        }

        if (!showArrow)
        {
            return;
        }

        Vector2 facing = direction.sqrMagnitude > 0.01f ? direction : Vector2.left;
        float angle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
        arrow.localRotation = Quaternion.Euler(0f, 0f, angle);
        arrow.localPosition = new Vector3(arrowBase.x - facing.x * 0.18f, arrowBase.y - facing.y * 0.18f, arrowBase.z);
    }

    private Vector3 GetLocalPosition(string childName)
    {
        Transform child = transform.Find(childName);
        return child != null ? child.localPosition : Vector3.zero;
    }

    public void PlayImpactPulse(float intensity)
    {
        impactPulse = Mathf.Clamp(impactPulse + intensity, 0f, 1f);
    }

    public void RefreshBaseScale()
    {
        baseScale = transform.localScale;
    }
}
