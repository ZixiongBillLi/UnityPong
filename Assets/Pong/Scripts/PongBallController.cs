using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PongBallController : MonoBehaviour
{
    public PongSettings settings;

    public PongGameManager gameManager;

    public float currentBaseSpeed;
    public float temporarySpeedBonus = 0f;

    public InputActionAsset inputActions;

    private Rigidbody2D rb;
    private Vector2 direction;
    private InputAction interactAction;
    private bool isMoving = false;

    public int paddleHitCount = 0;

    public PongTargetManager targetManager;



    private bool hasActiveEffect = false;
    private TargetType activeEffect;
    private Coroutine effectRoutine;



    private int fireHitCount = 0;
    private int currentTargetValueBonus = 0;



    private int bombHitCount = 0;


    private bool isFrozen = false;


    public PongMirageVisuals mirageVisuals;

    private bool isTeleporting = false;
    private SpriteRenderer ballRenderer;

    public GameObject portalExitVisualPrefab;

    private PongBombVisuals bombVisuals;
    private PongFireVisuals fireVisuals;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ballRenderer = GetComponent<SpriteRenderer>();
        bombVisuals = GetComponent<PongBombVisuals>();
        fireVisuals = GetComponent<PongFireVisuals>();
    }

    private void Start()
    {
        currentBaseSpeed = settings.baseBallSpeed;

        InputActionMap pongMap = inputActions.FindActionMap("Pong");
        interactAction = pongMap.FindAction("Spawn");
        interactAction.Enable();
    }

    private void Update()
    {
        if (!isMoving && interactAction.WasPressedThisFrame())
        {
            LaunchBall();
        }
    }

    private void FixedUpdate()
    {
        if (isMoving && !isFrozen && !isTeleporting)
        {
            float effectiveSpeed = currentBaseSpeed + temporarySpeedBonus;

            Vector2 newPosition = rb.position + direction * effectiveSpeed * Time.fixedDeltaTime;


            rb.MovePosition(newPosition);
        }
    }

    private void LaunchBall()
    {
        float xDirection;

        if (Random.value < 0.5f)
        {
            xDirection = -1f;
        }
        else
        {
            xDirection = 1f;
        }

        float yDirection = Random.Range(0.4f, 1f);

        if (Random.value < 0.5f)
        {
            yDirection *= -1f;
        }

        direction = new Vector2(xDirection, yDirection).normalized;

        isMoving = true;
        targetManager.OnBallLaunched();
    }

    public void AutoLaunchBall()
    {
        if (isMoving)
        {
            return;
        }

        LaunchBall();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Vector2 normal = collision.GetContact(0).normal;

        Vector2 reflectedDirection = Vector2.Reflect(direction, normal);

        PongPaddleController paddle = collision.gameObject.GetComponent<PongPaddleController>();

        if (paddle != null)
        {
            PongPaddleVisuals paddleVisuals = paddle.GetComponent<PongPaddleVisuals>();

            if (paddleVisuals != null)
            {
                paddleVisuals.PlayHitFeedback();
            }

            Collider2D paddleCollider = paddle.GetComponent<Collider2D>();

            float halfHeight = paddleCollider.bounds.extents.y;

            float hitOffset = (rb.position.y - paddle.transform.position.y) / halfHeight;

            hitOffset = Mathf.Clamp(hitOffset, -1f, 1f);

            float newY = reflectedDirection.y + hitOffset * settings.paddleHitAngleInfluence;

            newY = Mathf.Clamp(newY, -0.9f, 0.9f);

            direction = new Vector2(reflectedDirection.x, newY).normalized;

            paddleHitCount++;
            currentBaseSpeed += settings.ballSpeedIncreasePerHit;

            currentBaseSpeed = Mathf.Clamp(currentBaseSpeed, settings.baseBallSpeed, settings.maxBallSpeed);



            if (hasActiveEffect && activeEffect == TargetType.Fire)
            {
                fireHitCount++;

                if (fireHitCount >= settings.fireHitLimit)
                {
                    EndActiveEffect(true);
                }
            }


            if (hasActiveEffect && activeEffect == TargetType.Bomb)
            {
                bombHitCount++;

                if (bombHitCount >= settings.bombHitLimit)
                {
                    EndActiveEffect(true);
                }
            }

            gameManager.UpdateBallValueText();
        }
        else
        {
            direction = reflectedDirection;
        }
    }
    

    public void ResetBall(float newServeSpeed)
    {
        EndActiveEffect(false);
        isMoving = false;
        isFrozen = false;
        isTeleporting = false;
        SetBallVisible(true);
        direction = Vector2.zero;

        currentBaseSpeed = newServeSpeed;
        temporarySpeedBonus = 0f;
        paddleHitCount = 0;
        currentTargetValueBonus = 0;

        rb.MovePosition(new Vector2(0f, settings.playfieldCenterY));
    }

    public int GetGoalPoints()
    {
        int basePoints = settings.minGoalPoints + paddleHitCount * settings.goalPointsIncreasePerHit;

        basePoints = Mathf.Clamp(basePoints, settings.minGoalPoints, settings.maxGoalPoints);

         int finalPoints = basePoints + currentTargetValueBonus;

        if (hasActiveEffect && activeEffect == TargetType.Bomb)
        {
            finalPoints *= settings.bombScoreMultiplier;
        }

        return finalPoints;
    }

    public bool IsMoving()
    {
        return isMoving;
    }

    public bool TryApplyTargetEffect(TargetType type, Vector2 targetPosition)
    {
        if (hasActiveEffect)
        {
            return false;
        }

        switch (type)
        {
            case TargetType.Fire:
                StartFireEffect();
                return true;

            case TargetType.Bomb:
                StartBombEffect();
                return true;

            case TargetType.Freeze:
                StartFreezeEffect();
                return true;

            case TargetType.Mirage:
                StartMirageEffect();
                return true;
            case TargetType.Portal:
                StartPortalEffect(targetPosition);
                return true;

            default:
                Debug.Log(type + " effect is not implemented yet.");
                return false;
        }
    }

    private void EndActiveEffect(bool notifyTargetManager)
    {
        if (!hasActiveEffect)
        {
            return;
        }

        if (effectRoutine != null)
        {
            StopCoroutine(effectRoutine);
            effectRoutine = null;
        }

        if (activeEffect == TargetType.Fire)
        {
            temporarySpeedBonus = 0f;
            fireHitCount = 0;

            fireVisuals.StopFire();
        }

        else if (activeEffect == TargetType.Bomb)
        {
            bombHitCount = 0;
            bombVisuals.StopBomb();
        }
        else if (activeEffect == TargetType.Mirage)
        {
            mirageVisuals.StopMirage();
        }


        hasActiveEffect = false;

        gameManager.UpdateBallValueText();

        if (notifyTargetManager)
        {
            targetManager.OnEffectEnded();
        }
    }
    private void StartFireEffect()
    {
        hasActiveEffect = true;
        activeEffect = TargetType.Fire;

        fireHitCount = 0;

        temporarySpeedBonus = settings.fireSpeedBonus;

        fireVisuals.StartFire();

        effectRoutine = StartCoroutine(FireEffectTimer());
    }

    private IEnumerator FireEffectTimer()
    {
        yield return new WaitForSeconds(
            settings.fireMaxDuration
        );

        effectRoutine = null;

        EndActiveEffect(true);
    }

    public void AddTargetValueBonus(TargetType targetType)
    {
        if (targetType != TargetType.Bomb)
        {
            currentTargetValueBonus += settings.targetPickupValueBonus;
        }
        
        gameManager.UpdateBallValueText();
    }

    private void StartFreezeEffect()
    {
        hasActiveEffect = true;
        activeEffect = TargetType.Freeze;

        effectRoutine =
            StartCoroutine(FreezeEffectRoutine());
    }


    private void StartBombEffect()
    {
        hasActiveEffect = true;
        activeEffect = TargetType.Bomb;

        bombHitCount = 0;

        bombVisuals.StartBomb();

        gameManager.UpdateBallValueText();
    }

    private IEnumerator FreezeEffectRoutine()
    {
        isFrozen = true;

        Vector2 frozenPosition = rb.position;
        Vector2 preFreezeDirection = direction;

        float effectiveBallSpeed = currentBaseSpeed + temporarySpeedBonus;

        float freezeDuration = gameManager.GetFreezeDuration(effectiveBallSpeed);

        float elapsedTime = 0f;

        // Shake briefly at the beginning.
        while (elapsedTime < settings.freezeShakeDuration)
        {
            Vector2 shakeOffset = Random.insideUnitCircle * settings.freezeShakeDistance;

            rb.MovePosition(frozenPosition + shakeOffset);

            yield return new WaitForFixedUpdate();

            elapsedTime += Time.fixedDeltaTime;
        }

        rb.MovePosition(frozenPosition);

        float remainingFreezeTime = Mathf.Max(0f, freezeDuration - settings.freezeShakeDuration);

        if (remainingFreezeTime > 0f)
        {
            yield return new WaitForSeconds(remainingFreezeTime);
        }

        // Keep the same horizontal direction.
        float xDirection = Mathf.Sign(preFreezeDirection.x);

        if (xDirection == 0f)
        {
            xDirection = 1f;
        }

        // Generate a new vertical component.
        float yDirection = Random.Range(settings.freezeMinYDirection, settings.freezeMaxYDirection);

        if (Random.value < 0.5f)
        {
            yDirection *= -1f;
        }

        direction = new Vector2(xDirection, yDirection).normalized;

        isFrozen = false;

        effectRoutine = null;

        EndActiveEffect(true);
    }

    private void StartMirageEffect()
    {
        hasActiveEffect = true;
        activeEffect = TargetType.Mirage;

        mirageVisuals.StartMirage();

        effectRoutine = StartCoroutine(MirageEffectTimer());
    }

    private IEnumerator MirageEffectTimer()
    {
        yield return new WaitForSeconds(settings.mirageDuration);

        effectRoutine = null;

        EndActiveEffect(true);
    }
    private void StartPortalEffect(Vector2 entrancePosition)
    {
        hasActiveEffect = true;
        activeEffect = TargetType.Portal;

        effectRoutine = StartCoroutine(PortalEffectRoutine(entrancePosition));
    }

    private IEnumerator PortalEffectRoutine(Vector2 entrancePosition)
    {
        isTeleporting = true;

        Vector2 savedDirection = direction;

        Vector2 exitPosition = targetManager.GetPortalExitPosition(entrancePosition, savedDirection);

        SetBallVisible(false);

        yield return new WaitForSeconds(settings.portalTravelDelay);

        rb.MovePosition(exitPosition);

        GameObject exitVisual = Instantiate(portalExitVisualPrefab, exitPosition, Quaternion.identity);

        yield return new WaitForSeconds(settings.portalExitDelay);

        direction = savedDirection;

        SetBallVisible(true);

        isTeleporting = false;

        float remainingVisualTime = settings.portalExitVisualLifetime - settings.portalExitDelay;

        remainingVisualTime = Mathf.Max(0f, remainingVisualTime);

        Destroy(exitVisual, remainingVisualTime);

        effectRoutine = null;

        EndActiveEffect(true);
    }

    private void SetBallVisible(bool visible)
    {
        ballRenderer.enabled = visible;
    }


    public Vector2 GetVelocity()
    {
        if (!isMoving || isFrozen || isTeleporting)
        {
            return Vector2.zero;
        }

        float effectiveSpeed = currentBaseSpeed + temporarySpeedBonus;

        return direction * effectiveSpeed;
    }
}