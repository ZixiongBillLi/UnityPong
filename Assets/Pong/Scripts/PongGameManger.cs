using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Serialization;

public enum GameMode
{
    Legacy,
    Timed
}

public class PongGameManager : MonoBehaviour
{
    public GameMode gameMode = GameMode.Legacy;


    public GameObject gameplayRoot;
    public GameObject hudPanel;

    public GameObject legacyCustomizePanel;

    public TMP_Text matchInfoText;
    public TMP_InputField legacyScoreInput;

    public GameObject mainMenuPanel;
    public GameObject modeSelectPanel;

    public TMP_Text currentLegacyScoreText;

    public Toggle quickModeToggle;
    public bool quickMode = true;


    public PongSettings settings;

    public float totalTime = 120f;
    public float remainingTime;

    public TMP_Text currentBallValueText;

    public PongBallController ball;

    public int player1Score = 0;
    public int player2Score = 0;

    public TMP_Text player1ScoreText;
    public TMP_Text player2ScoreText;

    public TMP_Text leftScoreText;
    public TMP_Text rightScoreText;

    [Header("Game over UI")]
    public GameObject gameOverPanel;
    public TMP_Text winnerText;
    public TMP_Text gameOverBlueScoreText;
    public TMP_Text gameOverOrangeScoreText;
    [Tooltip("PongUIFeedback on the Play Again button, for the held Space overlay.")]
    public PongUIFeedback gameOverPlayAgainFeedback;

    [Header("Game over text outlines")]
    public Color blueResultOutline = new Color(0f, 0.8f, 1f, 1f);
    public Color orangeResultOutline = new Color(1f, 0.55f, 0.05f, 1f);
    public Color drawResultOutline = new Color(0.65f, 0.7f, 0.75f, 1f);
    [Range(0f, 1f)] public float winnerOutlineWidth = 0.25f;
    [Range(0f, 1f)] public float finalScoreOutlineWidth = 0.18f;

    public InputActionAsset inputActions;

    private InputAction spawnAction;

    private bool gameOver = false;
    private bool gameOverRestartHeld;

    public PongPaddleVisuals player1Visuals;
    public PongPaddleVisuals player2Visuals;

    private bool goalPause = false;

    public PongTargetManager targetManager;

    [Tooltip("Image on LegacyCustomizePanel/ApplyCheckImage/Visual.")]
    public Image applySuccessCheck;

    private Coroutine applyFeedbackRoutine;

    public GameObject sideSelectPanel;


    private ControlScheme player1ControlScheme;
    private ControlScheme player2ControlScheme;

    private PaddleSide player1Side;
    private PaddleSide player2Side;

    public PongPaddleController leftPaddleController;
    public PongPaddleController rightPaddleController;

    public PongPaddleVisuals leftPaddleVisuals;
    public PongPaddleVisuals rightPaddleVisuals;









    public GameObject pauseMenuPanel;
    public GameObject returnToMenuConfirmPanel;
    private bool gamePaused = false;
    private InputAction pauseAction;


    [Header("Countdown visuals")]
    [Tooltip("Shared UI container under Canvas for Resume Countdown Text and Quick Countdown Text.")]
    public RectTransform countdownVisuals;
    public TMP_Text resumeCountdownText;

    private bool resumeCountdownActive = false;
    private Coroutine resumeCountdownRoutine;

    public TMP_Text quickCountdownText;

    private Vector3 quickCountdownBaseScale;
    private Color quickCountdownBaseColor;
    private Coroutine goalPauseRoutine;
    private Coroutine scoreFlashRoutine;

    private bool quickCountdownActive = false;
    private Coroutine quickCountdownRoutine;

    private bool firstQuickServe = true;

    private bool resumeShouldAutoServe = false;


    [FormerlySerializedAs("modeSelectPanel")]
    public GameObject timedSetupPanel;

