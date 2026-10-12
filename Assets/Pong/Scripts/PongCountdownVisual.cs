using System.Collections;
using TMPro;
using UnityEngine;

// Shared by the side-selection countdown and the existing Quick Mode countdown.
public static class PongCountdownVisual
{
    public static IEnumerator Play(TMP_Text text, PongSettings settings,
        float stepDuration, Vector3 baseScale, Color baseColor, bool unscaledTime,
        RectTransform layer = null)
    {
        stepDuration = Mathf.Max(0.01f, stepDuration);
        Show(text, layer);

        for (int number = 3; number >= 1; number--)
        {
            text.text = number.ToString();
            float elapsed = 0f;
            ApplyFrame(text, settings, 0f, stepDuration, baseScale, baseColor);

            while (elapsed < stepDuration)
            {
                yield return null;
                elapsed += unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                ApplyFrame(text, settings, elapsed, stepDuration, baseScale, baseColor);
            }
        }

        Reset(text, baseScale, baseColor);
    }

    public static void PrepareLayer(RectTransform layer)
    {
        if (layer == null)
            return;

        // This is the shared container under Canvas, independent of menu and HUD panels.
        layer.gameObject.SetActive(true);
        layer.SetAsLastSibling();
        CanvasGroup group = layer.GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.alpha = 1f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }
    }

    public static void Show(TMP_Text text, RectTransform layer)
    {
        PrepareLayer(layer);
        text.raycastTarget = false;
        text.gameObject.SetActive(true);
    }

    private static void ApplyFrame(TMP_Text text, PongSettings settings, float elapsed,
        float duration, Vector3 baseScale, Color baseColor)
    {
        float progress = Mathf.Clamp01(elapsed / duration);
        text.rectTransform.localScale = baseScale *
            Mathf.Lerp(1f, settings.countdownFinalScale, progress);

        float fadeStart = Mathf.Clamp(settings.countdownFadeStart, 0f, duration);
        float fade = elapsed > fadeStart
            ? Mathf.Clamp01((elapsed - fadeStart) / Mathf.Max(0.001f, duration - fadeStart))
            : 0f;
        Color color = baseColor;
        color.a *= Mathf.Lerp(1f, settings.countdownFinalAlpha, fade);
        text.color = color;
    }

    // Call this explicitly when cancelling a coroutine as well.
    public static void Reset(TMP_Text text, Vector3 baseScale, Color baseColor)
    {
        if (text == null)
            return;
        text.rectTransform.localScale = baseScale;
        text.color = baseColor;
        text.gameObject.SetActive(false);
    }
}
