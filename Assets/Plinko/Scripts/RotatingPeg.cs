using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class RotatingPeg : MonoBehaviour
{
    [Tooltip("Degrees per second. Use a negative value to reverse direction.")]
    public float rotationSpeed = 90f;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        rb.MoveRotation(rb.rotation + rotationSpeed * Time.fixedDeltaTime);
    }
}
