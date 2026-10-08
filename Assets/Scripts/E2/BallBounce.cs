using UnityEngine;

public class BallBounce : MonoBehaviour
{
    public float bounceSpeed = 8f;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Vector2 bounceDirection = ((Vector2)transform.position - (Vector2)collision.transform.position).normalized;

            rb.linearVelocity = bounceDirection * bounceSpeed;
        }

        else if (collision.gameObject.CompareTag("Wall"))
        {
            Vector2 normal = collision.GetContact(0).normal;

            // Side wall
            if (Mathf.Abs(normal.x) > Mathf.Abs(normal.y))
            {
                Vector2 bounceDirection = new Vector2(-normal.x, Mathf.Sign(rb.linearVelocity.y)).normalized;

                rb.linearVelocity = bounceDirection * bounceSpeed;
            }

            // Top wall
            else
            {
                Vector2 bounceDirection = new Vector2(rb.linearVelocity.x, -Mathf.Sign(rb.linearVelocity.y)).normalized;

                rb.linearVelocity = bounceDirection * bounceSpeed;
            }
        }
    }
}