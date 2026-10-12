using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PongBallController))]
public class PongBallPresentation : MonoBehaviour
{
    [SerializeField] private Transform heightRoot;
    [SerializeField] private SpriteRenderer shadow;
    [SerializeField, Range(0.1f, 1f)] private float flightShadowScale = 0.8f;

    private PongBallController ball;
    private PongPaddleController leftPaddle;
    private PongPaddleController rightPaddle;
    private Vector3 heightBasePosition;
    private Vector3 shadowBaseScale;
    private float flightOffset;
    private bool initialized;
    private bool entranceActive;
    private bool entranceVisible = true;
    private float entranceHeight;
    private float entranceMaximumHeight;
    private float flightProgress;
    private float launchX;
    private float flightLimitX;

    public bool ReferencesAssigned => heightRoot != null && shadow != null &&
        heightRoot.parent == transform && shadow.transform.parent == transform;

    private void Awake() => Initialize();

    private void Initialize()
    {
        if (initialized || !ReferencesAssigned)
            return;
        ball = GetComponent<PongBallController>();
        heightBasePosition = heightRoot.localPosition;
        shadowBaseScale = shadow.transform.localScale;
        // Inspector Y is the maximum airborne offset; its manually adjusted X/Y scale is retained.
        flightOffset = Mathf.Abs(transform.TransformVector(shadow.transform.localPosition).y);
        initialized = true;
    }

    private void OnEnable()
    {
        Initialize();
        if (!initialized)
            return;
        ball.Launched += OnLaunched;
        ball.ServeReset += OnServeReset;
        OnServeReset();
    }

    private void OnDisable()
    {
        if (ball != null)
        {
            ball.Launched -= OnLaunched;
            ball.ServeReset -= OnServeReset;
        }
        if (initialized)
        {
            heightRoot.localPosition = heightBasePosition;
            shadow.transform.localScale = shadowBaseScale;
        }
    }

    public void ConfigurePaddles(PongPaddleController left, PongPaddleController right)
    {
        leftPaddle = left;
        rightPaddle = right;
    }

    public void SetEntranceVisible(bool visible)
    {
        Initialize();
        if (!initialized)
            return;
        entranceVisible = visible;
        heightRoot.gameObject.SetActive(visible);
        UpdateShadow();
    }

    public void BeginEntrance(float maximumHeight)
    {
        Initialize();
        entranceActive = true;
        entranceMaximumHeight = Mathf.Max(0.01f, maximumHeight);
        entranceVisible = true;
        heightRoot.gameObject.SetActive(true);
        SetEntranceHeight(maximumHeight);
    }

    public void SetEntranceHeight(float height)
    {
        entranceHeight = Mathf.Max(0f, height);
        heightRoot.localPosition = heightBasePosition +
            transform.InverseTransformVector(Vector3.up * entranceHeight);
        UpdateShadow();
    }

    public void EndEntrance()
    {
        if (!initialized)
            return;
        entranceActive = false;
        entranceHeight = 0f;
        heightRoot.localPosition = heightBasePosition;
        SetEntranceVisible(true);
    }

    private void OnServeReset()
    {
        entranceActive = false;
        entranceHeight = 0f;
        flightProgress = 0f;
        heightRoot.localPosition = heightBasePosition;
        UpdateShadow();
    }

    private void OnLaunched()
    {
        launchX = ball.transform.position.x;
        bool movingLeft = ball.GetVelocity().x < 0f;
        PongPaddleController paddle = movingLeft ? leftPaddle : rightPaddle;
        flightLimitX = paddle != null ? paddle.ForwardLimitX : launchX + (movingLeft ? -2f : 2f);
        flightProgress = 0f;
    }

    private void LateUpdate()
    {
        if (!initialized)
            return;
        if (!entranceActive && ball.IsMoving())
        {
            // Rebounds and portals cannot make an already raised ball appear to descend.
            flightProgress = AccumulateFlightProgress(flightProgress, launchX, flightLimitX,
                ball.transform.position.x, ball.paddleHitCount);
        }
        UpdateShadow();
    }

    internal static float AccumulateFlightProgress(float current, float launchX, float limitX,
        float ballX, int paddleHits)
    {
        if (paddleHits > 0)
            return 1f;
        float distance = limitX - launchX;
        float progress = Mathf.Abs(distance) > 0.001f ? Mathf.Clamp01((ballX - launchX) / distance) : 1f;
        return Mathf.Max(current, progress);
    }

    private void UpdateShadow()
    {
        if (!initialized)
            return;
        Vector3 position = shadow.transform.position;
        position.x = ball.transform.position.x;
        position.y = ball.transform.position.y - (entranceActive ? 0f : flightOffset * flightProgress);
        shadow.transform.position = position;

        float scale = entranceActive
            ? Mathf.Lerp(1f, ball.settings.ballShadowMinimumScale,
                Mathf.Clamp01(entranceHeight / entranceMaximumHeight))
            : Mathf.Lerp(1f, flightShadowScale, flightProgress);
        shadow.transform.localScale = shadowBaseScale * scale;
        shadow.enabled = entranceVisible && !ball.IsTeleporting;
    }
}
