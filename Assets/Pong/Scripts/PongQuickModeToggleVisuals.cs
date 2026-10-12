using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Toggle))]
public class PongQuickModeToggleVisuals : MonoBehaviour
{
    [Header("Switch Images (the label shell stays separate)")]
    [SerializeField] private Image trackImage;
    [Tooltip("Optional ON artwork with its own text and glow. Put this directly under Switch, before Thumb.")]
    [SerializeField] private Image onTrackImage;
    [SerializeField] private Image thumbImage;

    [Header("Slide")]
    [Tooltip("Set to zero for an immediate switch instead of an animation.")]
    [SerializeField, Min(0f)] private float transitionDuration = 0.18f;
    [SerializeField, Min(0f)] private float thumbInset = 6f;

    [Header("Off / On colors")]
    [SerializeField] private Color offTrackColor = new Color(0.72f, 0.75f, 0.79f, 1f);
    [SerializeField] private Color onTrackColor = new Color(0.08f, 0.85f, 1f, 1f);
    [SerializeField] private Color offThumbColor = new Color(0.80f, 0.83f, 0.87f, 1f);
    [SerializeField] private Color onThumbColor = new Color(0.90f, 0.98f, 1f, 1f);

    private Toggle toggle;
    private float visualState;
    private float startState;
    private float targetState;
    private float elapsed;
    private float thumbY;
    private bool initialized;
    private bool animating;

    private void Awake()
    {
        toggle = GetComponent<Toggle>();
        if (trackImage == null || thumbImage == null ||
            trackImage == thumbImage || thumbImage.transform.parent != trackImage.transform ||
            trackImage.gameObject != gameObject ||
            (onTrackImage != null && (onTrackImage == trackImage || onTrackImage == thumbImage ||
                onTrackImage.transform.parent != trackImage.transform)))
        {
            Debug.LogError("PongQuickModeToggleVisuals: assign this Switch's Image as Track Image " +
                "and its direct child Thumb Image. Optional On Track Image must also be a direct child. " +
                "Keep the label shell outside the Switch.", this);
            enabled = false;
            return;
        }

        toggle.transition = Selectable.Transition.None;
        // Native Toggle graphics hide when off. Both the track and sliding thumb stay visible.
        toggle.graphic = null;
        toggle.targetGraphic = trackImage;
        trackImage.raycastTarget = true;
        thumbImage.raycastTarget = false;
        if (onTrackImage != null)
        {
            onTrackImage.raycastTarget = false;
            onTrackImage.enabled = true;
        }

        RectTransform thumb = thumbImage.rectTransform;
        thumbY = thumb.anchoredPosition.y;
        thumb.anchorMin = Vector2.one * 0.5f;
        thumb.anchorMax = Vector2.one * 0.5f;
        thumb.pivot = Vector2.one * 0.5f;
        initialized = true;
    }

    private void OnEnable()
    {
        if (!initialized)
            return;
        toggle.onValueChanged.AddListener(OnValueChanged);
        RefreshImmediate();
    }

    private void OnDisable()
    {
        if (toggle != null)
            toggle.onValueChanged.RemoveListener(OnValueChanged);
        animating = false;
    }

    public void RefreshImmediate()
    {
        if (!initialized)
            return;
        visualState = toggle.isOn ? 1f : 0f;
        targetState = visualState;
        animating = false;
        ApplyVisuals();
    }

    private void OnValueChanged(bool isOn)
    {
        startState = visualState;
        targetState = isOn ? 1f : 0f;
        elapsed = 0f;
        animating = transitionDuration > 0f;
        if (!animating)
        {
            visualState = targetState;
            ApplyVisuals();
        }
    }

    private void Update()
    {
        if (!animating)
            return;

        elapsed += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.001f, transitionDuration));
        visualState = Mathf.Lerp(startState, targetState, Mathf.SmoothStep(0f, 1f, progress));
        ApplyVisuals();
        if (progress >= 1f)
            animating = false;
    }

    private void OnRectTransformDimensionsChange()
    {
        if (initialized)
            ApplyVisuals();
    }

    private void ApplyVisuals()
    {
        if (onTrackImage != null)
        {
            // Keep the dark OFF artwork underneath. The ON plate covers its text and
            // fades in its own colored rim and halo without tinting the dark interior.
            trackImage.color = Color.white;
            onTrackImage.color = new Color(1f, 1f, 1f, visualState);
        }
        else
        {
            trackImage.color = Color.Lerp(offTrackColor, onTrackColor, visualState);
        }
        thumbImage.color = Color.Lerp(offThumbColor, onThumbColor, visualState);

        RectTransform thumb = thumbImage.rectTransform;
        float thumbWidth = thumb.rect.width * Mathf.Abs(thumb.localScale.x);
        float travel = Mathf.Max(0f, (trackImage.rectTransform.rect.width - thumbWidth) * 0.5f - thumbInset);
        thumb.anchoredPosition = new Vector2(Mathf.Lerp(-travel, travel, visualState), thumbY);
    }
}
