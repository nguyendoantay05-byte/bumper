using UnityEngine;

/// <summary>
/// Camera follow mượt cho GameScene.
/// Giữ người chơi hơi lệch xuống dưới để thấy nhiều không gian phía trước.
/// </summary>
public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Vector3 followOffset = new Vector3(0f, 1.6f, -10f);
    [SerializeField] private float smoothTime = 0.18f;
    [SerializeField] private Vector2 lookAheadScale = new Vector2(0.65f, 0.45f);

    private Transform target;
    private FighterController fighterTarget;
    private Vector3 velocity;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        fighterTarget = newTarget != null ? newTarget.GetComponent<FighterController>() : null;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            TryFindTarget();
        }

        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = target.position + followOffset;
        if (fighterTarget != null && fighterTarget.Body != null)
        {
            Vector2 velocityLookAhead = fighterTarget.Body.linearVelocity;
            desiredPosition += new Vector3(velocityLookAhead.x * lookAheadScale.x, velocityLookAhead.y * lookAheadScale.y, 0f);
        }

        desiredPosition.z = followOffset.z;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
    }

    private void TryFindTarget()
    {
        if (GameManager.Instance != null && GameManager.Instance.PlayerInstance != null)
        {
            SetTarget(GameManager.Instance.PlayerInstance.transform);
        }
    }
}
