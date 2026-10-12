using UnityEngine;
using UnityEngine.InputSystem;

public enum ControlScheme
{
    WS,
    Arrows
}

public enum PaddleSide
{
    Left,
    Right
}

public class PongPaddleController : MonoBehaviour
{
    public ControlScheme controlScheme;
    public InputActionAsset inputActions;
    public PongSettings settings;
    public PongBallController ball;

    private InputAction upAction;
    private InputAction downAction;
    private Vector3 startPosition;
    private bool initialized;
    private bool inputLocked;

    // The scene's initial X is the back limit, independent of who controls this side.
    public float ForwardLimitX
    {
        get
        {
            CaptureStartPosition();
            float centerX = ball != null ? ball.ServePosition.x : 0f;
            float towardCenter = startPosition.x < centerX ? 1f : -1f;
            float distance = settings != null ? Mathf.Max(0f, settings.paddleForwardDistance) : 0f;
            return startPosition.x + towardCenter * distance;
        }
    }

    private void Awake()
    {
        CaptureStartPosition();
        if (upAction == null && downAction == null)
            BindActions();
    }

    private void CaptureStartPosition()
    {
        if (initialized)
            return;
        startPosition = transform.position;
        initialized = true;
    }

    private void BindActions()
    {
        InputActionMap map = inputActions != null ? inputActions.FindActionMap("Pong") : null;
        if (map == null)
            return;

        // Each paddle owns its copies. Swapping sides must not disable the other paddle's actions.
        upAction = map.FindAction(controlScheme == ControlScheme.WS ? "WSUp" : "ArrowUp")?.Clone();
        downAction = map.FindAction(controlScheme == ControlScheme.WS ? "WSDown" : "ArrowDown")?.Clone();
    }

    private void OnEnable()
    {
        upAction?.Enable();
        downAction?.Enable();
    }

    private void OnDisable()
    {
        upAction?.Disable();
        downAction?.Disable();
    }

    private void OnDestroy()
    {
        upAction?.Dispose();
        downAction?.Dispose();
    }

    private void Update()
    {
        if (inputLocked || Time.timeScale == 0f || settings == null || ball == null)
            return;

        float vertical = (upAction != null && upAction.IsPressed() ? 1f : 0f) -
            (downAction != null && downAction.IsPressed() ? 1f : 0f);
        float horizontal = 0f;
        Keyboard keys = Keyboard.current;
        if (keys != null)
        {
            horizontal = controlScheme == ControlScheme.WS
                ? (keys.dKey.isPressed ? 1f : 0f) - (keys.aKey.isPressed ? 1f : 0f)
                : (keys.rightArrowKey.isPressed ? 1f : 0f) - (keys.leftArrowKey.isPressed ? 1f : 0f);
        }

        float ballSpeed = ball.currentBaseSpeed + ball.temporarySpeedBonus;
        float ratio = Mathf.InverseLerp(settings.baseBallSpeed, settings.ballSpeedForMaxPaddleSpeed, ballSpeed);
        float moveSpeed = Mathf.Lerp(settings.basePaddleSpeed, settings.maxPaddleSpeed, ratio);
        Vector2 input = Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);
        Vector3 position = transform.position + (Vector3)input * moveSpeed * Time.deltaTime;
        position.x = Mathf.Clamp(position.x, Mathf.Min(startPosition.x, ForwardLimitX),
            Mathf.Max(startPosition.x, ForwardLimitX));
        position.y = Mathf.Clamp(position.y, settings.paddleMinY, settings.paddleMaxY);
        transform.position = position;
    }

    public void SetInputLocked(bool locked) => inputLocked = locked;

    public void ResetToStartPosition()
    {
        CaptureStartPosition();
        transform.position = startPosition;
    }

    public void SetControlScheme(ControlScheme newScheme)
    {
        upAction?.Disable();
        downAction?.Disable();
        upAction?.Dispose();
        downAction?.Dispose();
        upAction = null;
        downAction = null;
        controlScheme = newScheme;
        BindActions();
        if (isActiveAndEnabled)
            OnEnable();
    }
}
