using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_InputField))]
public class PongLegacyScoreInput : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PongGameManager gameManager;
    [Tooltip("The Image on LegacyScoreInput/Visual, not WinningScoreFrame.")]
    [SerializeField] private Image fieldImage;
    [Tooltip("Select PLAY after Enter, Escape, or clicking empty background.")]
    [SerializeField] private Selectable afterEditSelection;

    [Header("Input field sprites")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite focusedSprite;

    private TMP_InputField input;
    private Canvas canvas;
    private bool returnFocusAfterEdit;
    private bool initialized;

    private void Awake()
    {
        input = GetComponent<TMP_InputField>();
        if (gameManager == null || fieldImage == null ||
            normalSprite == null || focusedSprite == null)
        {
            Debug.LogError("PongLegacyScoreInput: assign Game Manager, the input's Visual Image, " +
                "and both gray/blue input sprites.", this);
            enabled = false;
            return;
        }

        canvas = fieldImage.canvas;
        input.transition = Selectable.Transition.None;
        input.contentType = TMP_InputField.ContentType.Custom;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.characterValidation = TMP_InputField.CharacterValidation.Digit;
        input.characterLimit = 3;
        input.richText = false;
        input.shouldActivateOnSelect = true;
        input.resetOnDeActivation = true;
        input.restoreOriginalTextOnEscape = true;
        initialized = true;
    }

    private void OnEnable()
    {
        if (!initialized)
            return;

        returnFocusAfterEdit = false;
        input.onEndEdit.AddListener(OnEndEdit);
        RefreshVisuals();
    }

    private void OnDisable()
    {
        if (input != null)
            input.onEndEdit.RemoveListener(OnEndEdit);
        returnFocusAfterEdit = false;
        if (fieldImage != null)
            fieldImage.overrideSprite = normalSprite;
    }

    private void LateUpdate()
    {
        if (!initialized)
            return;

        // This scene's Input System module keeps selection on background clicks.
        // Explicitly end the edit on a click outside, including non-selectable artwork.
        Mouse mouse = Mouse.current;
        if (input.isFocused && mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            if (!RectTransformUtility.RectangleContainsScreenPoint(
                fieldImage.rectTransform, mouse.position.ReadValue(), eventCamera))
                input.DeactivateInputField();
        }

        if (returnFocusAfterEdit && !input.isFocused)
        {
            EventSystem events = EventSystem.current;
            // Native OnDeselect can invoke onEndEdit while EventSystem is changing
            // selection. Wait until that finishes, and preserve any clicked button.
            if (events == null || !events.alreadySelecting)
            {
                returnFocusAfterEdit = false;
                if (events != null && events.currentSelectedGameObject == gameObject)
                {
                    if (afterEditSelection != null && afterEditSelection != input &&
                        afterEditSelection.IsActive() && afterEditSelection.IsInteractable())
                        afterEditSelection.Select();
                    else
                        events.SetSelectedGameObject(null);
                }
            }
        }

        RefreshVisuals();
    }

    private void OnEndEdit(string value)
    {
        if (input.wasCanceled)
            gameManager.ResetLegacyScoreInput();
        else
            gameManager.SubmitLegacyScore(value);

        returnFocusAfterEdit = true;
    }

    private void RefreshVisuals()
    {
        bool selected = EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject == gameObject;
        bool highlighted = input.IsInteractable() && (input.isFocused || selected);
        fieldImage.overrideSprite = highlighted ? focusedSprite : normalSprite;
    }
}
