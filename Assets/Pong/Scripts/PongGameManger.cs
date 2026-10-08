using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.UI;

public enum GameMode
{
    Legacy,
    Timed
}

public enum SideSelectState
{
    WaitingForPlayer1,
    ChoosingSide,
    ReadyToStart
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
    public GameObject unsavedChangesPanel;
    public TMP_Text unsavedChangesText;

    public Toggle quickModeToggle;
    public bool quickMode = false;


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

    public GameObject gameOverPanel;
    public TMP_Text winnerText;

    public InputActionAsset inputActions;

    private InputAction spawnAction;

    private bool gameOver = false;

    public PongPaddleVisuals player1Visuals;
    public PongPaddleVisuals player2Visuals;

    private bool goalPause = false;

    public PongTargetManager targetManager;

    public TMP_Text applySuccessCheck;

    private Coroutine applyFeedbackRoutine;

    public GameObject sideSelectPanel;

    public TMP_Text sideSelectPromptText;

    private ControlScheme player1ControlScheme;
    private ControlScheme player2ControlScheme;
    private bool sideSelectionComplete = false;

    private PaddleSide player1Side;
    private PaddleSide player2Side;

    public PongPaddleController leftPaddleController;
    public PongPaddleController rightPaddleController;

    public PongPaddleVisuals leftPaddleVisuals;
    public PongPaddleVisuals rightPaddleVisuals;

    private SideSelectState sideSelectState;

    private InputAction adChooseLeftAction;
    private InputAction adChooseRightAction;

    private InputAction arrowChooseLeftAction;
    private InputAction arrowChooseRightAction;
    private InputAction sideConfirmAction;

    public RectTransform leftCharacter;
    public RectTransform rightCharacter;

    public Outline leftCharacterOutline;
    public Outline rightCharacterOutline;

    public TMP_Text playerLabel;
    public RectTransform selectionArrow;
    public TMP_Text controlSchemeText;

    public TMP_Text player2Label;
    public TMP_Text player2ControlSchemeText;
    private Vector2 arrowBasePosition;


    public GameObject pauseMenuPanel;
    public GameObject returnToMenuConfirmPanel;
    private bool gamePaused = false;
    private InputAction pauseAction;


    public TMP_Text resumeCountdownText;

    private bool resumeCountdownActive = false;
    private Coroutine resumeCountdownRoutine;

    public TMP_Text quickCountdownText;

    private Vector3 quickCountdownBaseScale;

    private bool quickCountdownActive = false;
    private Coroutine quickCountdownRoutine;

    private bool firstQuickServe = true;

    private bool resumeShouldAutoServe = false;

    private Vector3 leftCharacterBaseScale;
    private Vector3 rightCharacterBaseScale;
    

    private void Start()
    {
        InputActionMap pongMap = inputActions.FindActionMap("Pong");
        spawnAction = pongMap.FindAction("Spawn");
        spawnAction.Enable();

        quickCountdownBaseScale = quickCountdownText.rectTransform.localScale;

        pauseAction = pongMap.FindAction("Pause");
        pauseAction.Enable();

        leftCharacterBaseScale = leftCharacter.localScale;
        rightCharacterBaseScale = rightCharacter.localScale;

        pauseMenuPanel.SetActive(false);
        returnToMenuConfirmPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
        modeSelectPanel.SetActive(false);
        legacyCustomizePanel.SetActive(false);
        unsavedChangesPanel.SetActive(false);
        sideSelectPanel.SetActive(false);
        

        hudPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        gameplayRoot.SetActive(false);

        ball.enabled = false;

        InputActionMap sideSelectMap = inputActions.FindActionMap("SideSelect");

        adChooseLeftAction = sideSelectMap.FindAction("WSChooseLeft");

        adChooseRightAction = sideSelectMap.FindAction("WSChooseRight");

        arrowChooseLeftAction = sideSelectMap.FindAction("ArrowChooseLeft");

        arrowChooseRightAction = sideSelectMap.FindAction("ArrowChooseRight");

        sideConfirmAction = sideSelectMap.FindAction("Confirm");

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
            CheckSideSelection();
            UpdateSelectionArrow();

            return;
        }