    [Header("Match presentation")]
    public PongMatchPresentation matchPresentation;
    private bool matchIntroActive;
    public bool IsMatchIntroActive => matchIntroActive;
    public bool CanManuallyServe => gameplayRoot != null && gameplayRoot.activeInHierarchy &&
        !matchIntroActive && !gameOver && !goalPause && !gamePaused &&
        !resumeCountdownActive && !quickCountdownActive && Time.timeScale > 0f &&
        !(gameMode == GameMode.Timed && quickMode) && ball != null && ball.enabled && !ball.IsMoving();
    
    

    private void Start()
    {
        // Existing scenes may still serialize the old false value. Set the startup default
        // once, then keep the player's Toggle choice while moving between menu panels.
        quickMode = true;
        if (quickModeToggle != null)
            quickModeToggle.SetIsOnWithoutNotify(true);

        InputActionMap pongMap = inputActions.FindActionMap("Pong");
        spawnAction = pongMap.FindAction("Spawn");
        spawnAction.Enable();

        PongCountdownVisual.PrepareLayer(countdownVisuals);
        if (resumeCountdownText != null)
            resumeCountdownText.gameObject.SetActive(false);
        quickCountdownBaseScale = quickCountdownText.rectTransform.localScale;
        quickCountdownBaseColor = quickCountdownText.color;
        PongCountdownVisual.Reset(quickCountdownText, quickCountdownBaseScale, quickCountdownBaseColor);

        pauseAction = pongMap.FindAction("Pause");
        pauseAction.Enable();


        pauseMenuPanel.SetActive(false);
        returnToMenuConfirmPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
        modeSelectPanel.SetActive(false);
        legacyCustomizePanel.SetActive(false);
        HideApplySuccessFeedback();
        sideSelectPanel.SetActive(false);
        

        hudPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        gameplayRoot.SetActive(false);

        ball.enabled = false;


        UpdateScoreText();
        UpdateBallValueText();

        if (gameMode == GameMode.Timed)
        {
            remainingTime = totalTime;
            matchInfoText.gameObject.SetActive(true);
            UpdateTimerText();
        }
    }
    
    private void Update()
    {
        if (matchIntroActive)
            return;

        if (resumeCountdownActive)
        {
            if (pauseAction.WasPressedThisFrame())
            {
                CancelResumeCountdownToPause();
            }

            return;
        }

        if (gamePaused && returnToMenuConfirmPanel.activeSelf)
        {
            if (pauseAction.WasPressedThisFrame())
            {
                CancelReturnToMenu();
            }

            return;
        }

        if (gameplayRoot.activeSelf && !gameOver && pauseAction.WasPressedThisFrame())
        {
            if (gamePaused)
            {
                BeginResumeCountdown();
            }
            else
            {
                PauseGame();
            }

            return;
        }

        if (gamePaused)
        {
            return;
        }

        if (sideSelectPanel.activeSelf)
        {

            return;
        }

        if (gameOver)
        {
            UpdateGameOverRestartInput();
            return;
        }

        if (gameMode == GameMode.Timed && !gameOver && !goalPause && ball.IsMoving())
        {
            remainingTime -= Time.deltaTime;

            if (remainingTime <= 0f)
            {
                remainingTime = 0f;

                UpdateTimerText();
                EndTimedGame();

                return;
            }

            UpdateTimerText();
        }
    }

    private void SetMenuPanel(GameObject panel)
    {
        CancelGameOverRestartPress();
        matchIntroActive = false;
        if (matchPresentation != null)
            matchPresentation.ResetPresentation();
        HideApplySuccessFeedback();


        mainMenuPanel.SetActive(panel == mainMenuPanel);
        timedSetupPanel.SetActive(panel == timedSetupPanel);
        legacyCustomizePanel.SetActive(panel == legacyCustomizePanel);

        sideSelectPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        hudPanel.SetActive(false);
        gameplayRoot.SetActive(false);
    }
    public void ShowTimedSetup()
    {
        SetMenuPanel(timedSetupPanel);
    }

