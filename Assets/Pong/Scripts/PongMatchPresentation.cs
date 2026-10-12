using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PongMatchPresentation : MonoBehaviour
{
    [Header("Scene references")]
    [SerializeField] private PongGameManager gameManager;
    [SerializeField] private Camera gameCamera;
    [SerializeField] private Canvas canvas;
    [SerializeField] private PongUINavigation menuNavigation;
    [SerializeField] private PongSideSelectionUI sideSelection;
    [SerializeField] private CanvasGroup sidePanelGroup;
    [SerializeField] private RectTransform transitionLayer;

    [Header("Selection character UI Images (existing Images under SideSelectPanel)")]
    [FormerlySerializedAs("leftStandImage")]
    [SerializeField] private Image leftSelectionCharacterImage;
    [FormerlySerializedAs("rightStandImage")]
    [SerializeField] private Image rightSelectionCharacterImage;

    [Header("Selection elements (children travel with their parent)")]
    [SerializeField] private RectTransform[] exitUp = new RectTransform[5];
    [SerializeField] private RectTransform[] exitDown = new RectTransform[2];
    [SerializeField] private Graphic[] fadeOut = new Graphic[6];

    [Header("Gameplay presentation")]
    [SerializeField] private RectTransform scoreBarRoot;
    [SerializeField] private SpriteRenderer leftGoalVisual;
    [SerializeField] private SpriteRenderer rightGoalVisual;
    [SerializeField] private PongBallPresentation ballPresentation;
    [Tooltip("Optional. An above-ground marker overrides the automatically calculated drop height.")]
    [SerializeField] private Transform ballDropStart;
    [SerializeField] private RectTransform servePromptRoot;
    [SerializeField] private Image[] modeIcons = new Image[2];
    [SerializeField] private Sprite legacyModeIcon;
    [SerializeField] private Sprite timedModeIcon;

    private sealed class ExitElement
    {
        public RectTransform rect;
        public Vector2 rest;
        public Vector2 outside;
    }

    private struct CharacterPose
    {
        public Vector2 center;
        public Vector2 size;
        public Vector3 scale;
        public Sprite sprite;
        public Color color;
    }

    private sealed class SelectionCharacterState
    {
        private readonly Image image;
        private readonly Transform parent;
        private readonly int siblingIndex;
        private readonly Vector2 anchorMin;
        private readonly Vector2 anchorMax;
        private readonly Vector2 pivot;
        private readonly Vector2 sizeDelta;
        private readonly Vector3 anchoredPosition;
        private readonly Vector3 scale;
        private readonly Quaternion rotation;
        private readonly Sprite sprite;
        private readonly Sprite overrideSprite;
        private readonly Color color;
        private readonly Image.Type type;
        private readonly bool preserveAspect;
        private readonly bool useSpriteMesh;
        private readonly bool raycastTarget;
        private readonly bool enabled;
        private readonly bool active;

        public SelectionCharacterState(Image source)
        {
            image = source;
            RectTransform rect = source.rectTransform;
            parent = rect.parent;
            siblingIndex = rect.GetSiblingIndex();
            anchorMin = rect.anchorMin;
            anchorMax = rect.anchorMax;
            pivot = rect.pivot;
            sizeDelta = rect.sizeDelta;
            anchoredPosition = rect.anchoredPosition3D;
            scale = rect.localScale;
            rotation = rect.localRotation;
            sprite = source.sprite;
            // Image.overrideSprite returns the active sprite even when no override is assigned.
            overrideSprite = source.overrideSprite != sprite ? source.overrideSprite : null;
            color = source.color;
            type = source.type;
            preserveAspect = source.preserveAspect;
            useSpriteMesh = source.useSpriteMesh;
            raycastTarget = source.raycastTarget;
            enabled = source.enabled;
            active = source.gameObject.activeSelf;
        }

        public void Restore()
        {
            if (image == null)
                return;

            image.enabled = false;
            RectTransform rect = image.rectTransform;
            rect.SetParent(parent, false);
            rect.SetSiblingIndex(siblingIndex);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition3D = anchoredPosition;
            rect.localScale = scale;
            rect.localRotation = rotation;
            image.overrideSprite = null;
            image.sprite = sprite;
            image.overrideSprite = overrideSprite;
            image.color = color;
            image.type = type;
            image.preserveAspect = preserveAspect;
            image.useSpriteMesh = useSpriteMesh;
            image.raycastTarget = raycastTarget;
            image.gameObject.SetActive(active);
            image.enabled = enabled;
        }
    }

    private ExitElement[] upper;
    private ExitElement[] lower;
    private Color[] fadeColors;
    private Color leftGoalColor;
    private Color rightGoalColor;
    private Vector2 scoreRest;
    private Vector2 scoreOutside;
    private Vector3 promptScale;
    private bool layoutCaptured;
    private SelectionCharacterState leftCharacterState;
    private SelectionCharacterState rightCharacterState;
    private float promptTime;
    private Coroutine entranceRoutine;
    private CharacterPose leftStart;
    private CharacterPose rightStart;
    private CharacterPose leftSelectionEnd;
    private CharacterPose rightSelectionEnd;
    private CharacterPose leftReadyEnd;
    private CharacterPose rightReadyEnd;

    public bool IsPlaying { get; private set; }
    private PongSettings Settings => gameManager.settings;
    private RectTransform CanvasRect => (RectTransform)canvas.transform;

    public bool ValidateReferences()
    {
        var missing = new List<string>();
        if (gameManager == null) missing.Add("Game Manager");
        if (gameCamera == null) missing.Add("Game Camera");
        if (canvas == null) missing.Add("Canvas");
        if (menuNavigation == null) missing.Add("Menu Navigation");
        if (sideSelection == null) missing.Add("Side Selection");
        if (sidePanelGroup == null) missing.Add("Side Panel Group");
        if (transitionLayer == null) missing.Add("Transition Layer");
        if (leftSelectionCharacterImage == null || leftSelectionCharacterImage.overrideSprite == null)
            missing.Add("Left Selection Character Image (with Sprite)");
        if (rightSelectionCharacterImage == null || rightSelectionCharacterImage.overrideSprite == null)
            missing.Add("Right Selection Character Image (with Sprite)");
        ValidateElements(exitUp, "Exit Up", missing);
        ValidateElements(exitDown, "Exit Down", missing);
        ValidateElements(fadeOut, "Fade Out", missing);
        if (scoreBarRoot == null) missing.Add("Score Bar Root");
        if (leftGoalVisual == null) missing.Add("Left Goal Visual");
        if (rightGoalVisual == null) missing.Add("Right Goal Visual");
        if (ballPresentation == null || !ballPresentation.ReferencesAssigned)
            missing.Add("Ball Presentation (HeightRoot and Shadow must be direct children of Ball)");
        else if (!ballPresentation.enabled)
            missing.Add("enabled Ball Presentation");
        if (servePromptRoot == null) missing.Add("Serve Prompt Root");
        ValidateElements(modeIcons, "Mode Icons", missing);
        if (legacyModeIcon == null) missing.Add("Legacy Mode Icon Sprite");
        if (timedModeIcon == null) missing.Add("Timed Mode Icon Sprite");
        if (gameManager != null && (gameManager.leftPaddleVisuals == null ||
            gameManager.rightPaddleVisuals == null || gameManager.leftPaddleController == null ||
            gameManager.rightPaddleController == null || gameManager.leftPaddleVisuals.ReadyRenderer == null ||
            gameManager.rightPaddleVisuals.ReadyRenderer == null))
            missing.Add("Game Manager's left/right controllers and Ready SpriteRenderers");
        if (gameManager != null && (gameManager.ball == null || gameManager.settings == null))
            missing.Add("Game Manager's Ball and Settings");

        if (missing.Count > 0)
        {
            Debug.LogError("PongMatchPresentation: assign " + string.Join(", ", missing) + ".", this);
            return false;
        }
        if (canvas.renderMode != RenderMode.ScreenSpaceCamera || canvas.worldCamera != gameCamera ||
            !gameCamera.orthographic ||
            ballPresentation.gameObject != gameManager.ball.gameObject ||
            sidePanelGroup.gameObject != gameManager.sideSelectPanel ||
            leftSelectionCharacterImage == rightSelectionCharacterImage ||
            !leftSelectionCharacterImage.transform.IsChildOf(sidePanelGroup.transform) ||
            !rightSelectionCharacterImage.transform.IsChildOf(sidePanelGroup.transform) ||
            !transitionLayer.IsChildOf(canvas.transform) ||
            transitionLayer.IsChildOf(sidePanelGroup.transform))
        {
            Debug.LogError("PongMatchPresentation: use the same orthographic Camera for Canvas and gameplay; " +
                "assign the two existing character UI Images under SideSelectPanel, " +
                "put TransitionLayer under Canvas outside SideSelectPanel, " +
                "Ball Presentation on Ball, and Side Panel Group on SideSelectPanel.", this);
            return false;
        }
        return true;
    }

    private static void ValidateElements<T>(T[] elements, string name, List<string> missing)
        where T : Object
    {
        if (elements == null || elements.Length == 0)
        {
            missing.Add(name);
            return;
        }
        for (int i = 0; i < elements.Length; i++)
            if (elements[i] == null) missing.Add(name + "[" + i + "]");
    }

    // Called before enabling GameplayRoot, so no Ready character or ball appears for one frame.
    public void PrepareEntrance()
    {
        IsPlaying = true;
        sideSelection.LockForEntrance();
        menuNavigation.SetExclusiveScreenInput(true);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
        sidePanelGroup.interactable = false;
        sidePanelGroup.blocksRaycasts = false;

        gameManager.leftPaddleController.SetInputLocked(true);
        gameManager.rightPaddleController.SetInputLocked(true);
        gameManager.leftPaddleController.ResetToStartPosition();
        gameManager.rightPaddleController.ResetToStartPosition();
        gameManager.leftPaddleVisuals.SetPresentationHidden(true);
        gameManager.rightPaddleVisuals.SetPresentationHidden(true);
        ballPresentation.ConfigurePaddles(gameManager.leftPaddleController, gameManager.rightPaddleController);
        ballPresentation.SetEntranceVisible(false);

        gameManager.hudPanel.SetActive(true);
        Canvas.ForceUpdateCanvases();
        CaptureLayout();
        upper = CaptureExitElements(exitUp, true);
        lower = CaptureExitElements(exitDown, false);
        scoreOutside = OutsidePosition(scoreBarRoot, true);
        scoreBarRoot.anchoredPosition = scoreOutside;
        servePromptRoot.gameObject.SetActive(false);
        Sprite modeSprite = gameManager.gameMode == GameMode.Legacy ? legacyModeIcon : timedModeIcon;
        foreach (Image icon in modeIcons)
        {
            icon.overrideSprite = null;
            icon.sprite = modeSprite;
        }
        SetGoalAlpha(0f);

        transitionLayer.gameObject.SetActive(true);
        transitionLayer.SetAsLastSibling();
        // Capture every entrance, before reparenting or changing the Images. This also keeps
        // any layout or sprite edits made between matches as the next selection's rest state.
        leftCharacterState = new SelectionCharacterState(leftSelectionCharacterImage);
        rightCharacterState = new SelectionCharacterState(rightSelectionCharacterImage);
        leftStart = ImagePose(leftSelectionCharacterImage);
        rightStart = ImagePose(rightSelectionCharacterImage);
        leftReadyEnd = RendererPose(gameManager.leftPaddleVisuals.ReadyRenderer);
        rightReadyEnd = RendererPose(gameManager.rightPaddleVisuals.ReadyRenderer);
        leftSelectionEnd = SelectionAtReadyFeet(leftStart, leftReadyEnd);
        rightSelectionEnd = SelectionAtReadyFeet(rightStart, rightReadyEnd);
        SetupSelectionCharacter(leftSelectionCharacterImage, leftStart);
        SetupSelectionCharacter(rightSelectionCharacterImage, rightStart);
    }

    private void CaptureLayout()
    {
        if (layoutCaptured)
            return;
        scoreRest = scoreBarRoot.anchoredPosition;
        promptScale = servePromptRoot.localScale;
        leftGoalColor = leftGoalVisual.color;
        rightGoalColor = rightGoalVisual.color;
        fadeColors = new Color[fadeOut.Length];
        for (int i = 0; i < fadeOut.Length; i++)
            fadeColors[i] = fadeOut[i].color;
        layoutCaptured = true;
    }

    public void PlayEntrance()
    {
        entranceRoutine = StartCoroutine(EntranceRoutine());
    }

    private IEnumerator EntranceRoutine()
    {
        float duration = Mathf.Max(Settings.menuExitDuration, Settings.characterTravelDuration,
            Settings.selectionGlowFadeDuration, Settings.scoreboardEntranceDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            AnimateDeparture(elapsed);
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }
        AnimateDeparture(duration);
        gameManager.sideSelectPanel.SetActive(false);
        RestoreSelectionLayout();

        float hopDuration = Settings.characterEntranceHopUp + Settings.characterEntranceFlipDuration +
            Settings.characterEntranceHopDown;
        float bounceDuration = Settings.ballEntranceBounceDuration;
        float ballDuration = Settings.ballEntranceDropDuration + bounceDuration * 1.7f;
        duration = Mathf.Max(hopDuration, ballDuration, Settings.goalEntranceFlickerDuration);
        float depth = Vector3.Dot(gameManager.ball.transform.position - gameCamera.transform.position,
            gameCamera.transform.forward);
        float dropHeight = gameCamera.ViewportToWorldPoint(new Vector3(0.5f, 1f, depth)).y -
            gameManager.ball.ServePosition.y + 0.75f;
        if (ballDropStart != null && ballDropStart.position.y > gameManager.ball.ServePosition.y)
            dropHeight = ballDropStart.position.y - gameManager.ball.ServePosition.y;
        ballPresentation.BeginEntrance(dropHeight);

        bool swapFrameShown = false;
        bool handedOff = false;
        elapsed = 0f;
        while (elapsed < duration)
        {
            if (!handedOff)
            {
                AnimateHop(elapsed, ref swapFrameShown);
                if (elapsed >= hopDuration)
                {
                    HandOffCharacters();
                    handedOff = true;
                }
            }
            ballPresentation.SetEntranceHeight(BallHeight(elapsed, dropHeight));
            AnimateGoals(elapsed);
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }
        if (!handedOff)
            HandOffCharacters();
        ballPresentation.EndEntrance();
        SetGoalAlpha(1f);
        scoreBarRoot.anchoredPosition = scoreRest;
        transitionLayer.gameObject.SetActive(false);
        menuNavigation.SetExclusiveScreenInput(false);
        gameManager.leftPaddleController.SetInputLocked(false);
        gameManager.rightPaddleController.SetInputLocked(false);
        entranceRoutine = null;
        IsPlaying = false;
        gameManager.CompleteMatchEntrance();
    }

    private void AnimateDeparture(float elapsed)
    {
        float exitProgress = Ease(elapsed / Mathf.Max(0.01f, Settings.menuExitDuration));
        MoveElements(upper, exitProgress);
        MoveElements(lower, exitProgress);
        float fade = 1f - Mathf.Clamp01(elapsed / Mathf.Max(0.01f, Settings.selectionGlowFadeDuration));
        for (int i = 0; i < fadeOut.Length; i++)
        {
            Color color = fadeColors[i];
            color.a *= fade;
            fadeOut[i].color = color;
        }
        float travel = Ease(elapsed / Mathf.Max(0.01f, Settings.characterTravelDuration));
        ApplyPose(leftSelectionCharacterImage, LerpPose(leftStart, leftSelectionEnd, travel));
        ApplyPose(rightSelectionCharacterImage, LerpPose(rightStart, rightSelectionEnd, travel));
        scoreBarRoot.anchoredPosition = Vector2.Lerp(scoreOutside, scoreRest,
            Ease(elapsed / Mathf.Max(0.01f, Settings.scoreboardEntranceDuration)));
    }

    private void AnimateHop(float elapsed, ref bool swapFrameShown)
    {
        float up = Mathf.Max(0.01f, Settings.characterEntranceHopUp);
        float flip = Mathf.Max(0.01f, Settings.characterEntranceFlipDuration);
        float down = Mathf.Max(0.01f, Settings.characterEntranceHopDown);
        float height;
        float angle = 0f;
        bool ready = elapsed >= up + flip * 0.5f;
        if (elapsed < up)
            height = Settings.characterEntranceHopHeight * (1f - Mathf.Pow(1f - elapsed / up, 2f));
        else if (elapsed < up + flip)
        {
            height = Settings.characterEntranceHopHeight;
            float t = Mathf.Clamp01((elapsed - up) / flip);
            angle = t < 0.5f ? t * 180f : (1f - t) * 180f;
        }
        else
            height = Settings.characterEntranceHopHeight *
                (1f - Mathf.Pow(Mathf.Clamp01((elapsed - up - flip) / down), 2f));

        if (ready && !swapFrameShown)
        {
            // Force one edge-on frame even if the display frame skipped the exact 90-degree instant.
            angle = 90f;
            swapFrameShown = true;
        }
        CharacterPose leftPose = ready ? leftReadyEnd : leftSelectionEnd;
        CharacterPose rightPose = ready ? rightReadyEnd : rightSelectionEnd;
        leftPose.center += WorldUpOffset(height);
        rightPose.center += WorldUpOffset(height);
        ApplyPose(leftSelectionCharacterImage, leftPose);
        ApplyPose(rightSelectionCharacterImage, rightPose);
        // Orthographic Y rotation projects to horizontal compression. Keeping UI on its plane
        // avoids the rotating sprite crossing the Screen Space Camera's near clipping plane.
        float width = angle == 90f ? 0f : Mathf.Cos(angle * Mathf.Deg2Rad);
        leftSelectionCharacterImage.rectTransform.localScale = new Vector3(leftPose.scale.x * width, leftPose.scale.y, 1f);
        rightSelectionCharacterImage.rectTransform.localScale = new Vector3(rightPose.scale.x * width, rightPose.scale.y, 1f);
    }

    private float BallHeight(float elapsed, float dropHeight)
    {
        return EntranceBallHeight(elapsed, dropHeight, Settings.ballEntranceDropDuration,
            Settings.ballEntranceBounceDuration, Settings.ballEntranceBounceHeight);
    }

    internal static float EntranceBallHeight(float elapsed, float dropHeight, float dropDuration,
        float bounceDuration, float bounceHeight)
    {
        float drop = Mathf.Max(0.01f, dropDuration);
        if (elapsed < drop)
            return dropHeight * (1f - Mathf.Pow(Mathf.Clamp01(elapsed / drop), 2f));
        float bounce = Mathf.Max(0.01f, bounceDuration);
        float t = elapsed - drop;
        if (t < bounce)
            return bounceHeight * Mathf.Sin(Mathf.PI * t / bounce);
        t -= bounce;
        if (t < bounce * 0.7f)
            return bounceHeight * 0.4f * Mathf.Sin(Mathf.PI * t / (bounce * 0.7f));
        return 0f;
    }

    private void AnimateGoals(float elapsed)
    {
        float duration = Mathf.Max(0.01f, Settings.goalEntranceFlickerDuration);
        if (elapsed >= duration)
        {
            SetGoalAlpha(1f);
            return;
        }
        int step = Mathf.FloorToInt(elapsed / duration * 9f);
        float alpha = step == 0 || step == 2 || step == 5 || step >= 7 ? 1f : 0.08f;
        SetGoalAlpha(alpha);
    }

    private void HandOffCharacters()
    {
        // The landing Images use the same Ready sprites/geometry as these gameplay renderers.
        gameManager.leftPaddleVisuals.SetPresentationHidden(false);
        gameManager.rightPaddleVisuals.SetPresentationHidden(false);
        // SideSelectPanel is already inactive, so returning the original Images to it hides
        // them without a duplicate frame and leaves them ready for the next menu opening.
        RestoreSelectionCharacters();
    }

    private void SetGoalAlpha(float alpha)
    {
        Color left = leftGoalColor;
        Color right = rightGoalColor;
        left.a *= alpha;
        right.a *= alpha;
        leftGoalVisual.color = left;
        rightGoalVisual.color = right;
    }

    private ExitElement[] CaptureExitElements(RectTransform[] rects, bool upward)
    {
        var entries = new ExitElement[rects.Length];
        for (int i = 0; i < rects.Length; i++)
            entries[i] = new ExitElement { rect = rects[i], rest = rects[i].anchoredPosition,
                outside = OutsidePosition(rects[i], upward) };
        return entries;
    }

    private Vector2 OutsidePosition(RectTransform rect, bool upward)
    {
        // Include children: ScoreBarVisual can be much taller than ScoreBarRoot's transparent Rect.
        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(CanvasRect, rect);
        float delta = upward ? CanvasRect.rect.yMax - bounds.min.y + Settings.entranceScreenMargin
            : CanvasRect.rect.yMin - bounds.max.y - Settings.entranceScreenMargin;
        Vector3 world = CanvasRect.TransformVector(Vector3.up * delta);
        Vector3 parentDelta = rect.parent.InverseTransformVector(world);
        return rect.anchoredPosition + new Vector2(parentDelta.x, parentDelta.y);
    }

    private static void MoveElements(ExitElement[] elements, float progress)
    {
        foreach (ExitElement element in elements)
            element.rect.anchoredPosition = Vector2.Lerp(element.rest, element.outside, progress);
    }

    private void RestoreSelectionLayout()
    {
        if (upper != null) MoveElements(upper, 0f);
        if (lower != null) MoveElements(lower, 0f);
        for (int i = 0; i < fadeOut.Length; i++)
            fadeOut[i].color = fadeColors[i];
        sidePanelGroup.interactable = true;
        sidePanelGroup.blocksRaycasts = true;
    }

    private void RestoreSelectionCharacters()
    {
        leftCharacterState?.Restore();
        rightCharacterState?.Restore();
        leftCharacterState = null;
        rightCharacterState = null;
    }

    public void ResetPresentation()
    {
        if (entranceRoutine != null)
            StopCoroutine(entranceRoutine);
        entranceRoutine = null;
        IsPlaying = false;
        RestoreSelectionCharacters();
        if (!layoutCaptured)
            return;
        RestoreSelectionLayout();
        scoreBarRoot.anchoredPosition = scoreRest;
        servePromptRoot.localScale = promptScale;
        servePromptRoot.gameObject.SetActive(false);
        transitionLayer.gameObject.SetActive(false);
        menuNavigation.SetExclusiveScreenInput(false);
        gameManager.leftPaddleVisuals.SetPresentationHidden(false);
        gameManager.rightPaddleVisuals.SetPresentationHidden(false);
        gameManager.leftPaddleController.SetInputLocked(false);
        gameManager.rightPaddleController.SetInputLocked(false);
        ballPresentation.EndEntrance();
        SetGoalAlpha(1f);
    }

    private void LateUpdate()
    {
        if (servePromptRoot == null || gameManager == null || !layoutCaptured)
            return;
        bool visible = !IsPlaying && gameManager.CanManuallyServe;
        servePromptRoot.gameObject.SetActive(visible);
        if (visible)
        {
            promptTime += Time.unscaledDeltaTime;
            float wave = (1f - Mathf.Cos(promptTime * Mathf.PI * 2f /
                Mathf.Max(0.1f, Settings.servePromptPulsePeriod))) * 0.5f;
            servePromptRoot.localScale = promptScale * (1f + wave * Settings.servePromptPulseAmount);
        }
        else
        {
            promptTime = 0f;
            servePromptRoot.localScale = promptScale;
        }
    }

    private CharacterPose ImagePose(Image image)
    {
        Sprite sprite = image.overrideSprite != null ? image.overrideSprite : image.sprite;
        Rect rect = image.GetPixelAdjustedRect();
        if (image.preserveAspect && sprite != null)
        {
            float ratio = sprite.rect.width / sprite.rect.height;
            if (ratio > rect.width / rect.height)
            {
                float oldHeight = rect.height;
                rect.height = rect.width / ratio;
                rect.y += (oldHeight - rect.height) * image.rectTransform.pivot.y;
            }
            else
            {
                float oldWidth = rect.width;
                rect.width = rect.height * ratio;
                rect.x += (oldWidth - rect.width) * image.rectTransform.pivot.x;
            }
        }
        return PoseFromWorldRect(image.rectTransform, rect, sprite, image.color, false, false);
    }

    private CharacterPose RendererPose(SpriteRenderer renderer)
    {
        Sprite sprite = renderer.sprite;
        Rect rect = new Rect(-sprite.pivot / sprite.pixelsPerUnit, sprite.rect.size / sprite.pixelsPerUnit);
        return PoseFromWorldRect(renderer.transform, rect, sprite, renderer.color, renderer.flipX, renderer.flipY);
    }

    private CharacterPose PoseFromWorldRect(Transform source, Rect rect, Sprite sprite, Color color,
        bool flipX, bool flipY)
    {
        Vector3 centerLocal = rect.center;
        if (flipX) centerLocal.x *= -1f;
        if (flipY) centerLocal.y *= -1f;
        Vector2 center = ProxyPoint(source.TransformPoint(centerLocal));
        Vector2 right = ProxyPoint(source.TransformPoint(centerLocal +
            Vector3.right * rect.width * (flipX ? -0.5f : 0.5f)));
        Vector2 top = ProxyPoint(source.TransformPoint(centerLocal +
            Vector3.up * rect.height * (flipY ? -0.5f : 0.5f)));
        return new CharacterPose { center = center,
            size = new Vector2(Vector2.Distance(center, right) * 2f, Vector2.Distance(center, top) * 2f),
            scale = new Vector3(right.x < center.x ? -1f : 1f, top.y < center.y ? -1f : 1f, 1f),
            sprite = sprite, color = color };
    }

    private Vector2 ProxyPoint(Vector3 worldPoint)
    {
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(gameCamera, worldPoint);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(transitionLayer, screen, gameCamera, out Vector2 point);
        return point;
    }

    private Vector2 WorldUpOffset(float height) => ProxyPoint(Vector3.up * height) - ProxyPoint(Vector3.zero);

    private static CharacterPose SelectionAtReadyFeet(CharacterPose selection, CharacterPose ready)
    {
        CharacterPose pose = selection;
        pose.scale = ready.scale;
        pose.size = new Vector2(ready.size.x * selection.sprite.rect.width / ready.sprite.rect.width,
            ready.size.y * selection.sprite.rect.height / ready.sprite.rect.height);
        pose.center = ready.center + Vector2.up * (pose.size.y - ready.size.y) * 0.5f;
        return pose;
    }

    private void SetupSelectionCharacter(Image image, CharacterPose pose)
    {
        // Moving only the character Image leaves its glow, badge, arrow and outline in
        // SideSelectPanel, where they can exit/fade independently of the character.
        image.rectTransform.SetParent(transitionLayer, false);
        image.gameObject.SetActive(true);
        image.enabled = true;
        image.type = Image.Type.Simple;
        image.preserveAspect = false; // Pose already includes the original Image's aspect-fit rectangle.
        image.useSpriteMesh = false;
        image.raycastTarget = false;
        image.overrideSprite = null;
        image.rectTransform.anchorMin = Vector2.one * 0.5f;
        image.rectTransform.anchorMax = Vector2.one * 0.5f;
        image.rectTransform.pivot = Vector2.one * 0.5f;
        ApplyPose(image, pose);
    }

    private static void ApplyPose(Image image, CharacterPose pose)
    {
        image.sprite = pose.sprite;
        image.color = pose.color;
        image.rectTransform.localPosition = new Vector3(pose.center.x, pose.center.y, 0f);
        image.rectTransform.sizeDelta = pose.size;
        image.rectTransform.localScale = pose.scale;
        image.rectTransform.localRotation = Quaternion.identity;
    }

    private static CharacterPose LerpPose(CharacterPose start, CharacterPose end, float progress)
    {
        CharacterPose pose = start;
        pose.center = Vector2.Lerp(start.center, end.center, progress);
        pose.size = Vector2.Lerp(start.size, end.size, progress);
        // Both source and destination use the same side's mirroring; avoid flipping twice.
        pose.scale = end.scale;
        return pose;
    }

    private static float Ease(float progress)
    {
        progress = Mathf.Clamp01(progress);
        return progress * progress * (3f - 2f * progress);
    }
}
