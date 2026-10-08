using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float Speed = 5f;
    public Vector2 direction;
    public Vector2 velocity;

    public Vector2 position;

    public Rigidbody2D rb;

    public InputActionReference moveAction;

    public void FixedUpdate()
    {
        velocity = direction * Speed;
        position = rb.position + velocity * Time.fixedDeltaTime;
        rb.MovePosition(position);
    }
    void OnEnable()
    {
        moveAction.action.performed += OnMove;
        moveAction.action.canceled += OnMove;
        moveAction.action.Enable();
    }

    private void OnDisable()
    {
        moveAction.action.performed -= OnMove;
        moveAction.action.canceled -= OnMove;
        moveAction.action.Disable();
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        direction = context.ReadValue<Vector2>().normalized;
    }
    
}
