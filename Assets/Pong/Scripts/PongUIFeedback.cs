using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PongUIFeedback : MonoBehaviour,
    ISelectHandler, IDeselectHandler,
    IPointerEnterHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Visual References")]
    [SerializeField] private RectTransform visualRoot;
    [SerializeField] private Image buttonImage;
    [SerializeField] private Image pressedOverlay;

    [Header("Button Sprites")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite selectedSprite;

    [Header("Selection Pulse")]
    [SerializeField, Range(0f, 0.1f)] private float pulseAmount = 0.025f;
    [SerializeField, Min(0.1f)] private float pulsePeriod = 1.2f;

    [Header("Held Press")]
    [SerializeField] private Color pressedColor =
        new Color(0.8f, 0.8f, 0.8f, 0.22f);

    private Selectable control;
    private Vector3 baseScale;
    private bool selected;
    private bool pointerHeld;
    private bool keyboardHeld;
    private bool externalHeld;
    private float pulseTime;

    private bool CanInteract =>
        control != null && control.IsActive() && control.IsInteractable();

    private void Awake()
    {
        control = GetComponent<Selectable>();

        if (control == null)
        {
            Debug.LogError("PongUIFeedback needs a Button or Toggle.", this);
            enabled = false;
            return;
        }

        if (buttonImage == null)
            buttonImage = control.targetGraphic as Image;

        if (visualRoot == null && buttonImage != null)
            visualRoot = buttonImage.rectTransform;

        if (visualRoot == null)
            visualRoot = transform as RectTransform;

        baseScale = visualRoot != null ? visualRoot.localScale : Vector3.one;

        if (normalSprite == null && buttonImage != null)
            normalSprite = buttonImage.sprite;

        if (selectedSprite == null)
            selectedSprite = normalSprite;

        control.transition = Selectable.Transition.None;
    }

    private void OnEnable()
    {
        selected = EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject == gameObject;

        pointerHeld = false;
        keyboardHeld = false;
        externalHeld = false;
        pulseTime = 0f;

        RefreshVisuals();
    }

    private void Update()
    {
        if (selected && CanInteract)
            pulseTime += Time.unscaledDeltaTime;
        else
        {
            pulseTime = 0f;
            pointerHeld = false;
            keyboardHeld = false;
        }

        RefreshVisuals();
    }

    private void OnDisable()
    {
        selected = false;
        pointerHeld = false;
        keyboardHeld = false;
        externalHeld = false;
        pulseTime = 0f;

        RefreshVisuals();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            pointerHeld = false;
            keyboardHeld = false;
            externalHeld = false;
            RefreshVisuals();
        }
    }

    public void SetKeyboardHeld(bool held)
    {
        keyboardHeld = held && CanInteract;
        RefreshVisuals();
    }

    public void SetExternalHeld(bool held)
    {
        externalHeld = held && CanInteract;
        RefreshVisuals();
    }
    public void OnSelect(BaseEventData eventData)
    {
        selected = true;
        pulseTime = 0f;

        RefreshVisuals();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        selected = false;
        pointerHeld = false;
        keyboardHeld = false;

        RefreshVisuals();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!CanInteract || EventSystem.current == null)
            return;

        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            return;

        PongUINavigation navigation =
            EventSystem.current.GetComponent<PongUINavigation>();

        if (navigation != null && navigation.IsKeyboardPressActive)
            return;

        control.Select();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left ||
            !CanInteract)
            return;

        pointerHeld = true;
        RefreshVisuals();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        pointerHeld = false;
        RefreshVisuals();
    }

    private void RefreshVisuals()
    {
        bool showSelected = selected && CanInteract;
        Sprite sprite = showSelected ? selectedSprite : normalSprite;

        if (buttonImage != null && buttonImage.overrideSprite != sprite)
            buttonImage.overrideSprite = sprite;

        if (visualRoot != null)
        {
            float wave = showSelected
                ? (1f - Mathf.Cos(pulseTime * Mathf.PI * 2f / pulsePeriod)) * 0.5f
                : 0f;

            visualRoot.localScale = baseScale * (1f + wave * pulseAmount);
        }

        if (pressedOverlay != null)
        {
            pressedOverlay.color = pressedColor;
            pressedOverlay.enabled = CanInteract && (pointerHeld || keyboardHeld || externalHeld);
        }
    }
}