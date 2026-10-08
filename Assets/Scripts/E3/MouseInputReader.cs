using UnityEngine;
using UnityEngine.InputSystem;

public class MouseInputReader : MonoBehaviour
{
    public InputActionReference mousePositionAction;

    public Vector3 MousePosition { get; private set; }

    private void OnEnable()
    {
        mousePositionAction.action.performed += OnMouseMove;
        mousePositionAction.action.Enable();
    }

    private void OnDisable()
    {
        mousePositionAction.action.performed -= OnMouseMove;
        mousePositionAction.action.Disable();
    }

    private void OnMouseMove(InputAction.CallbackContext context)
    {
        Vector2 screenPosition = context.ReadValue<Vector2>();

        MousePosition = Camera.main.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, 0f)
        );

        MousePosition = new Vector3(
            MousePosition.x,
            MousePosition.y,
            0f
        );
    }
}