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
        float distanceFromCenter = Vector2.Distance(Center, worldPosition);
        bool insideOuter = distanceFromCenter <= GetBoundaryRadiusAt(worldPosition) + fallOutMargin;
        return insideOuter && !IsInsideAnyHole(worldPosition);
    }

    public bool IsNearEdge(Vector2 worldPosition, float edgeDistance)
    {
        return DistanceToEdge(worldPosition) <= edgeDistance;
    }

    public float DistanceToEdge(Vector2 worldPosition)
    {
        float distanceFromCenter = Vector2.Distance(Center, worldPosition);
        float distanceToOuterEdge = GetBoundaryRadiusAt(worldPosition) - distanceFromCenter;
        float distanceToHoleEdge = GetDistanceToNearestHoleEdge(worldPosition);
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

    private bool IsInsideAnyHole(Vector2 worldPosition)
    {
        if (innerHoleRadius > 0f)
        {
            float distanceFromPrimaryHole = Vector2.Distance(GetInnerHoleWorldCenter(), worldPosition);
            if (distanceFromPrimaryHole <= innerHoleRadius)
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

            if (Vector2.Distance(Center + hole.offset, worldPosition) <= hole.radius)
            {
                return true;
            }
        }

        return false;
    }

    private float GetDistanceToNearestHoleEdge(Vector2 worldPosition)
    {
        float nearestDistance = float.MaxValue;

        if (innerHoleRadius > 0f)
        {
            float distance = Vector2.Distance(GetInnerHoleWorldCenter(), worldPosition) - innerHoleRadius;
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

                float distance = Vector2.Distance(Center + hole.offset, worldPosition) - hole.radius;
                nearestDistance = Mathf.Min(nearestDistance, distance);
            }
        }

        return nearestDistance == float.MaxValue ? float.MaxValue : nearestDistance;
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

            if (!IsInside(fighter.Position))
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