    public void ScorePoint(int player)
    {
        if (goalPause || gameOver || matchIntroActive || !gameplayRoot.activeInHierarchy || !ball.IsMoving())
        {
            return;
        }

        goalPause = true;

        int points = ball.GetGoalPoints();

        if (player == 1)
        {
            player1Score += points;
        }
        else if (player == 2)
        {
            player2Score += points;
        }

        UpdateScoreText();

        // Keep the final field's target when this goal already decides a Legacy match.
        bool winningGoal = gameMode == GameMode.Legacy &&
            (player1Score >= settings.legacyWinScore || player2Score >= settings.legacyWinScore);
        if (winningGoal)
            targetManager.OnMatchEnded();
        else
            targetManager.OnGoalScored();

        goalPauseRoutine = StartCoroutine(GoalPauseRoutine(player));
    }

    public void ScoreGoal(PaddleSide goalSide)
    {
        int scoringPlayer;

        if (goalSide == PaddleSide.Left)
        {
            if (player1Side == PaddleSide.Right)
            {
                scoringPlayer = 1;
            }
            else
            {
                scoringPlayer = 2;
            }
        }
        else
        {
            if (player1Side == PaddleSide.Left)
            {
                scoringPlayer = 1;
            }
            else
            {
                scoringPlayer = 2;
            }
        }

        ScorePoint(scoringPlayer);
    }

    private void UpdateScoreText()
    {
        player1ScoreText.text = player1Score.ToString();
        player2ScoreText.text = player2Score.ToString();
    }

    private void EndGame(int winner)
    {
        gameOver = true;
        CancelGameOverRestartPress();
        CancelQuickCountdown();
        StopScoreFlash();
        targetManager.OnMatchEnded();

        ball.ResetBall(GetNextServeSpeed());
        ball.enabled = false;

        // Freeze the field while the menu keeps animating with unscaled time.
        Time.timeScale = 0f;

        // Enable the panel first so TMP has initialized its font material.
        gameOverPanel.SetActive(true);
        UpdateGameOverText(winner);
    }

    private void UpdateGameOverText(int winner)
    {
        // Scores are stored by control group. Blue always occupies Left, orange Right.
        int blueScore = player1Side == PaddleSide.Left ? player1Score : player2Score;
        int orangeScore = player1Side == PaddleSide.Right ? player1Score : player2Score;

        if (gameOverBlueScoreText != null)
        {
            gameOverBlueScoreText.text = blueScore.ToString("00");
            ApplyGameOverOutline(gameOverBlueScoreText, blueResultOutline, finalScoreOutlineWidth);
        }

        if (gameOverOrangeScoreText != null)
        {
            gameOverOrangeScoreText.text = orangeScore.ToString("00");
            ApplyGameOverOutline(gameOverOrangeScoreText, orangeResultOutline, finalScoreOutlineWidth);
        }

        if (winner == 0)
        {
            winnerText.text = "DRAW!";
            ApplyGameOverOutline(winnerText, drawResultOutline, winnerOutlineWidth);
        }
        else
        {
            PaddleSide winnerSide = winner == 1 ? player1Side : player2Side;
            bool blueWins = winnerSide == PaddleSide.Left;
            winnerText.text = blueWins ? "BLUE WINS!" : "ORANGE WINS!";
            ApplyGameOverOutline(winnerText,
                blueWins ? blueResultOutline : orangeResultOutline, winnerOutlineWidth);
        }
    }

    private static void ApplyGameOverOutline(TMP_Text text, Color color, float width)
    {
        if (text == null || text.fontSharedMaterial == null)
            return;

        // fontMaterial gives this text its own instance; never edit the shared font asset.
        Material material = text.fontMaterial;
        if (!material.HasProperty(ShaderUtilities.ID_OutlineWidth) ||
            !material.HasProperty(ShaderUtilities.ID_OutlineColor))
        {
            Debug.LogWarning("PongGameManager: game over outlines need a TMP Distance Field font material.", text);
            return;
        }

        text.outlineColor = color;
        text.outlineWidth = Mathf.Clamp01(width);
        material.EnableKeyword(ShaderUtilities.Keyword_Outline);
        text.UpdateMeshPadding();
    }

