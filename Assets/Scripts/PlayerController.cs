using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Điều khiển nhân vật người chơi bằng WASD hoặc phím mũi tên.
/// </summary>
public class PlayerController : FighterController
{
    [Header("Player Input")]
    [SerializeField] private bool useLegacyInput = true;
    [SerializeField] private float keyboardAccelerationTime = 0.35f;
    [SerializeField] private float keyboardMinStrength = 0.8f;
    [SerializeField] private float keyboardMaxStrength = 5.2f;
    [SerializeField] private float mouseMinStrength = 0.38f;
    [SerializeField] private float mouseMaxStrength = 5.6f;
    [SerializeField] private float maxMouseDragDistance = 220f;
    [SerializeField] private float keyboardSpeedCapBonus = 12.5f;
    [SerializeField] private float mouseSpeedCapBonus = 13.5f;

    private Vector2 lastKeyboardDirection;
    private float keyboardHoldTimer;
    private Vector2 dragStartScreenPosition;
    private bool draggingMouse;

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
            return;
        }

        Vector2 mouseDirection = ReadMouseDragDirection(out float mouseStrength);
        if (mouseDirection.sqrMagnitude > 0.0001f)
        {
            float mouseT = Mathf.InverseLerp(mouseMinStrength, mouseMaxStrength, mouseStrength);
            SetInputSpeedCapBonus(Mathf.Lerp(0.15f, mouseSpeedCapBonus, mouseT));
            MoveInDirection(mouseDirection, mouseStrength);
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

    private Vector2 ReadMouseDragDirection(out float strength)
    {
        strength = 0f;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject() && Input.GetMouseButtonDown(0))
        {
            draggingMouse = false;
            return Vector2.zero;
        }

        if (Input.GetMouseButtonDown(0))
        {
            dragStartScreenPosition = Input.mousePosition;
            draggingMouse = true;
        }

        if (Input.GetMouseButtonUp(0))
        {
            draggingMouse = false;
        }

        if (!draggingMouse || Camera.main == null)
        {
            return Vector2.zero;
        }

        Vector2 dragDelta = (Vector2)Input.mousePosition - dragStartScreenPosition;
        if (dragDelta.sqrMagnitude < 16f)
        {
            return Vector2.zero;
        }

        strength = Mathf.Lerp(mouseMinStrength, mouseMaxStrength, Mathf.Clamp01(dragDelta.magnitude / maxMouseDragDistance));

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Mathf.Abs(Camera.main.transform.position.z)));
        Vector2 direction = (mouseWorld - transform.position);
        if (direction.sqrMagnitude > 1f)
        {
            direction.Normalize();
        }

        return direction;
    }
}
