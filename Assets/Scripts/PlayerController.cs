using UnityEngine;

/// <summary>
/// Điều khiển nhân vật người chơi bằng WASD.
/// </summary>
public class PlayerController : FighterController
{
    [Header("Player Input")]
    [SerializeField] private bool useLegacyInput = true;
    [SerializeField] private float keyboardAccelerationTime = 0.35f;
    [SerializeField] private float keyboardMinStrength = 0.7f;
    [SerializeField] private float keyboardMaxStrength = 4.35f;
    [SerializeField] private float keyboardSpeedCapBonus = 9.6f;

    private Vector2 lastKeyboardDirection;
    private float keyboardHoldTimer;

    protected override void Awake()
    {
        base.Awake();
        SetPlayerControlled(true);
    }

    protected override void HandleMovement()
    {
        Vector2 keyboardInput = ReadKeyboardInput();
        if (keyboardInput.sqrMagnitude > 0.0001f)
        {
            float keyboardStrength = ReadKeyboardStrength(keyboardInput);
            float keyboardT = Mathf.InverseLerp(keyboardMinStrength, keyboardMaxStrength, keyboardStrength);
            SetInputSpeedCapBonus(Mathf.Lerp(0.25f, keyboardSpeedCapBonus, keyboardT));
            MoveInDirection(keyboardInput, keyboardStrength);
        }
    }

    private Vector2 ReadKeyboardInput()
    {
        if (!useLegacyInput)
        {
            return Vector2.zero;
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector2 input = new Vector2(horizontal, vertical);

        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }

        return input;
    }

    private float ReadKeyboardStrength(Vector2 keyboardInput)
    {
        Vector2 normalizedInput = keyboardInput.normalized;
        if (Vector2.Dot(lastKeyboardDirection, normalizedInput) > 0.96f)
        {
            keyboardHoldTimer += Time.fixedDeltaTime;
        }
        else
        {
            keyboardHoldTimer = 0f;
        }

        lastKeyboardDirection = normalizedInput;
        float t = Mathf.Clamp01(keyboardHoldTimer / keyboardAccelerationTime);
        return Mathf.Lerp(keyboardMinStrength, keyboardMaxStrength, t);
    }
}
