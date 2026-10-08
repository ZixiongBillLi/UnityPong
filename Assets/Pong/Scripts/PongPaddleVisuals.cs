using System.Collections;
using UnityEngine;

public class PongPaddleVisuals : MonoBehaviour
{

    public GameObject sweatDrop;

    public GameObject readyVisual;
    public GameObject middle1Visual;
    public GameObject middle2Visual;
    public GameObject swingVisual;

    public Transform visualRoot;

    public PongBallController ball;
    public Collider2D paddleCollider;

    public PaddleSide paddleSide;

    public float middleFrameDuration = 0.08f;
    public float swingHoldDuration = 0.8f;
    public float returnFrameDuration = 0.15f;
    
    public float hopHeight = 0.08f;
    public float hopDuration = 0.05f;


    private Collider2D ballCollider;

    private Vector3 visualStartLocalPos;

    private Coroutine hitRoutine;

    private bool anticipatingHit = false;

    private void Awake()
    {
        if (visualRoot == null)
        {
            visualRoot = transform;
        }

        if (paddleCollider == null)
        {
            paddleCollider = GetComponent<Collider2D>();
        }

        if (ball != null)
        {
            ballCollider = ball.GetComponent<Collider2D>();
        }

        visualStartLocalPos = visualRoot.localPosition;

        SetReady();

        if (sweatDrop != null)
        {
            sweatDrop.SetActive(false);
        }
    }


    private void Update()
    {
        if (Time.timeScale == 0f)
        {
            return;
        }

        if (hitRoutine != null)
        {
            return;
        }

        if (ball == null || paddleCollider == null || ballCollider == null)
        {
            return;
        }

        UpdatePreHitAnticipation();
    }


    private void UpdatePreHitAnticipation()
    {
        Vector2 velocity = ball.GetVelocity();

        if (velocity.sqrMagnitude < 0.001f)
        {
            CancelAnticipation();
            return;
        }

        bool movingTowardPaddle;

        if (paddleSide == PaddleSide.Left)
        {
            movingTowardPaddle = velocity.x < 0f;
        }
        else
        {
            movingTowardPaddle = velocity.x > 0f;
        }

        if (!movingTowardPaddle)
        {
            CancelAnticipation();
            return;
        }


        float ballRadius = ballCollider.bounds.extents.x;

        float contactX;

        if (paddleSide == PaddleSide.Left)
        {
            contactX = paddleCollider.bounds.max.x + ballRadius;
        }
        else
        {
            contactX = paddleCollider.bounds.min.x - ballRadius;
        }


        float timeToImpact = (contactX - ball.transform.position.x) / velocity.x;

        float ballY = ball.transform.position.y;

        float ballHalfHeight = ballCollider.bounds.extents.y;

        bool verticalOverlap = ballY >= paddleCollider.bounds.min.y - ballHalfHeight && ballY <= paddleCollider.bounds.max.y + ballHalfHeight;


        bool shouldAnticipate = verticalOverlap && timeToImpact >= 0f && timeToImpact <= middleFrameDuration;


        if (shouldAnticipate)
        {
            if (!anticipatingHit)
            {
                anticipatingHit = true; SetMiddle1();
            }
        }
        else
        {
            CancelAnticipation();
        }
    }


    private void CancelAnticipation()
    {
        if (!anticipatingHit)
        {
            return;
        }

        anticipatingHit = false;
        SetReady();
    }


    public void PlayHitFeedback()
    {
        anticipatingHit = false;

        if (hitRoutine != null)
        {
            StopCoroutine(hitRoutine);
        }

        visualRoot.localPosition = visualStartLocalPos;

        hitRoutine = StartCoroutine(HitFeedbackRoutine());
    }


    private IEnumerator HitFeedbackRoutine()
    {
        SetMiddle2();

        yield return new WaitForSeconds(middleFrameDuration);

        SetSwing();


        // yield return StartCoroutine(HopOnce());
        yield return StartCoroutine(HopOnce());

        visualRoot.localPosition = visualStartLocalPos;

        yield return new WaitForSeconds(swingHoldDuration);

        SetMiddle2();

        yield return new WaitForSeconds(middleFrameDuration);

        SetMiddle1();

        yield return new WaitForSeconds(middleFrameDuration);

        SetReady();

        visualRoot.localPosition = visualStartLocalPos;

        hitRoutine = null;
    }


    private IEnumerator HopOnce()
    {
        Vector3 upPos = visualStartLocalPos + Vector3.up * hopHeight;

        float t = 0f;

        while (t < hopDuration)
        {
            t += Time.deltaTime;

            float p = Mathf.Clamp01(t / hopDuration);

            visualRoot.localPosition = Vector3.Lerp(visualStartLocalPos, upPos, p);

            yield return null;
        }


        t = 0f;

        while (t < hopDuration)
        {
            t += Time.deltaTime;

            float p = Mathf.Clamp01(t / hopDuration);

            visualRoot.localPosition = Vector3.Lerp(upPos, visualStartLocalPos, p);

            yield return null;
        }

        visualRoot.localPosition = visualStartLocalPos;
    }


    private void SetFrame(GameObject activeFrame)
    {
        readyVisual.SetActive(activeFrame == readyVisual);

        middle1Visual.SetActive(activeFrame == middle1Visual);

        middle2Visual.SetActive(activeFrame == middle2Visual);

        swingVisual.SetActive(activeFrame == swingVisual);
    }


    public void SetReady()
    {
        SetFrame(readyVisual);
    }


    private void SetMiddle1()
    {
        SetFrame(middle1Visual);
    }


    private void SetMiddle2()
    {
        SetFrame(middle2Visual);
    }


    private void SetSwing()
    {
        SetFrame(swingVisual);
    }


    public void SetSweat(bool visible)
    {
        sweatDrop.SetActive(visible);
    }
}