    private void UpdateGameOverRestartInput()
    {
        if (!Application.isFocused || !gameOverPanel.activeInHierarchy)
        {
            CancelGameOverRestartPress();
            return;
        }

        if (spawnAction.WasPressedThisFrame())
        {
            gameOverRestartHeld = true;
            if (gameOverPlayAgainFeedback != null && gameOverPlayAgainFeedback.isActiveAndEnabled)
                gameOverPlayAgainFeedback.SetExternalHeld(true);
        }

        if (gameOverRestartHeld && spawnAction.WasReleasedThisFrame())
        {
            CancelGameOverRestartPress();
            RestartGame();
        }
    }

    private void CancelGameOverRestartPress()
    {
        gameOverRestartHeld = false;
        if (gameOverPlayAgainFeedback != null && gameOverPlayAgainFeedback.isActiveAndEnabled)
            gameOverPlayAgainFeedback.SetExternalHeld(false);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            CancelGameOverRestartPress();
    }

    public void RestartGame()
    {
        CancelGameOverRestartPress();
        Time.timeScale = 1f;
        gameOver = false;
        leftPaddleController.ResetToStartPosition();
        rightPaddleController.ResetToStartPosition();
        leftPaddleVisuals.SetPresentationHidden(false);
        rightPaddleVisuals.SetPresentationHidden(false);
        firstQuickServe = true;
        targetManager.ResetForNewMatch();
        goalPause = false;

        player1Visuals.SetSweat(false);
        player2Visuals.SetSweat(false);
        
        player1Score = 0;
        player2Score = 0;

        if (gameMode == GameMode.Timed)
        {
            remainingTime = totalTime;
            UpdateTimerText();
        }
        else
        {
            UpdateLegacyMatchInfo();
        }

        UpdateScoreText();

        gameOverPanel.SetActive(false);

        ball.ResetBall(GetNextServeSpeed());

        UpdateBallValueText();

        BeginServePhase();
    }

    private float GetNextServeSpeed()
    {
        if (gameMode == GameMode.Legacy)
        {
            return settings.baseBallSpeed;
        }

        float timeProgress =
            1f - (remainingTime / totalTime);

        return Mathf.Lerp(settings.baseBallSpeed, settings.timedMaxServeSpeed, timeProgress);
    }

    public void UpdateBallValueText()
    {
        currentBallValueText.text = ball.GetGoalPoints().ToString();
    }

    private void UpdateTimerText()
    {
        int timeToDisplay = Mathf.CeilToInt(remainingTime);

        int minutes = timeToDisplay / 60;
        int seconds = timeToDisplay % 60;

        matchInfoText.text = minutes.ToString("00") + ":" + seconds.ToString("00");
    }

    private void EndTimedGame()
    {
        if (player1Score > player2Score)
        {
            EndGame(1);
        }
        else if (player2Score > player1Score)
        {
            EndGame(2);
        }
        else
        {
            EndGame(0);
        }
    }

    private IEnumerator GoalPauseRoutine(int scoringPlayer)
    {
        TMP_Text scoringText;
        PongPaddleVisuals losingPlayerVisuals;

        if (scoringPlayer == 1)
        {
            scoringText = player1ScoreText;
            losingPlayerVisuals = player2Visuals;
        }
        else
        {
            scoringText = player2ScoreText;
            losingPlayerVisuals = player1Visuals;
        }

        ball.ResetBall(GetNextServeSpeed());
        ball.enabled = false;

        losingPlayerVisuals.SetSweat(true);
        StopScoreFlash();
        scoreFlashRoutine = StartCoroutine(FlashScoreText(scoringText));
        yield return new WaitForSeconds(settings.goalPauseDuration);

        losingPlayerVisuals.SetSweat(false);

        if (gameMode == GameMode.Legacy)
        {
            if (player1Score >= settings.legacyWinScore)
            {
                goalPause = false;
                EndGame(1);
                yield break;
            }

            if (player2Score >= settings.legacyWinScore)
            {
                goalPause = false;
                EndGame(2);
                yield break;
            }
        }

        goalPause = false;

        UpdateBallValueText();

        BeginServePhase();
    }

