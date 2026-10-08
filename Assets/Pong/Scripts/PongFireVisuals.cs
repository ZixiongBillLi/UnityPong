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

    private bool flipVertical = false;


    private Vector3 fireBaseScale;


    private void Awake()
    {
        fireBaseScale = fireVisual.transform.localScale;

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

            flipVertical = !flipVertical;

            SetNextRandomFrame();
        }
    }


    public void StartFire()
    {
        active = true;

        frameTimer = 0f;
        lastFrameIndex = -1;

        flipVertical = false;

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

        if (Mathf.Abs(velocity.x) < 0.001f)
        {
            return;
        }

        float xScale = Mathf.Abs(fireBaseScale.x);

        float yScale = Mathf.Abs(fireBaseScale.y);

        if (velocity.x < 0f)
        {
            xScale *= -1f;
        }

        if (flipVertical)
        {
            yScale *= -1f;
        }

        fireVisual.transform.localScale = new Vector3(xScale, yScale, fireBaseScale.z);
    }


    private void UpdateFrameAnimation()
    {
        frameTimer += Time.deltaTime;

        if (frameTimer < frameDuration)
        {
            return;
        }

        frameTimer = 0f;

        SetNextRandomFrame();
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