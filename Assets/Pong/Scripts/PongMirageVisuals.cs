using UnityEngine;
using System.Collections;

public class PongMirageVisuals : MonoBehaviour
{
    public PongSettings settings;

    public GameObject mirageA;
    public GameObject mirageB;

    private bool active = false;
    private float angle = 90f;

    private Coroutine fadeRoutine;

    public SpriteRenderer ballVisualRenderer;


    private void Awake()
    {
        mirageA.SetActive(false);
        mirageB.SetActive(false);
    }

    private void Start()
    {
    }

    private void Update()
    {
        if (!active)
        {
            return;
        }

        angle += settings.mirageRotationSpeed * Time.deltaTime;

        float radians = angle * Mathf.Deg2Rad;

        Vector2 offsetA = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * settings.mirageRadius;

        Vector2 offsetB = new Vector2(Mathf.Cos(radians + Mathf.PI), Mathf.Sin(radians + Mathf.PI)) * settings.mirageRadius;

        mirageA.transform.localPosition = offsetA;

        mirageB.transform.localPosition = offsetB;
    }

    public void StartMirage()
    {
        angle = 90f;

        active = true;

        mirageA.SetActive(true);
        mirageB.SetActive(true);

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(
            FadeBallAlpha(settings.mirageBallMinAlpha, settings.mirageFadeDuration)
        );
    }

    public void StopMirage()
    {
        active = false;

        mirageA.SetActive(false);
        mirageB.SetActive(false);

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        SetBallAlpha(1f);
    }

    private IEnumerator FadeBallAlpha(
        float targetAlpha,
        float duration
    )
    {
        float startAlpha = ballVisualRenderer.color.a;

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float progress = elapsedTime / duration;

            progress = Mathf.Clamp01(progress);

            float newAlpha = Mathf.Lerp( startAlpha, targetAlpha, progress);

            SetBallAlpha(newAlpha);

            yield return null;
        }

        SetBallAlpha(targetAlpha);

        fadeRoutine = null;
    }

    private void SetBallAlpha(float alpha)
    {
        Color color = ballVisualRenderer.color;

        color.a = alpha;

        ballVisualRenderer.color = color;
    }
}