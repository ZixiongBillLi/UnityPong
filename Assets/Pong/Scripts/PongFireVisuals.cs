using UnityEngine;

public class PongFireVisuals : MonoBehaviour
{
    public PongBallController ball;

    public GameObject ballVisual;
    public GameObject fireVisual;


    public GameObject[] fireFrames;

    public float frameDuration = 0.08f;

    private bool active = false;

    private float frameTimer = 0f;

    private int lastFrameIndex = -1;

    private Vector3 fireBaseScale;
    private Quaternion fireBaseRotation;


    private void Awake()
    {
        fireBaseScale = fireVisual.transform.localScale;
        fireBaseRotation = fireVisual.transform.localRotation;

        fireVisual.SetActive(false);

        HideAllFrames();
    }


    private void Update()
    {
        if (!active)
        {
            return;
        }

        UpdateDirection();
        frameTimer += Time.deltaTime;

        if (frameTimer >= frameDuration)
        {
            frameTimer = 0f;

            SetNextRandomFrame();
        }
    }


    public void StartFire()
    {
        active = true;

        frameTimer = 0f;
        lastFrameIndex = -1;

        ballVisual.SetActive(false);
        fireVisual.SetActive(true);

        SetNextRandomFrame();
        UpdateDirection();
    }


    public void StopFire()
    {
        active = false;

        fireVisual.SetActive(false);

        HideAllFrames();

        ballVisual.SetActive(true);

        fireVisual.transform.localScale = fireBaseScale;
        fireVisual.transform.localRotation = fireBaseRotation;
    }

    private void SetNextRandomFrame()
    {
        if (fireFrames == null || fireFrames.Length == 0)
        {
            return;
        }

        int nextIndex;

        if (fireFrames.Length == 1)
        {
            nextIndex = 0;
        }
        else
        {
            do
            {
                nextIndex = Random.Range(0, fireFrames.Length);
            }
            while (nextIndex == lastFrameIndex);
        }

        lastFrameIndex = nextIndex;


        for (int i = 0; i < fireFrames.Length; i++)
        {
            fireFrames[i].SetActive(i == nextIndex);
        }
    }


    private void UpdateDirection()
    {
        Vector2 velocity = ball.GetVelocity();

        if (velocity.sqrMagnitude < 0.0001f)
        {
            return;
        }

        // The source sprites point up. Their tips should point backward along the trail.
        Vector3 tailDirection = new Vector3(-velocity.x, -velocity.y, 0f);
        Transform anchor = fireVisual.transform.parent;
        if (anchor != null)
            tailDirection = anchor.InverseTransformVector(tailDirection);
        float angle = Mathf.Atan2(tailDirection.y, tailDirection.x) * Mathf.Rad2Deg - 90f;
        fireVisual.transform.localScale = fireBaseScale;
        fireVisual.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    //Hopefully randomize next frame
    private void HideAllFrames()
    {
        if (fireFrames == null)
        {
            return;
        }

        foreach (GameObject frame in fireFrames)
        {
            if (frame != null)
            {
                frame.SetActive(false);
            }
        }
    }
}
