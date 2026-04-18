using UnityEngine;

/// <summary>
/// AI bot đơn giản: tìm mục tiêu gần nhất, đuổi theo và tránh mép sân.
/// </summary>
public class BotController : FighterController
{
    [Header("AI")]
    [SerializeField] private float targetRefreshInterval = 0.01f;
    [SerializeField] private float attackRange = 3.5f;
    [SerializeField] private float edgeAvoidDistance = 2.5f;
    [SerializeField] private float edgeAvoidWeight = 1.8f;
    [SerializeField] private float chaseWeight = 1.2f;
    [SerializeField] private float edgePunishWeight = 2.2f;
    [SerializeField] private float playerFocusWeight = 0.35f;
    [SerializeField] private float distanceWeight = 0.22f;
    [SerializeField] private float targetRandomness = 1.15f;
    [SerializeField] private float retargetIfNoPressureTime = 3f;
    [SerializeField] private float randomRetargetIntervalMin = 1.5f;
    [SerializeField] private float randomRetargetIntervalMax = 3.1f;
    [SerializeField] private float allySeparationDistance = 3.2f;
    [SerializeField] private float allySeparationWeight = 1.75f;
    [SerializeField] private float crowdPenaltyRadius = 4.6f;
    [SerializeField] private float crowdPenaltyWeight = 2.6f;
    [SerializeField] private float botMoveForceMultiplier = 0.88f;
    [SerializeField] private float botMaxSpeedMultiplier = 0.84f;
    [SerializeField] private float targetDiversityWeight = 1.9f;

    private FighterController currentTarget;
    private float refreshTimer;
    private float targetLockTimer;
    private float randomRetargetTimer;

    protected override void Awake()
    {
        base.Awake();
        SetPlayerControlled(false);
        moveForce *= botMoveForceMultiplier;
        maxSpeed *= botMaxSpeedMultiplier;
        randomRetargetTimer = Random.Range(randomRetargetIntervalMin, randomRetargetIntervalMax);
    }

    protected override void HandleMovement()
    {
        if (gameManager == null)
        {
            return;
        }

        targetLockTimer += Time.fixedDeltaTime;
        randomRetargetTimer -= Time.fixedDeltaTime;

        refreshTimer -= Time.fixedDeltaTime;
        if (refreshTimer <= 0f || currentTarget == null || currentTarget.IsEliminated)
        {
            refreshTimer = targetRefreshInterval;
            currentTarget = SelectBestTarget();
            targetLockTimer = 0f;
        }

        Vector2 moveDirection = Vector2.zero;

        if (currentTarget != null && !currentTarget.IsEliminated)
        {
            Vector2 toTarget = currentTarget.Position - Position;
            float distance = toTarget.magnitude;

            if (distance > 0.01f)
            {
                float weight = chaseWeight;
                if (distance <= attackRange)
                {
                    weight += 0.8f;
                }

                if (arenaBoundary != null && arenaBoundary.IsNearEdge(currentTarget.Position, edgeAvoidDistance))
                {
                    weight += 0.8f;
                }

                moveDirection += toTarget.normalized * weight;

                if (distance <= attackRange * 1.15f)
                {
                    targetLockTimer = 0f;
                }
            }
        }

        if (targetLockTimer >= retargetIfNoPressureTime || randomRetargetTimer <= 0f)
        {
            currentTarget = SelectBestTarget(currentTarget);
            targetLockTimer = 0f;
            randomRetargetTimer = Random.Range(randomRetargetIntervalMin, randomRetargetIntervalMax);
        }

        if (arenaBoundary != null)
        {
            Vector2 toCenter = arenaBoundary.DirectionToCenter(Position);
            float distanceToEdge = arenaBoundary.DistanceToEdge(Position);

            if (distanceToEdge <= edgeAvoidDistance)
            {
                // Khi đứng quá sát rìa, bot sẽ ưu tiên quay về trung tâm.
                moveDirection += toCenter.normalized * edgeAvoidWeight;
            }
        }

        moveDirection += CalculateSeparationVector();
        MoveInDirection(moveDirection);
    }

    private FighterController SelectBestTarget(FighterController avoidTarget = null)
    {
        if (gameManager == null)
        {
            return null;
        }

        var fighters = gameManager.GetAllActiveFighters();
        FighterController bestTarget = null;
        float bestScore = float.MinValue;

        for (int i = 0; i < fighters.Count; i++)
        {
            FighterController candidate = fighters[i];
            if (candidate == null || candidate == this || candidate.IsEliminated)
            {
                continue;
            }

            if (avoidTarget != null && candidate == avoidTarget)
            {
                continue;
            }

            float score = EvaluateTargetScore(candidate);

            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = candidate;
            }
        }

        if (bestTarget == null && avoidTarget != null)
        {
            return SelectBestTarget();
        }

        return bestTarget;
    }

    private float EvaluateTargetScore(FighterController candidate)
    {
        if (candidate == null || candidate == this || candidate.IsEliminated)
        {
            return float.MinValue;
        }

        float distance = Vector2.Distance(Position, candidate.Position);
        float score = -distance * distanceWeight;

        score += candidate.IsPlayerControlled ? playerFocusWeight : 0.7f;

        if (arenaBoundary != null)
        {
            float edgePressure = Mathf.Clamp01((edgeAvoidDistance + 1.5f - arenaBoundary.DistanceToEdge(candidate.Position)) / (edgeAvoidDistance + 1.5f));
            score += edgePressure * (edgePunishWeight + 4.4f);
        }

        Vector2 approachDirection = (candidate.Position - Position).normalized;
        if (candidate.Body != null)
        {
            float targetMomentumToEdge = Vector2.Dot(candidate.Body.linearVelocity, approachDirection);
            score += Mathf.Clamp(targetMomentumToEdge, -1f, 2.5f) * 0.8f;
        }

        if (candidate != currentTarget)
        {
            score += Random.Range(0f, targetRandomness);
        }

        int nearbyBotCount = CountBotsNearTarget(candidate);
        score -= nearbyBotCount * crowdPenaltyWeight;
        score += Mathf.Clamp(2 - nearbyBotCount, 0, 2) * targetDiversityWeight;
        return score;
    }

    private Vector2 CalculateSeparationVector()
    {
        if (gameManager == null)
        {
            return Vector2.zero;
        }

        var fighters = gameManager.GetAllActiveFighters();
        Vector2 separation = Vector2.zero;

        for (int i = 0; i < fighters.Count; i++)
        {
            FighterController fighter = fighters[i];
            if (fighter == null || fighter == this || fighter.IsEliminated)
            {
                continue;
            }

            Vector2 away = Position - fighter.Position;
            float distance = away.magnitude;
            if (distance <= 0.001f || distance > allySeparationDistance)
            {
                continue;
            }

            float weight = 1f - Mathf.Clamp01(distance / allySeparationDistance);
            if (fighter is BotController)
            {
                weight *= 1.2f;
            }

            separation += away.normalized * weight;
        }

        return separation * allySeparationWeight;
    }

    private int CountBotsNearTarget(FighterController candidate)
    {
        if (gameManager == null || candidate == null)
        {
            return 0;
        }

        int count = 0;
        var fighters = gameManager.GetAllActiveFighters();
        for (int i = 0; i < fighters.Count; i++)
        {
            FighterController fighter = fighters[i];
            if (fighter == null || fighter == this || fighter == candidate || fighter.IsEliminated)
            {
                continue;
            }

            if (fighter is not BotController)
            {
                continue;
            }

            if (Vector2.Distance(fighter.Position, candidate.Position) <= crowdPenaltyRadius)
            {
                count++;
            }
        }

        return count;
    }
}
