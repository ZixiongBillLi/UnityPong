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

        // UpdatePlaceholderColor();
    }

    // private void UpdatePlaceholderColor()
    // {
    //     switch (targetType)
    //     {
    //         case TargetType.Freeze:
    //             spriteRenderer.color = Color.cyan;
    //             break;

    //         case TargetType.Fire:
    //             spriteRenderer.color = new Color(1f, 0.3f, 0f);
    //             break;

    //         case TargetType.Bomb:
    //             spriteRenderer.color = Color.gray;
    //             break;

    //         case TargetType.Mirage:
    //             spriteRenderer.color = new Color(0.7f, 0.3f, 1f);
    //             break;

    //         case TargetType.Portal:
    //             spriteRenderer.color = Color.magenta;
    //             break;
    //     }
    // }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PongBallController ball = other.GetComponent<PongBallController>();

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