        if (gameOver && spawnAction.WasPressedThisFrame())
        {
            RestartGame();
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

    public void ScorePoint(int player)
    {
        if (goalPause || gameOver)
        {
            return;
        }

        goalPause = true;

        targetManager.OnGoalScored();

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

        StartCoroutine(GoalPauseRoutine(player));
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

        ball.ResetBall(GetNextServeSpeed());
        ball.enabled = false;

        if (winner == 0)
        {
            winnerText.text = "Draw!";
        }
        else
        {
            winnerText.text = "Player " + winner + " Wins!";
        }

        gameOverPanel.SetActive(true);
    }

    public void RestartGame()
    {
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
        StartCoroutine(FlashScoreText(scoringText));
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
        mainMenuPanel.SetActive(false);
        modeSelectPanel.SetActive(true);
        legacyCustomizePanel.SetActive(false);

        hudPanel.SetActive(false);
        gameplayRoot.SetActive(false);
    }

    public void ShowMainMenu()
    {
        modeSelectPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void ReturnToMainMenu()
    {
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
        unsavedChangesPanel.SetActive(false);

        mainMenuPanel.SetActive(true);
        
    }

    public void SelectLegacyMode()
    {
        gameMode = GameMode.Legacy;
        quickMode = false;

        ShowSideSelect();
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

        quickMode = quickModeToggle.isOn;

        ShowSideSelect();
    }

    private void StartSelectedMode()
    {
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

        BeginServePhase();
    }

    public void ShowLegacyCustomize()
    {
        modeSelectPanel.SetActive(false);
        legacyCustomizePanel.SetActive(true);
        unsavedChangesPanel.SetActive(false);

        HideApplySuccessFeedback();

        legacyScoreInput.text = settings.legacyWinScore.ToString();
        UpdateLegacyCustomizeText();
    }

    public void ApplyLegacyCustomization()
    {
        if (!SaveLegacyCustomization())
        {
            return;
        }

        StartApplySuccessFeedback();
    }

    public void ApplyLegacyCustomizationAndGoBack()
    {
        if (!SaveLegacyCustomization())
        {
            return;
        }

        HideApplySuccessFeedback();

        legacyCustomizePanel.SetActive(false);
        modeSelectPanel.SetActive(true);
    }

    public void SubmitLegacyScore(string inputText)
    {
        ApplyLegacyCustomization();
    }

    private void StartApplySuccessFeedback()
    {
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

        yield return new WaitForSeconds(2f);

        float fadeDuration = 3f;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;

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
        applySuccessCheck.gameObject.SetActive(false);
    }

    private bool SaveLegacyCustomization()
    {
        int newScore;

        if (!int.TryParse(legacyScoreInput.text, out newScore))
        {
            return false;
        }

        newScore = Mathf.Clamp(newScore, 1, 999);

        settings.legacyWinScore = newScore;

        legacyScoreInput.text = newScore.ToString();

        UpdateLegacyCustomizeText();

        return true;
    }

    public void BackFromLegacyCustomize()
    {
        legacyCustomizePanel.SetActive(false);
        modeSelectPanel.SetActive(true);
    }
    private void UpdateLegacyMatchInfo()
    {
        matchInfoText.text = "First to " + settings.legacyWinScore;
    }

    private void UpdateLegacyCustomizeText()
    {
        currentLegacyScoreText.text = "Current: " + settings.legacyWinScore;
    }

    

    public void RequestBackFromLegacyCustomize()
    {
        if (HasUnsavedLegacyChanges())
        {
            ShowUnsavedChangesWarning();
        }
        else
        {
            ShowModeSelect();
        }
    }

    private bool HasUnsavedLegacyChanges()
    {
        int inputScore;

        if (!int.TryParse(legacyScoreInput.text, out inputScore
        ))
        {
            return true;
        }

        return inputScore != settings.legacyWinScore;
    }

    private void ShowUnsavedChangesWarning()
    {
        legacyCustomizePanel.SetActive(false);
        unsavedChangesPanel.SetActive(true);

        string inputValue = legacyScoreInput.text;

        if (string.IsNullOrEmpty(inputValue))
        {
            inputValue = "(empty)";
        }

        unsavedChangesText.text = "Changes:\n" + "Legacy Winning Score: " + settings.legacyWinScore + " → " + inputValue;
    }

    public void CancelDiscardLegacyChanges()
    {
        unsavedChangesPanel.SetActive(false);
        legacyCustomizePanel.SetActive(true);
    }

    public void ConfirmDiscardLegacyChanges()
    {
        unsavedChangesPanel.SetActive(false);

        legacyScoreInput.text = settings.legacyWinScore.ToString();

        legacyCustomizePanel.SetActive(false);
        modeSelectPanel.SetActive(true);
    }

    public void SaveLegacyChangesAndContinue()
    {
        if (!SaveLegacyCustomization())
        {
            return;
        }

        HideApplySuccessFeedback();

        unsavedChangesPanel.SetActive(false);
        legacyCustomizePanel.SetActive(false);
        modeSelectPanel.SetActive(true);
    }

    public void SetQuickMode(bool isOn)
    {
        quickMode = isOn;
    }

    private void EnableSideSelectInput()
    {
        adChooseLeftAction.Enable();
        adChooseRightAction.Enable();

        arrowChooseLeftAction.Enable();
        arrowChooseRightAction.Enable();

        sideConfirmAction.Enable();
    }

    private void DisableSideSelectInput()
    {
        adChooseLeftAction.Disable();
        adChooseRightAction.Disable();

        arrowChooseLeftAction.Disable();
        arrowChooseRightAction.Disable();

        sideConfirmAction.Disable();
    }

    private void ShowSideSelect()
    {
        mainMenuPanel.SetActive(false);
        modeSelectPanel.SetActive(false);
        legacyCustomizePanel.SetActive(false);

        sideSelectPanel.SetActive(true);

        hudPanel.SetActive(false);
        gameplayRoot.SetActive(false);

        sideSelectState = SideSelectState.WaitingForPlayer1;

        playerLabel.gameObject.SetActive(false);
        controlSchemeText.gameObject.SetActive(false);

        player2Label.gameObject.SetActive(false);
        player2ControlSchemeText.gameObject.SetActive(false);

        
        selectionArrow.gameObject.SetActive(false);

        ResetSideSelectVisuals();

        sideSelectPromptText.text = "Player 1: A/D or ←/→ to choose a side";

        EnableSideSelectInput();
    }

    private void CheckSideSelection()
    {
        if (sideSelectState == SideSelectState.WaitingForPlayer1)
        {
            CheckFirstPlayerInput();
            return;
        }

        if (sideSelectState == SideSelectState.ChoosingSide)
        {
            CheckPlayer1SideInput();

            if (sideConfirmAction.WasPressedThisFrame())
            {
                ConfirmSideSelection();
            }

            return;
        }

        if (sideSelectState == SideSelectState.ReadyToStart)
        {
            if (sideConfirmAction.WasPressedThisFrame())
            {
                StartGameFromSideSelect();
            }
        }
    }

    private void CompleteSideSelection(ControlScheme firstControlScheme, PaddleSide chosenSide)
    {
        sideSelectionComplete = true;

        player1ControlScheme = firstControlScheme;

        player1Side = chosenSide;

        if (firstControlScheme == ControlScheme.WS)
        {
            player2ControlScheme = ControlScheme.Arrows;
        }
        else
        {
            player2ControlScheme = ControlScheme.WS;
        }

        if (chosenSide == PaddleSide.Left)
        {
            player2Side = PaddleSide.Right;
        }
        else
        {
            player2Side = PaddleSide.Left;
        }

        DisableSideSelectInput();

        Debug.Log("Player 1: " + player1ControlScheme + " / " + player1Side);

        Debug.Log("Player 2: " + player2ControlScheme + " / " + player2Side);

        sideSelectPromptText.text = "Selection complete";
    }

    private void ResetSideSelectVisuals()
    {
        leftCharacter.localScale = leftCharacterBaseScale;

        rightCharacter.localScale = rightCharacterBaseScale;

        leftCharacterOutline.enabled = false;
        rightCharacterOutline.enabled = false;
    }

    private void CheckFirstPlayerInput()
    {
        if (adChooseLeftAction.WasPressedThisFrame())
        {
            BeginSideSelection();

            SetPlayer1Selection(
                ControlScheme.WS,
                PaddleSide.Left
            );

            return;
        }

        if (adChooseRightAction.WasPressedThisFrame())
        {
            BeginSideSelection();

            SetPlayer1Selection(
                ControlScheme.WS,
                PaddleSide.Right
            );

            return;
        }

        if (arrowChooseLeftAction.WasPressedThisFrame())
        {
            BeginSideSelection();

            SetPlayer1Selection(
                ControlScheme.Arrows,
                PaddleSide.Left
            );

            return;
        }

        if (arrowChooseRightAction.WasPressedThisFrame())
        {
            BeginSideSelection();

            SetPlayer1Selection(
                ControlScheme.Arrows,
                PaddleSide.Right
            );
        }
    }

    private void BeginSideSelection()
    {
        sideSelectState = SideSelectState.ChoosingSide;

        playerLabel.text = "Player 1";

        playerLabel.gameObject.SetActive(true);
        controlSchemeText.gameObject.SetActive(true);
        selectionArrow.gameObject.SetActive(true);

        sideSelectPromptText.text = "A/D or ←/→ to change side\n" + "Press Enter or Space to confirm";
    }

    private void CheckPlayer1SideInput()
    {
        if (adChooseLeftAction.WasPressedThisFrame())
        {
            SetPlayer1Selection(ControlScheme.WS, PaddleSide.Left);

            return;
        }

        if (adChooseRightAction.WasPressedThisFrame())
        {
            SetPlayer1Selection(ControlScheme.WS, PaddleSide.Right);

            return;
        }

        if (arrowChooseLeftAction.WasPressedThisFrame())
        {
            SetPlayer1Selection(ControlScheme.Arrows, PaddleSide.Left);

            return;
        }

        if (arrowChooseRightAction.WasPressedThisFrame())
        {
            SetPlayer1Selection(ControlScheme.Arrows, PaddleSide.Right);
        }
    }

    private void UpdateSelectedSideVisual()
    {
        ResetSideSelectVisuals();

        RectTransform selectedCharacter;

        if (player1Side == PaddleSide.Left)
        {
            selectedCharacter = leftCharacter;

            leftCharacterOutline.enabled = true;

            selectedCharacter.localScale = leftCharacterBaseScale * settings.selectedCharacterScale;
        }
        else
        {
            selectedCharacter = rightCharacter;

            rightCharacterOutline.enabled = true;

            selectedCharacter.localScale = rightCharacterBaseScale * settings.selectedCharacterScale;
        }


        MoveSelectionIndicator(selectedCharacter);
    }

    private void MoveSelectionIndicator(RectTransform selectedCharacter)
    {
        PositionPlayerInfo(selectedCharacter, playerLabel, controlSchemeText);

        Vector2 characterPosition = selectedCharacter.anchoredPosition;

        playerLabel.rectTransform.anchoredPosition = characterPosition + new Vector2(0f, 150f);

        selectionArrow.anchoredPosition = characterPosition + new Vector2(0f, 90f);

        arrowBasePosition = selectionArrow.anchoredPosition;
    }
    
    private void UpdateSelectionArrow()
    {
        if (sideSelectState != SideSelectState.ChoosingSide || !selectionArrow.gameObject.activeSelf)
        {
            return;
        }

        float cycleProgress = Mathf.Repeat(Time.time, settings.selectionArrowCycle) / settings.selectionArrowCycle;

        float offset = Mathf.Sin(cycleProgress * Mathf.PI * 2f);

        selectionArrow.anchoredPosition = arrowBasePosition + Vector2.up * offset * settings.selectionArrowDistance;
    }

    private void ConfirmSideSelection()
    {
        if (player1Side == PaddleSide.Left)
        {
            player2Side = PaddleSide.Right;
        }
        else
        {
            player2Side = PaddleSide.Left;
        }

        if (player1ControlScheme == ControlScheme.WS)
        {
            player2ControlScheme =
                ControlScheme.Arrows;
        }
        else
        {
            player2ControlScheme =
                ControlScheme.WS;
        }

        sideSelectState =
            SideSelectState.ReadyToStart;

        selectionArrow.gameObject.SetActive(false);

        leftCharacter.localScale = leftCharacterBaseScale * settings.selectedCharacterScale;

        rightCharacter.localScale = rightCharacterBaseScale * settings.selectedCharacterScale;

        leftCharacterOutline.enabled = true;
        rightCharacterOutline.enabled = true;

        playerLabel.gameObject.SetActive(true);
        controlSchemeText.gameObject.SetActive(true);

        player2Label.text = "Player 2";

        player2ControlSchemeText.text = GetControlSchemeText(player2ControlScheme);

        player2Label.gameObject.SetActive(true);
        player2ControlSchemeText.gameObject.SetActive(true);

        RectTransform player1Character;
        RectTransform player2Character;

        if (player1Side == PaddleSide.Left)
        {
            player1Character = leftCharacter;
            player2Character = rightCharacter;
        }
        else
        {
            player1Character = rightCharacter;
            player2Character = leftCharacter;
        }

        PositionPlayerInfo(player1Character, playerLabel, controlSchemeText);

        PositionPlayerInfo(player2Character, player2Label, player2ControlSchemeText);

        sideSelectPromptText.text = "Press Enter or Space to start game";
    }

    private void StartGameFromSideSelect()
    {
        DisableSideSelectInput();

        ApplySideSelectionMapping();

        sideSelectPanel.SetActive(false);

        StartSelectedMode();
    }

    private void SetPlayer1Selection(ControlScheme controlScheme, PaddleSide side)
    {
        player1ControlScheme = controlScheme;
        player1Side = side;

        if (player1ControlScheme == ControlScheme.WS)
        {
            player2ControlScheme = ControlScheme.Arrows;
        }
        else
        {
            player2ControlScheme = ControlScheme.WS;
        }

        UpdateSelectedSideVisual();
        UpdateControlSchemeDisplay();
    }

    private void UpdateControlSchemeDisplay()
    {
        controlSchemeText.gameObject.SetActive(true);

        controlSchemeText.text =
            GetControlSchemeText(
                player1ControlScheme
            );
    }

    private string GetControlSchemeText(ControlScheme controlScheme)
    {
        if (controlScheme == ControlScheme.WS)
        {
            return "(W/S)";
        }

        return "(↑/↓)";
    }

    private void PositionPlayerInfo(RectTransform character, TMP_Text playerText, TMP_Text controlsText)
    {
        Vector2 characterPosition = character.anchoredPosition;

        playerText.rectTransform.anchoredPosition = characterPosition + new Vector2(0f, 150f);

        controlsText.rectTransform.anchoredPosition = characterPosition + new Vector2(0f, 120f);
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
        resumeCountdownText.gameObject.SetActive(true);

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
        quickCountdownText.gameObject.SetActive(true);

        float stepDuration =
            GetQuickCountdownStep();

        for (int number = 3; number >= 1; number--)
        {
            quickCountdownText.text = number.ToString();

            Color startColor = quickCountdownText.color;

            startColor.a = 1f;
            quickCountdownText.color = startColor;

            quickCountdownText.rectTransform.localScale = quickCountdownBaseScale;

            float elapsedTime = 0f;

            while (elapsedTime < stepDuration)
            {
                elapsedTime += Time.deltaTime;

                float progress = Mathf.Clamp01(elapsedTime / stepDuration);


                float scale = Mathf.Lerp(1f, settings.countdownFinalScale, progress);

                quickCountdownText.rectTransform.localScale = quickCountdownBaseScale * scale;

                // VFX here
                if (elapsedTime > settings.countdownFadeStart)
                {
                    float fadeDuration = stepDuration - settings.countdownFadeStart;

                    float fadeProgress = 1f;

                    if (fadeDuration > 0f)
                    {
                        fadeProgress = (elapsedTime - settings.countdownFadeStart) / fadeDuration;

                        fadeProgress = Mathf.Clamp01(fadeProgress);
                    }

                    float alpha =
                        Mathf.Lerp(1f, settings.countdownFinalAlpha, fadeProgress);

                    Color color = quickCountdownText.color;

                    color.a = alpha;

                    quickCountdownText.color = color;
                }

                yield return null;
            }
        }
        
        quickCountdownText.rectTransform.localScale = quickCountdownBaseScale;

        quickCountdownText.gameObject.SetActive(false);

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

        quickCountdownText.gameObject.SetActive(false);
    }
}