using System.Collections;
using UnityEngine;

public class PongGoalShockwave : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;

    public float startScale = 0.3f;
    public float endScale = 3.5f;

    public float duration = 0.45f;

    public float startAlpha = 0.55f;
    public float endAlpha = 0f;

    private void Start()
    {
        StartCoroutine(ShockwaveRoutine());
    }

    private IEnumerator ShockwaveRoutine()
    {
        float elapsedTime = 0f;

        transform.localScale =
            Vector3.one * startScale;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsedTime / duration);

            float scale = Mathf.Lerp(startScale, endScale, progress);

            transform.localScale = Vector3.one * scale;

            float alpha = Mathf.Lerp(startAlpha, endAlpha, progress);

            Color color = spriteRenderer.color;

            color.a = alpha;

            spriteRenderer.color = color;

            yield return null;
        }

        Destroy(gameObject);
    }
}