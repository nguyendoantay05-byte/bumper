using UnityEngine;

/// <summary>
/// Xác định vùng đấu trường.
/// Chỉ cần dùng một hình tròn đơn giản để kiểm tra ai bị đẩy ra ngoài.
/// </summary>
public class ArenaBoundary : MonoBehaviour
{
    [System.Serializable]
    public struct HoleZone
    {
        public Vector2 offset;
        public float radius;

        public HoleZone(Vector2 offset, float radius)
        {
            this.offset = offset;
            this.radius = radius;
        }
    }

    public static ArenaBoundary Instance { get; private set; }

    [Header("Arena")]
    [SerializeField] private Transform centerPoint;
    [SerializeField] private float radius = 8f;
    [SerializeField] private bool useIslandShape = true;
    [SerializeField] private float islandBaseRadius = 12f;
    [SerializeField] private float islandWaveA = 1.6f;
    [SerializeField] private float islandWaveB = 1f;
    [SerializeField] private float islandWaveC = 0.65f;
    [SerializeField] private float fallOutMargin = 0.6f;
    [SerializeField] private float innerHoleRadius = 0f;
    [SerializeField] private Vector2 innerHoleOffset = Vector2.zero;
    [SerializeField] private HoleZone[] extraHoles = new HoleZone[0];
    [SerializeField] private float holeDetectionPadding = 0.55f;
    [SerializeField] private float fullFallCenterMargin = 0.35f;
    [SerializeField] private int minimumSupportedSamples = 2;
    [SerializeField] private Color gizmoColor = new Color(0.2f, 0.9f, 0.9f, 0.6f);

    public float Radius => radius;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public Vector2 Center => centerPoint != null ? (Vector2)centerPoint.position : (Vector2)transform.position;

    public bool IsInside(Vector2 worldPosition)
    {
        return IsInside(worldPosition, 0f);
    }

    public bool IsInside(Vector2 worldPosition, float supportRadius)
    {
        float safeRadius = Mathf.Max(0f, supportRadius);
        float distanceFromCenter = Vector2.Distance(Center, worldPosition);
        bool insideOuter = distanceFromCenter <= GetBoundaryRadiusAt(worldPosition) + fallOutMargin - safeRadius;
        return insideOuter && !IsInsideAnyHole(worldPosition, safeRadius);
    }

    public bool IsNearEdge(Vector2 worldPosition, float edgeDistance)
    {
        return DistanceToEdge(worldPosition) <= edgeDistance;
    }

    public float DistanceToEdge(Vector2 worldPosition)
    {
        return DistanceToEdge(worldPosition, 0f);
    }

    public float DistanceToEdge(Vector2 worldPosition, float supportRadius)
    {
        float safeRadius = Mathf.Max(0f, supportRadius);
        float distanceFromCenter = Vector2.Distance(Center, worldPosition);
        float distanceToOuterEdge = GetBoundaryRadiusAt(worldPosition) + fallOutMargin - safeRadius - distanceFromCenter;
        float distanceToHoleEdge = GetDistanceToNearestHoleEdge(worldPosition, safeRadius);
        return Mathf.Min(distanceToOuterEdge, distanceToHoleEdge);
    }

    public Vector2 DirectionToCenter(Vector2 worldPosition)
    {
        Vector2 direction = Center - worldPosition;
        if (direction.sqrMagnitude < 0.0001f)
        {
            return Vector2.up;
        }

        return direction.normalized;
    }

    public Vector2 GetRecoveryDirection(Vector2 worldPosition)
    {
        Vector2 toSafety = DirectionToCenter(worldPosition);
        Vector2 holeEscape = GetHoleEscapeDirection(worldPosition, out float holeRisk);

        if (holeRisk <= 0f || holeEscape.sqrMagnitude <= 0.0001f)
        {
            return toSafety;
        }

        Vector2 blendedDirection = holeEscape * (1.1f + holeRisk * 1.9f) + toSafety * 0.45f;
        if (blendedDirection.sqrMagnitude <= 0.0001f)
        {
            return holeEscape;
        }

        return blendedDirection.normalized;
    }

    public bool HasGroundSupport(FighterController fighter)
    {
        if (fighter == null)
        {
            return false;
        }

        float sampleRadius = fighter.BoundarySampleRadius * 0.82f;
        float centerDistanceToEdge = DistanceToEdge(fighter.Position);
        float fullFallThreshold = -Mathf.Max(fullFallCenterMargin, fighter.BoundarySampleRadius * 0.42f);

        // Chỉ tính thua khi tâm nhân vật đã vượt khỏi mép một đoạn rõ ràng.
        if (centerDistanceToEdge > fullFallThreshold)
        {
            return true;
        }

        if (sampleRadius <= 0.08f)
        {
            return false;
        }

        int supportedSamples = 0;
        const int sampleCount = 8;

        for (int i = 0; i < sampleCount; i++)
        {
            float angle = (Mathf.PI * 2f / sampleCount) * i;
            Vector2 probe = fighter.Position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * sampleRadius;
            if (IsInside(probe))
            {
                supportedSamples++;
            }
        }

        return supportedSamples >= Mathf.Clamp(minimumSupportedSamples, 1, sampleCount);
    }

    public void ConfigureIslandShape(
        float newRadius,
        float newWaveA,
        float newWaveB,
        float newWaveC,
        float newFallOutMargin,
        float newInnerHoleRadius = 0f,
        Vector2? newInnerHoleOffset = null,
        HoleZone[] newExtraHoles = null)
    {
        radius = newRadius;
        useIslandShape = true;
        islandBaseRadius = newRadius;
        islandWaveA = newWaveA;
        islandWaveB = newWaveB;
        islandWaveC = newWaveC;
        fallOutMargin = newFallOutMargin;
        innerHoleRadius = Mathf.Max(0f, newInnerHoleRadius);
        innerHoleOffset = newInnerHoleOffset ?? Vector2.zero;
        extraHoles = newExtraHoles ?? new HoleZone[0];
    }