    private IEnumerator FlashScoreText(TMP_Text scoreText)
    {
        float elapsedTime = 0f;

        while (elapsedTime < settings.scoreFlashDuration)
        {
            scoreText.enabled = !scoreText.enabled;

            yield return new WaitForSeconds(settings.scoreFlashInterval);

            elapsedTime += settings.scoreFlashInterval;
        }

        scoreText.enabled = true;
        scoreFlashRoutine = null;
    }

    private void StopScoreFlash()
    {
        if (scoreFlashRoutine != null)
        {
            StopCoroutine(scoreFlashRoutine);
            scoreFlashRoutine = null;
        }

        if (player1ScoreText != null)
            player1ScoreText.enabled = true;
        if (player2ScoreText != null)
            player2ScoreText.enabled = true;
    }

    public float GetFreezeDuration(float effectiveBallSpeed)
    {
        if (gameMode == GameMode.Timed)
        {
            float timeProgress = 1f - (remainingTime / totalTime);

            timeProgress = Mathf.Clamp01(timeProgress);

            return Mathf.Lerp(settings.freezeMaxDuration, settings.freezeMinDuration, timeProgress);
        }

        float speedProgress = Mathf.InverseLerp(settings.baseBallSpeed, settings.maxBallSpeed, effectiveBallSpeed);

        return Mathf.Lerp(settings.freezeMaxDuration, settings.freezeMinDuration, speedProgress);
    }
    public void ShowModeSelect()
    {
        ShowTimedSetup();
    }

    public void ShowMainMenu()
    {
        // Also commit pending Legacy input when this method is called directly.
        if (legacyCustomizePanel != null && legacyCustomizePanel.activeInHierarchy &&
            legacyScoreInput != null)
            SaveLegacyCustomization(legacyScoreInput.text);
        SetMenuPanel(mainMenuPanel);
    }

    public void ReturnToMainMenu()
    {
        CancelGameOverRestartPress();
        StopScoreFlash();
        matchIntroActive = false;
        if (matchPresentation != null)
            matchPresentation.ResetPresentation();
        if (goalPauseRoutine != null)
        {
            StopCoroutine(goalPauseRoutine);
            goalPauseRoutine = null;
        }
        if (resumeCountdownRoutine != null)
        {
            StopCoroutine(resumeCountdownRoutine);
            resumeCountdownRoutine = null;
        }

        resumeCountdownActive = false;

        if (resumeCountdownText != null)
        {
            resumeCountdownText.gameObject.SetActive(false);
        }

        Time.timeScale = 1f;
        gamePaused = false;
        pauseMenuPanel.SetActive(false);
        returnToMenuConfirmPanel.SetActive(false);

        targetManager.ResetForNewMatch();

        gameOver = false;
        goalPause = false;

        player1Visuals.SetSweat(false);
        player2Visuals.SetSweat(false);

        ball.enabled = false;
        ball.ResetBall(settings.baseBallSpeed);

        CancelQuickCountdown();
        resumeShouldAutoServe = false;
        firstQuickServe = true;

        gameOverPanel.SetActive(false);
        hudPanel.SetActive(false);
        gameplayRoot.SetActive(false);

        sideSelectPanel.SetActive(false);
        modeSelectPanel.SetActive(false);
        legacyCustomizePanel.SetActive(false);

        mainMenuPanel.SetActive(true);
        
    }

    public void SelectLegacyMode()
    {
        ShowLegacyCustomize();
    }

    public void SelectTimed45()
    {
        StartTimedMode(45f);
    }

    public void SelectTimed90()
    {
        StartTimedMode(90f);
    }

    public void SelectTimed120()
    {
        StartTimedMode(120f);
    }

