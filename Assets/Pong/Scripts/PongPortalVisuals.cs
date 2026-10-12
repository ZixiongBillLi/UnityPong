using UnityEngine;
using System.Collections;

public class PongPortalVisuals : MonoBehaviour
{
    // ---------- References ----------

    public Transform portal;
    public Transform flash;

    public SpriteRenderer flashRenderer;


    // ---------- Portal Settings ----------

    public float portalScaleDuration = 0.15f;


    // ---------- Flash Settings ----------

    // Full cross -> horizontal line
    public float flashFlattenDuration = 0.08f;

    // Horizontal line -> point
    public float flashCollapseDuration = 0.10f;

    // Make the horizontal line slightly wider before it collapses.
    public float flashWideScale = 1.15f;

    // How thin the horizontal line becomes.
    public float flashThinScale = 0.08f;


    // ---------- Stored Scales ----------

    private Vector3 portalBaseScale;
    private Vector3 flashBaseScale;


    private void Awake()
    {
        portalBaseScale = portal.localScale;
        flashBaseScale = flash.localScale;

        HideFlash();
    }


    // =========================================================
    // PUBLIC ANIMATIONS
    // =========================================================


    // Entrance:
    // Flash appears -> CRT disappears -> portal shrinks.
    public IEnumerator PlayEntranceDisappear()
    {
        portal.gameObject.SetActive(true);
        portal.localScale = portalBaseScale;

        yield return PlayFlash();

        yield return AnimateScale(
            portal,
            portalBaseScale,
            Vector3.zero,
            portalScaleDuration
        );

        portal.gameObject.SetActive(false);
    }


    // Exit portal:
    // Portal grows from zero to normal size.
    public IEnumerator PlayPortalAppear()
    {
        portal.gameObject.SetActive(true);
        portal.localScale = Vector3.zero;

        yield return AnimateScale(
            portal,
            Vector3.zero,
            portalBaseScale,
            portalScaleDuration
        );
    }


    // Used when the ball appears.
    // Full cross -> horizontal line -> point -> disappear.
    public IEnumerator PlayFlash()
    {
        ShowFlash();

        Vector3 thinLineScale = new Vector3(
            flashBaseScale.x * flashWideScale,
            flashBaseScale.y * flashThinScale,
            flashBaseScale.z
        );

        // Cross -> horizontal line
        yield return AnimateScale(
            flash,
            flashBaseScale,
            thinLineScale,
            flashFlattenDuration
        );

        Vector3 pointScale = new Vector3(
            0f,
            flashBaseScale.y * flashThinScale,
            flashBaseScale.z
        );

        float timer = 0f;

        // Horizontal line -> point
        while (timer < flashCollapseDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(
                timer / flashCollapseDuration
            );

            float smoothT =
                Mathf.SmoothStep(0f, 1f, t);

            flash.localScale =
                Vector3.Lerp(
                    thinLineScale,
                    pointScale,
                    smoothT
                );

            SetFlashAlpha(1f - smoothT);

            yield return null;
        }

        HideFlash();
    }


    // Final exit:
    // Portal simply shrinks away.
    public IEnumerator PlayPortalDisappear()
    {
        yield return AnimateScale(
            portal,
            portalBaseScale,
            Vector3.zero,
            portalScaleDuration
        );

        portal.gameObject.SetActive(false);
    }


    // =========================================================
    // HELPERS
    // =========================================================


    private IEnumerator AnimateScale(
        Transform target,
        Vector3 startScale,
        Vector3 endScale,
        float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(
                timer / duration
            );

            float smoothT =
                Mathf.SmoothStep(0f, 1f, t);

            target.localScale =
                Vector3.Lerp(
                    startScale,
                    endScale,
                    smoothT
                );

            yield return null;
        }

        target.localScale = endScale;
    }


    private void ShowFlash()
    {
        flash.gameObject.SetActive(true);

        flash.localScale = flashBaseScale;

        SetFlashAlpha(1f);
    }


    private void HideFlash()
    {
        flash.gameObject.SetActive(false);

        flash.localScale = flashBaseScale;

        SetFlashAlpha(1f);
    }


    private void SetFlashAlpha(float alpha)
    {
        Color color = flashRenderer.color;

        color.a = alpha;

        flashRenderer.color = color;
    }

    public void StartFlash()
    {
        StartCoroutine(PlayFlash());
    }
}