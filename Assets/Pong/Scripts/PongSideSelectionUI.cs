using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PongSideSelectionUI : MonoBehaviour
{
    [Serializable]
    public class SideView
    {
        public Image glowImage;
        public GameObject readyOutline;
        public Image controlKeysImage;
        public GameObject readyWord;
        public RectTransform selectionArrow;

        [NonSerialized] public Vector2 arrowPosition;
    }

    [SerializeField] private PongGameManager gameManager;
    [SerializeField] private PongUINavigation menuNavigation;
    [SerializeField] private SideView left = new SideView();
    [SerializeField] private SideView right = new SideView();
    [SerializeField] private Sprite wasdKeysSprite;
    [SerializeField] private Sprite arrowKeysSprite;

    // Optional: Escape also works when no Back button is assigned.
    [SerializeField] private Button backButton;
    [Tooltip("May reuse the Game Manager's Quick Countdown Text; leave empty to use it automatically.")]
    [SerializeField] private TMP_Text readyCountdownText;
    [SerializeField, Range(0f, 1f)] private float idleGlowAlpha = 0.35f;
    [SerializeField, Range(0f, 1f)] private float readyGlowAlpha = 1f;
    [SerializeField, Min(0f)] private float arrowDistance = 6f;
    [SerializeField, Min(0.1f)] private float arrowPeriod = 0.8f;

    private PongUIFeedback backFeedback;
    private bool wasdOnLeft;
    private bool wasdReady;
    private bool arrowsReady;
    private bool wasdPress;
    private bool arrowsPress;
    private bool backPress;
    private bool inputArmed;
    private bool leaving;
    private bool sessionOpen;
    private Coroutine readyCountdownRoutine;
    private Vector3 countdownBaseScale;
    private Color countdownBaseColor;

    private TMP_Text CountdownText => readyCountdownText != null
        ? readyCountdownText : gameManager != null ? gameManager.quickCountdownText : null;

    private void OnEnable()
    {
        if (!ReferencesAssigned())
        {
            Debug.LogError(
                "PongSideSelectionUI: assign Game Manager, Menu Navigation, " +
                "key sprites, and every Left/Right reference.", this);
            enabled = false;
            return;
        }

        backFeedback = backButton != null
            ? backButton.GetComponent<PongUIFeedback>() : null;
        left.arrowPosition = left.selectionArrow.anchoredPosition;
        right.arrowPosition = right.selectionArrow.anchoredPosition;
        sessionOpen = true;
        countdownBaseScale = CountdownText.rectTransform.localScale;
        countdownBaseColor = CountdownText.color;
        PongCountdownVisual.Reset(CountdownText, countdownBaseScale, countdownBaseColor);

        wasdOnLeft = true;
        wasdReady = false;
        arrowsReady = false;
        leaving = false;
        inputArmed = false;
        CancelPresses();
        menuNavigation.SetExclusiveScreenInput(true);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        Refresh();
    }

    private bool ReferencesAssigned()
    {
        return gameManager != null && menuNavigation != null &&
            gameManager.settings != null && CountdownText != null &&
            wasdKeysSprite != null && arrowKeysSprite != null &&
            Assigned(left) && Assigned(right);
    }

    private static bool Assigned(SideView view)
    {
        return view != null && view.glowImage != null &&
            view.readyOutline != null && view.controlKeysImage != null &&
            view.readyWord != null && view.selectionArrow != null;
    }

    private void OnDisable()
    {
        if (!sessionOpen)
            return;
        sessionOpen = false;
        CancelReadyCountdown();
        CancelPresses();
        RestoreArrow(left);
        RestoreArrow(right);

        if (menuNavigation != null && (gameManager == null || !gameManager.IsMatchIntroActive))
            menuNavigation.SetExclusiveScreenInput(false);

        if (EventSystem.current != null)
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (selected != null && selected.transform.IsChildOf(transform))
                EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!sessionOpen || leaving)
            return;
        if (!hasFocus)
        {
            CancelReadyCountdown();
            CancelPresses();
            inputArmed = false;
            wasdReady = false;
            arrowsReady = false;
            if (sessionOpen && !leaving)
                Refresh();
        }
    }

    private void Update()
    {
        if (leaving)
            return;

        AnimateArrow(left);
        AnimateArrow(right);

        Keyboard keys = Keyboard.current;
        if (keys == null)
        {
            CancelReadyCountdown();
            wasdReady = false;
            arrowsReady = false;
            Refresh();
            CancelPresses();
            inputArmed = false;
            return;
        }

        bool wsHeld = keys.spaceKey.isPressed;
        bool arrowsHeld = keys.enterKey.isPressed || keys.numpadEnterKey.isPressed;
        bool mouseHeld = Mouse.current != null && Mouse.current.leftButton.isPressed;

        // Ignore the press used to open this panel, and cancel on mouse takeover.
        if (!inputArmed || mouseHeld)
        {
            if (mouseHeld && readyCountdownRoutine != null)
            {
                CancelReadyCountdown();
                wasdReady = false;
                arrowsReady = false;
                Refresh();
            }
            CancelPresses();
            inputArmed = !mouseHeld && !wsHeld && !arrowsHeld &&
                !keys.escapeKey.isPressed;
            return;
        }

        if (keys.escapeKey.wasPressedThisFrame)
        {
            CancelReadyCountdown();
            CancelPresses();
            backPress = true;
        }

        if (backPress)
        {
            UpdateHeldVisuals();
            if (!keys.escapeKey.isPressed)
                Back();
            return;
        }

        int wsMove = (keys.dKey.wasPressedThisFrame ? 1 : 0) -
            (keys.aKey.wasPressedThisFrame ? 1 : 0);
        int arrowsMove = (keys.rightArrowKey.wasPressedThisFrame ? 1 : 0) -
            (keys.leftArrowKey.wasPressedThisFrame ? 1 : 0);
        bool conflictingMoves = wsMove != 0 && arrowsMove != 0 &&
            (wsMove < 0) != (arrowsMove > 0);

        if (!conflictingMoves)
        {
            bool requestedLeft = wsMove != 0 ? wsMove < 0 : arrowsMove > 0;
            if ((wsMove != 0 || arrowsMove != 0) && requestedLeft != wasdOnLeft)
            {
                wasdOnLeft = requestedLeft;
                CancelReadyCountdown();
                wasdReady = false;
                arrowsReady = false;
                CancelPresses();
                inputArmed = false;
                Refresh();
                return;
            }
        }

        // Ready input no longer depends on a UI Button.
        if (keys.spaceKey.wasPressedThisFrame)
        {
            CancelReadyCountdown();
            wasdPress = true;
        }

        if (keys.enterKey.wasPressedThisFrame || keys.numpadEnterKey.wasPressedThisFrame)
        {
            CancelReadyCountdown();
            arrowsPress = true;
        }

        UpdateHeldVisuals();

        if (wasdPress && !wsHeld)
        {
            wasdPress = false;
            UpdateHeldVisuals();
            ToggleReady(true);
            if (!isActiveAndEnabled || leaving)
                return;
        }

        if (arrowsPress && !arrowsHeld)
        {
            arrowsPress = false;
            UpdateHeldVisuals();
            ToggleReady(false);
        }
    }

    private void ToggleReady(bool isWasd)
    {
        if (leaving || !isActiveAndEnabled)
            return;

        if (isWasd)
            wasdReady = !wasdReady;
        else
            arrowsReady = !arrowsReady;

        Refresh();

        if (wasdReady && arrowsReady)
        {
            readyCountdownRoutine = StartCoroutine(ReadyCountdownRoutine());
        }
        else
            CancelReadyCountdown();
    }

    private IEnumerator ReadyCountdownRoutine()
    {
        yield return PongCountdownVisual.Play(CountdownText, gameManager.settings,
            gameManager.settings.sideReadyCountdownStep, countdownBaseScale, countdownBaseColor, true,
            gameManager.countdownVisuals);
        readyCountdownRoutine = null;

        // Update processes cancellations before the coroutine can commit the transition.
        if (!wasdReady || !arrowsReady || wasdPress || arrowsPress || backPress || !inputArmed)
            yield break;

        if (!gameManager.TryStartGameFromSideSelect(wasdOnLeft ? PaddleSide.Left : PaddleSide.Right))
        {
            wasdReady = false;
            arrowsReady = false;
            Refresh();
        }
    }

    private void CancelReadyCountdown()
    {
        if (readyCountdownRoutine != null)
            StopCoroutine(readyCountdownRoutine);
        readyCountdownRoutine = null;
        PongCountdownVisual.Reset(CountdownText, countdownBaseScale, countdownBaseColor);
    }

    public void LockForEntrance()
    {
        leaving = true;
        CancelReadyCountdown();
        CancelPresses();
        RestoreArrow(left);
        RestoreArrow(right);
    }

    public void Back()
    {
        if (leaving || !isActiveAndEnabled)
            return;
        leaving = true;
        CancelReadyCountdown();
        CancelPresses();
        gameManager.BackFromSideSelect();
    }

    private void Refresh()
    {
        RefreshSide(left, wasdOnLeft, wasdOnLeft ? wasdReady : arrowsReady);
        RefreshSide(right, !wasdOnLeft, wasdOnLeft ? arrowsReady : wasdReady);
        UpdateHeldVisuals();
    }

    private void RefreshSide(SideView view, bool isWasd, bool ready)
    {
        view.controlKeysImage.overrideSprite = null;
        view.controlKeysImage.sprite = isWasd ? wasdKeysSprite : arrowKeysSprite;
        view.readyOutline.SetActive(ready);
        view.readyWord.SetActive(ready);
        view.selectionArrow.gameObject.SetActive(!ready);

        // The character Image and all manually placed visual RectTransforms stay fixed.
        // Only the glow's alpha changes; its RGB color is kept.
        view.glowImage.gameObject.SetActive(true);
        Color glowColor = view.glowImage.color;
        glowColor.a = ready ? readyGlowAlpha : idleGlowAlpha;
        view.glowImage.color = glowColor;
    }

    private void UpdateHeldVisuals()
    {
        if (backFeedback != null)
            backFeedback.SetExternalHeld(backPress);
    }

    private void CancelPresses()
    {
        wasdPress = false;
        arrowsPress = false;
        backPress = false;
        UpdateHeldVisuals();
    }

    private void AnimateArrow(SideView view)
    {
        if (!view.selectionArrow.gameObject.activeSelf)
            return;
        float offset = Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / arrowPeriod) *
            arrowDistance;
        view.selectionArrow.anchoredPosition = view.arrowPosition + Vector2.up * offset;
    }

    private static void RestoreArrow(SideView view)
    {
        if (view != null && view.selectionArrow != null)
            view.selectionArrow.anchoredPosition = view.arrowPosition;
    }
}