    private void StartTimedMode(float duration)
    {
        gameMode = GameMode.Timed;

        totalTime = duration;
        remainingTime = duration;

        quickMode = quickModeToggle == null || quickModeToggle.isOn;

        ShowSideSelect();
    }

    private void StartSelectedMode()
    {
        Time.timeScale = 1f;
        firstQuickServe = true;

        mainMenuPanel.SetActive(false);
        modeSelectPanel.SetActive(false);
        legacyCustomizePanel.SetActive(false);
        gameOverPanel.SetActive(false);

        gameplayRoot.SetActive(true);
        hudPanel.SetActive(true);

        player1Score = 0;
        player2Score = 0;

        gameOver = false;
        goalPause = false;

        targetManager.ResetForNewMatch();

        if (gameMode == GameMode.Timed)
        {
            remainingTime = totalTime;
            UpdateTimerText();
        }
        else
        {
            UpdateLegacyMatchInfo();
        }

        UpdateScoreText();

        ball.ResetBall(GetNextServeSpeed());

        UpdateBallValueText();

        if (!matchIntroActive)
            BeginServePhase();
        else
            ball.enabled = false;
    }

    public void ShowLegacyCustomize()
    {
        gameMode = GameMode.Legacy;
        quickMode = false;
        SetMenuPanel(legacyCustomizePanel);
        ResetLegacyScoreInput();
    }

    public void PlayLegacyMode()
    {
        if (settings == null || legacyScoreInput == null)
            return;

        // An empty field uses the score shown by the placeholder.
        SaveLegacyCustomization(legacyScoreInput.text);
        gameMode = GameMode.Legacy;
        quickMode = false;
        ShowSideSelect();
    }

    public void BackFromLegacyCustomize()
    {
        ShowMainMenu();
    }

    public void SubmitLegacyScore(string inputText)
    {
        if (SaveLegacyCustomization(inputText))
            StartApplySuccessFeedback();
    }

    public void ResetLegacyScoreInput()
    {
        if (settings == null || legacyScoreInput == null)
            return;

        settings.legacyWinScore = Mathf.Clamp(settings.legacyWinScore, 1, 999);
        legacyScoreInput.SetTextWithoutNotify(string.Empty);
        UpdateLegacyCustomizeText();
    }

    private void StartApplySuccessFeedback()
    {
        if (applySuccessCheck == null || legacyCustomizePanel == null ||
            !legacyCustomizePanel.activeInHierarchy)
            return;

        if (applyFeedbackRoutine != null)
        {
            StopCoroutine(applyFeedbackRoutine);
        }

        applyFeedbackRoutine =
            StartCoroutine(ApplySuccessFeedbackRoutine());
    }

    private IEnumerator ApplySuccessFeedbackRoutine()
    {
        applySuccessCheck.gameObject.SetActive(true);

        SetApplyCheckAlpha(1f);

        yield return new WaitForSecondsRealtime(2f);

        float fadeDuration = 3f;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsedTime / fadeDuration);

            float alpha = Mathf.Lerp(1f, 0f, progress);

            SetApplyCheckAlpha(alpha);

