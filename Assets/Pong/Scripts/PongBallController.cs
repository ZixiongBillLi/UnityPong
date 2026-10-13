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
    public GameObject ballVisual;

    public GameObject portalVisualPrefab;
    private GameObject activePortalVisual;

    private PongBombVisuals bombVisuals;
    private PongFireVisuals fireVisuals;

    public event System.Action Launched;
    public event System.Action ServeReset;
    public bool IsTeleporting => isTeleporting;
    public TargetType? ActiveEffect => hasActiveEffect ? activeEffect : (TargetType?)null;
    private Vector2 servePosition;
    private bool initialized;
    private bool serveInputArmed;

    public Vector2 ServePosition
    {
        get { Initialize(); return servePosition; }
    }

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (initialized)
            return;
        rb = GetComponent<Rigidbody2D>();
        bombVisuals = GetComponent<PongBombVisuals>();
        fireVisuals = GetComponent<PongFireVisuals>();
        servePosition = transform.position;
        currentBaseSpeed = settings.baseBallSpeed;
        InputActionMap pongMap = inputActions.FindActionMap("Pong");
        interactAction = pongMap.FindAction("Spawn");
        interactAction.Enable();
        initialized = true;
    }

    private void OnEnable() => RequireServeInputRelease();

    public void RequireServeInputRelease() => serveInputArmed = false;

    private void Update()
    {
        if (gameManager != null && !gameManager.CanManuallyServe)
            return;

        if (!serveInputArmed)
        {
            serveInputArmed = !interactAction.IsPressed();
            return;
        }

        if (!isMoving && Time.timeScale > 0f && interactAction.WasPressedThisFrame())
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
        Launched?.Invoke();
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
        if (!isMoving || isTeleporting)
            return;
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
        Initialize();
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

        // The Inspector's initial Ball position is the serve/entrance landing point.
        rb.position = servePosition;
        RequireServeInputRelease();
        ServeReset?.Invoke();
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
        if (hasActiveEffect || !isMoving)
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
        else if (activeEffect == TargetType.Portal)
        {
            isTeleporting = false;

            SetBallVisible(true);

            ClearPortalVisual();
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

    Vector2 exitPosition =
        targetManager.GetPortalExitPosition(
            entrancePosition,
            savedDirection
        );


    // =====================================================
    // Entrance
    // =====================================================

    // Ball disappears immediately.
    SetBallVisible(false);


    // Create a visual-only copy of the entrance portal.
    activePortalVisual = Instantiate(
        portalVisualPrefab,
        entrancePosition,
        Quaternion.identity
    );

    PongPortalVisuals entranceVisual =
        activePortalVisual.GetComponent<PongPortalVisuals>();



    // Flash disappears first, then portal shrinks away.
    yield return entranceVisual.PlayEntranceDisappear();


    Destroy(activePortalVisual);
    activePortalVisual = null;


    // =====================================================
    // Travel
    // =====================================================

    // The original portal travel timer begins only
    // after the entrance animation has finished.
    yield return new WaitForSeconds(
        settings.portalTravelDelay
    );


    // Teleport the invisible ball immediately.
    rb.position = exitPosition;


    // =====================================================
    // Exit
    // =====================================================

    activePortalVisual = Instantiate(
        portalVisualPrefab,
        exitPosition,
        Quaternion.identity
    );

    PongPortalVisuals exitVisual =
        activePortalVisual.GetComponent<PongPortalVisuals>();


    // Portal grows back using the same scale duration.
    yield return exitVisual.PlayPortalAppear();


    // Wait before the ball comes back.
    yield return new WaitForSeconds(
        settings.portalExitDelay
    );


    // =====================================================
    // Ball returns
    // =====================================================

    direction = savedDirection;

    SetBallVisible(true);

    // Ball may move again.
    isTeleporting = false;


    // Ball and flash appear at the same time.
    exitVisual.StartFlash();


    // Keep the portal visible after the ball returns.
    yield return new WaitForSeconds(
        settings.portalExitVisualLifetime
    );


    // By this point the flash has already disappeared.
    yield return exitVisual.PlayPortalDisappear();


    Destroy(activePortalVisual);
    activePortalVisual = null;


    // =====================================================
    // Finish effect
    // =====================================================

    effectRoutine = null;

    EndActiveEffect(true);
}

    private void SetBallVisible(bool visible)
    {
        ballVisual.SetActive(visible);
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

    private void ClearPortalVisual()
    {
        if (activePortalVisual != null)
        {
            Destroy(activePortalVisual);

            activePortalVisual = null;
        }
    }

    
}
