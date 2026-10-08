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

    private void Awake()
    {
        BindActions();
    }

    private void BindActions()
    {
        InputActionMap movementMap = inputActions.FindActionMap("Pong");

        if (controlScheme == ControlScheme.WS)
     {
            upAction = movementMap.FindAction("WSUp");
            downAction = movementMap.FindAction("WSDown");
        }
        else
        {
            upAction = movementMap.FindAction("ArrowUp");
            downAction = movementMap.FindAction("ArrowDown");
        }
    }

    private void OnEnable()
    {
        if (upAction != null)
        {
            upAction.Enable();
        }

        if (downAction != null)
        {
            downAction.Enable();
        }
    }

    private void OnDisable()
    {
        if (upAction != null)
        {
            upAction.Disable();
        }

        if (downAction != null)
        {
            downAction.Disable();
        }
    }

    private void Update()
    {
        float moveInput = 0f;

        if (upAction.IsPressed())
        {
            moveInput += 1f;
        }

        if (downAction.IsPressed())
        {
            moveInput -= 1f;
        }

        float effectiveBallSpeed = ball.currentBaseSpeed + ball.temporarySpeedBonus;
        
        float speedRatio = Mathf.InverseLerp(settings.baseBallSpeed, settings.ballSpeedForMaxPaddleSpeed, effectiveBallSpeed);

        float moveSpeed = Mathf.Lerp(settings.basePaddleSpeed, settings.maxPaddleSpeed, speedRatio);

        transform.Translate(Vector3.up * moveInput * moveSpeed * Time.deltaTime);
        
        Vector3 position = transform.position;

        position.y = Mathf.Clamp(position.y, settings.paddleMinY, settings.paddleMaxY);

        transform.position = position;
    }

    public void SetControlScheme(ControlScheme newScheme)
    {
        if (upAction != null)
        {
            upAction.Disable();
        }

        if (downAction != null)
        {
            downAction.Disable();
        }

        controlScheme = newScheme;

        BindActions();

        if (isActiveAndEnabled)
        {
            upAction.Enable();
            downAction.Enable();
        }
    }
}