            yield return null;
        }

        SetApplyCheckAlpha(0f);
        applySuccessCheck.gameObject.SetActive(false);

        applyFeedbackRoutine = null;
    }

    private void SetApplyCheckAlpha(float alpha)
    {
        if (applySuccessCheck == null)
            return;
        Color color = applySuccessCheck.color;
        color.a = alpha;
        applySuccessCheck.color = color;
    }

    private void HideApplySuccessFeedback()
    {
        if (applyFeedbackRoutine != null)
        {
            StopCoroutine(applyFeedbackRoutine);
            applyFeedbackRoutine = null;
        }

        SetApplyCheckAlpha(0f);
        if (applySuccessCheck != null)
            applySuccessCheck.gameObject.SetActive(false);
    }

    private bool SaveLegacyCustomization(string inputText)
    {
        if (settings == null || legacyScoreInput == null)
            return false;

        if (!int.TryParse(inputText, out int newScore))
        {
            ResetLegacyScoreInput();
            return false;
        }

        newScore = Mathf.Clamp(newScore, 1, 999);

        settings.legacyWinScore = newScore;
        ResetLegacyScoreInput();

        return true;
    }

    private void UpdateLegacyMatchInfo()
    {
        matchInfoText.text = "First to " + settings.legacyWinScore;
    }

    private void UpdateLegacyCustomizeText()
    {
        if (currentLegacyScoreText != null)
            currentLegacyScoreText.text = "FIRST TO " + settings.legacyWinScore + " POINTS WINS";
        if (legacyScoreInput != null && legacyScoreInput.placeholder is TMP_Text placeholderText)
            placeholderText.text = settings.legacyWinScore.ToString();
    }

    public void SetQuickMode(bool isOn)
    {
        quickMode = isOn;
    }

    private void ShowSideSelect()
    {
        SetMenuPanel(null);
        sideSelectPanel.SetActive(true);
    }

    public void StartGameFromSideSelect(PaddleSide wasdSide)
    {
        TryStartGameFromSideSelect(wasdSide);
    }

    public bool TryStartGameFromSideSelect(PaddleSide wasdSide)
    {
        if (!sideSelectPanel.activeSelf || matchIntroActive)
            return false;
        if (matchPresentation == null || !matchPresentation.isActiveAndEnabled)
        {
            Debug.LogError("PongGameManager: assign an enabled Match Presentation component before starting.", this);
            return false;
        }
        if (!matchPresentation.ValidateReferences())
            return false;

        matchIntroActive = true;
        player1ControlScheme = ControlScheme.WS;
        player2ControlScheme = ControlScheme.Arrows;
        player1Side = wasdSide;
        player2Side = wasdSide == PaddleSide.Left ? PaddleSide.Right : PaddleSide.Left;
        ApplySideSelectionMapping();
        matchPresentation.PrepareEntrance();
        StartSelectedMode();
        matchPresentation.PlayEntrance();
        return true;
    }

    public void CompleteMatchEntrance()
    {
        if (!matchIntroActive)
            return;
        matchIntroActive = false;
        ball.RequireServeInputRelease();
        if (gameMode == GameMode.Timed && quickMode)
        {
            // The opening 3-2-1 happened on side selection; the first ball can now launch.
            firstQuickServe = false;
            ball.enabled = true;
            ball.AutoLaunchBall();
        }
        else
            BeginServePhase();
    }

    public void BackFromSideSelect()
    {
        if (!sideSelectPanel.activeSelf)
            return;

        SetMenuPanel(gameMode == GameMode.Timed
            ? timedSetupPanel : mainMenuPanel);
    }

    private void ApplySideSelectionMapping()
    {
        if (player1Side == PaddleSide.Left)
        {
            leftPaddleController.SetControlScheme(player1ControlScheme);

            rightPaddleController.SetControlScheme(player2ControlScheme);

            player1Visuals = leftPaddleVisuals;
            player2Visuals = rightPaddleVisuals;

            player1ScoreText = leftScoreText;
            player2ScoreText = rightScoreText;
        }
        else
        {
            leftPaddleController.SetControlScheme(player2ControlScheme);

            rightPaddleController.SetControlScheme(player1ControlScheme);

            player1Visuals = rightPaddleVisuals;
            player2Visuals = leftPaddleVisuals;
            player1ScoreText = rightScoreText;
            player2ScoreText = leftScoreText;
        }
    }

    public void PauseGame()
    {
        if (matchIntroActive)
            return;
        if (quickCountdownActive)
        {
            CancelQuickCountdown();

            resumeShouldAutoServe = true;
        }
        else
        {
            resumeShouldAutoServe = false;
        }
        gamePaused = true;

        pauseMenuPanel.SetActive(true);
        returnToMenuConfirmPanel.SetActive(false);

        Time.timeScale = 0f;
    }

    // public void ResumeGame()
    // {
    //     Time.timeScale = 1f;

    //     gamePaused = false;

    //     pauseMenuPanel.SetActive(false);
    //     returnToMenuConfirmPanel.SetActive(false);
    // }

    public void BeginResumeCountdown()
    {
        if (!gamePaused || resumeCountdownActive)
        {
            return;
        }

        pauseMenuPanel.SetActive(false);
        returnToMenuConfirmPanel.SetActive(false);

        resumeCountdownActive = true;

        resumeCountdownRoutine =
            StartCoroutine(ResumeCountdownRoutine());
    }

    private IEnumerator ResumeCountdownRoutine()
    {
        PongCountdownVisual.Show(resumeCountdownText, countdownVisuals);

        for (int number = 3; number >= 1; number--)
        {
            resumeCountdownText.text = number.ToString();

            yield return new WaitForSecondsRealtime(settings.pauseCountdownStep);
        }

        resumeCountdownText.gameObject.SetActive(false);

        resumeCountdownActive = false;
        resumeCountdownRoutine = null;

        gamePaused = false;

        Time.timeScale = 1f;

        if (resumeShouldAutoServe)
        {
            resumeShouldAutoServe = false;

            firstQuickServe = false;

            ball.enabled = true;
            ball.AutoLaunchBall();
        }
    }

    public void ShowReturnToMenuConfirmation()
    {
        pauseMenuPanel.SetActive(false);
        returnToMenuConfirmPanel.SetActive(true);
    }

    public void CancelReturnToMenu()
    {
        returnToMenuConfirmPanel.SetActive(false);
        pauseMenuPanel.SetActive(true);
    }
    
    public void ConfirmReturnToMenu()
    {
        Time.timeScale = 1f;

        gamePaused = false;

        pauseMenuPanel.SetActive(false);
        returnToMenuConfirmPanel.SetActive(false);

        ReturnToMainMenu();
    }

    private void CancelResumeCountdownToPause()
    {
        if (resumeCountdownRoutine != null)
        {
            StopCoroutine(resumeCountdownRoutine);
            resumeCountdownRoutine = null;
        }

        resumeCountdownActive = false;

        resumeCountdownText.gameObject.SetActive(false);

        gamePaused = true;
        Time.timeScale = 0f;

        returnToMenuConfirmPanel.SetActive(false);
        pauseMenuPanel.SetActive(true);
    }

    private void BeginServePhase()
    {
        if (matchIntroActive)
            return;
        if (gameMode == GameMode.Timed && quickMode)
        {
            ball.enabled = false;
            StartQuickCountdown();
        }
        else
        {
            ball.enabled = true;
        }
    }

    private void StartQuickCountdown()
    {
        if (quickCountdownRoutine != null)
        {
            StopCoroutine(quickCountdownRoutine);
        }

        quickCountdownActive = true;

        quickCountdownRoutine = StartCoroutine(QuickCountdownRoutine());
        
    }

    private float GetQuickCountdownStep()
    {
        if (firstQuickServe)
        {
            return settings.firstQuickCountdownStep;
        }

        float remainingRatio = remainingTime / totalTime;

        if (remainingRatio <= settings.lateQuickThresholdRatio)
        {
            return settings.lateQuickCountdownStep;
        }

        return settings.normalQuickCountdownStep;
    }

    private IEnumerator QuickCountdownRoutine()
    {
        yield return PongCountdownVisual.Play(quickCountdownText, settings, GetQuickCountdownStep(),
            quickCountdownBaseScale, quickCountdownBaseColor, false, countdownVisuals);
        quickCountdownActive = false;
        quickCountdownRoutine = null;
        firstQuickServe = false;
        ball.enabled = true;
        ball.AutoLaunchBall();
    }

    private void CancelQuickCountdown()
    {
        if (quickCountdownRoutine != null)
        {
            StopCoroutine(quickCountdownRoutine);
            quickCountdownRoutine = null;
        }

        quickCountdownActive = false;

        PongCountdownVisual.Reset(quickCountdownText, quickCountdownBaseScale, quickCountdownBaseColor);
    }
}
