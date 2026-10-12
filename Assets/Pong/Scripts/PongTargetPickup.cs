using UnityEngine;

public enum TargetType
{
    Freeze,
    Fire,
    Bomb,
    Mirage,
    Portal
}

public class PongTargetPickup : MonoBehaviour
{
    public TargetType targetType;

    private SpriteRenderer spriteRenderer;

    private PongTargetManager targetManager;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Initialize(TargetType type, PongTargetManager manager)
    {
        targetType = type;
        targetManager = manager;

    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PongBallController ball = null;

        if (other.attachedRigidbody != null)
        {
            ball = other.attachedRigidbody.GetComponent<PongBallController>();
        }

        if (ball == null)
        {
            ball = other.GetComponentInParent<PongBallController>();
        }
        if (ball == null)
        {
            return;
        }

        bool effectApplied = ball.TryApplyTargetEffect(targetType, transform.position);

        if (effectApplied)
        {
            ball.AddTargetValueBonus(targetType);
            targetManager.OnTargetCollected();
        }
    }
}