    public Vector2 GetInnerHoleWorldCenter()
    {
        return Center + innerHoleOffset;
    }

    public HoleZone[] GetExtraHoles()
    {
        return extraHoles ?? new HoleZone[0];
    }

    private float GetBoundaryRadiusAt(Vector2 worldPosition)
    {
        if (!useIslandShape)
        {
            return radius;
        }

        Vector2 offset = worldPosition - Center;
        if (offset.sqrMagnitude < 0.0001f)
        {
            return islandBaseRadius;
        }

        float angle = Mathf.Atan2(offset.y, offset.x);
        float wobble = Mathf.Sin(angle * 3f) * islandWaveA
            + Mathf.Cos(angle * 5f) * islandWaveB
            + Mathf.Sin(angle * 7f) * islandWaveC;

        return islandBaseRadius + wobble;
    }

    private bool IsInsideAnyHole(Vector2 worldPosition, float supportRadius)
    {
        if (innerHoleRadius > 0f)
        {
            float distanceFromPrimaryHole = Vector2.Distance(GetInnerHoleWorldCenter(), worldPosition);
            if (distanceFromPrimaryHole <= innerHoleRadius + holeDetectionPadding + supportRadius)
            {
                return true;
            }
        }

        if (extraHoles == null)
        {
            return false;
        }

        for (int i = 0; i < extraHoles.Length; i++)
        {
            HoleZone hole = extraHoles[i];
            if (hole.radius <= 0f)
            {
                continue;
            }

            if (Vector2.Distance(Center + hole.offset, worldPosition) <= hole.radius + holeDetectionPadding + supportRadius)
            {
                return true;
            }
        }

        return false;
    }

    private float GetDistanceToNearestHoleEdge(Vector2 worldPosition, float supportRadius)
    {
        float nearestDistance = float.MaxValue;

        if (innerHoleRadius > 0f)
        {
            float distance = Vector2.Distance(GetInnerHoleWorldCenter(), worldPosition) - (innerHoleRadius + holeDetectionPadding + supportRadius);
            nearestDistance = Mathf.Min(nearestDistance, distance);
        }

        if (extraHoles != null)
        {
            for (int i = 0; i < extraHoles.Length; i++)
            {
                HoleZone hole = extraHoles[i];
                if (hole.radius <= 0f)
                {
                    continue;
                }

                float distance = Vector2.Distance(Center + hole.offset, worldPosition) - (hole.radius + holeDetectionPadding + supportRadius);
                nearestDistance = Mathf.Min(nearestDistance, distance);
            }
        }

        return nearestDistance == float.MaxValue ? float.MaxValue : nearestDistance;
    }

    private Vector2 GetHoleEscapeDirection(Vector2 worldPosition, out float risk)
    {
        risk = 0f;
        Vector2 bestDirection = Vector2.zero;
        float bestRisk = 0f;

        EvaluateHoleRisk(worldPosition, GetInnerHoleWorldCenter(), innerHoleRadius, ref bestDirection, ref bestRisk);

        if (extraHoles != null)
        {
            for (int i = 0; i < extraHoles.Length; i++)
            {
                EvaluateHoleRisk(worldPosition, Center + extraHoles[i].offset, extraHoles[i].radius, ref bestDirection, ref bestRisk);
            }
        }

        risk = bestRisk;
        return bestDirection;
    }

    private void EvaluateHoleRisk(Vector2 worldPosition, Vector2 holeCenter, float holeRadius, ref Vector2 bestDirection, ref float bestRisk)
    {
        if (holeRadius <= 0f)
        {
            return;
        }

        Vector2 away = worldPosition - holeCenter;
        float distanceFromHole = away.magnitude;
        float dangerZone = holeRadius + holeDetectionPadding + 2.4f;
        float holeRisk = 1f - Mathf.Clamp01((distanceFromHole - holeRadius) / Mathf.Max(0.001f, dangerZone - holeRadius));

        if (holeRisk <= bestRisk)
        {
            return;
        }

        bestRisk = holeRisk;
        bestDirection = away.sqrMagnitude > 0.0001f ? away.normalized : DirectionToCenter(worldPosition);
    }

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsMatchRunning)
        {
            return;
        }

        if (GameManager.Instance.IsSpawnProtectionActive)
        {
            return;
        }

        var fighters = GameManager.Instance.GetAllActiveFighters();
        for (int i = fighters.Count - 1; i >= 0; i--)
        {
            FighterController fighter = fighters[i];
            if (fighter == null || fighter.IsEliminated)
            {
                continue;
            }

            if (!HasGroundSupport(fighter))
            {
                GameManager.Instance.EliminateFighter(fighter);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = gizmoColor;
        Vector3 c = centerPoint != null ? centerPoint.position : transform.position;
        Gizmos.DrawWireSphere(c, useIslandShape ? islandBaseRadius : radius);
        if (innerHoleRadius > 0f)
        {
            Gizmos.DrawWireSphere(c + (Vector3)innerHoleOffset, innerHoleRadius);
        }

        if (extraHoles != null)
        {
            for (int i = 0; i < extraHoles.Length; i++)
            {
                if (extraHoles[i].radius > 0f)
                {
                    Gizmos.DrawWireSphere(c + (Vector3)extraHoles[i].offset, extraHoles[i].radius);
                }
            }
        }
    }
}
