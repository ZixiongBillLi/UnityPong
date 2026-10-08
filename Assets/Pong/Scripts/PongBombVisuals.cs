using UnityEngine;

public class PongBombVisuals : MonoBehaviour
{
    public PongSettings settings;
    public GameObject ballVisual;

    public SpriteRenderer ballRenderer;

    public GameObject bombVisual;
    public SpriteRenderer blackBombRenderer;
    public SpriteRenderer redBombRenderer;

    private bool active = false;
    private float flashTime = 0f;


    private void Awake()
    {
        bombVisual.SetActive(false);
    }


    private void Update()
    {
        if (!active)
        {
            return;
        }

        UpdateFlash();
        UpdateRotation();
    }


    public void StartBomb()
    {
        active = true;
        flashTime = 0f;

        ballRenderer.enabled = false;

        ballVisual.SetActive(false);
        bombVisual.SetActive(true);

        SetRendererAlpha(blackBombRenderer, 1f);

        SetRendererAlpha(redBombRenderer, 0f);

        bombVisual.transform.localRotation = Quaternion.identity;
    }


    public void StopBomb()
    {
        active = false;

        bombVisual.SetActive(false);
        ballVisual.SetActive(true);

        ballRenderer.enabled = true;
    }


    private void UpdateFlash()
    {
        flashTime += Time.deltaTime;

        float blend = Mathf.PingPong( flashTime / settings.bombFlashHalfCycle, 1f);

        SetRendererAlpha(blackBombRenderer, 1f - blend);

        SetRendererAlpha(redBombRenderer, blend);
    }


    private void UpdateRotation()
    {
        PongBallController ball = GetComponent<PongBallController>();

        Vector2 velocity = ball.GetVelocity();

        if (Mathf.Abs(velocity.x) < 0.001f)
        {
            return;
        }

        float rotationDirection;

        if (velocity.x > 0f)
        {
            rotationDirection = -1f;
        }
        else
        {
            rotationDirection = 1f;
        }

        bombVisual.transform.Rotate(0f, 0f, rotationDirection * settings.bombRotationSpeed * Time.deltaTime);
    }


    private void SetRendererAlpha(SpriteRenderer renderer,float alpha)
    {
        Color color = renderer.color;
        color.a = alpha;
        renderer.color = color;
    }
}