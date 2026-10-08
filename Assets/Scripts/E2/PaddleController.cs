using UnityEngine;

public class PaddleController : MonoBehaviour
{
    public float minX = -8f;
    public float maxX = 8f;

    private Rigidbody2D rb;
    private float targetX;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        targetX = Mathf.Clamp(mousePosition.x, minX, maxX);
    }

    void FixedUpdate()
    {
        rb.MovePosition(
            new Vector2(
                targetX,
                rb.position.y
            )
        );
    }
}