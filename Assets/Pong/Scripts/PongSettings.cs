using UnityEngine;

public class PongSettings : MonoBehaviour
{
    // Playfield
    public float playfieldTop = 4f;
    public float playfieldBottom = -5f;
    public float playfieldCenterY = -0.5f;

    public float paddleMinY = -2.1f;
    public float paddleMaxY = 1.6f;

    // Match
    public int legacyWinScore = 50;

    // Ball speed
    public float baseBallSpeed = 5f;
    public float maxBallSpeed = 15f;
    public float ballSpeedIncreasePerHit = 2f;

    // Timed mode
    public float timedMaxServeSpeed = 12f;

    // Paddle speed
    public float basePaddleSpeed = 5f;
    public float maxPaddleSpeed = 10f;
    public float ballSpeedForMaxPaddleSpeed = 12f;

    [Min(0f)] public float paddleForwardDistance = 2f;

    // Scoring
    public int minGoalPoints = 1;
    public int maxGoalPoints = 10;
    public int goalPointsIncreasePerHit = 2;

    public int targetPickupValueBonus = 5;

    // Goal transition
    public float goalPauseDuration = 2f;
    public float scoreFlashDuration = 1f;
    public float scoreFlashInterval = 0.1f;

    // Target spawning
    public float firstServeTargetDelay = 5f;
    public float normalServeTargetDelay = 2f;

    public float targetLifetime = 10f;
    public float targetBlinkStartTime = 6f;
    public float targetRespawnDelay = 2f;
    public float targetRespawnAfterEffect = 1.5f;

    // Target spawn region
    public float targetMinX = 0.25f;
    public float targetMaxX = 0.75f;
    public float targetMinY = 0.135f;
    public float targetMaxY = 0.765f;

    // Freeze
    public float freezeDuration = 0.8f;
    public float freezeMaxDuration = 0.8f;
    public float freezeMinDuration = 0.5f;
    public float freezeShakeDuration = 0.2f;
    public float freezeShakeDistance = 0.08f;

    public float freezeMinYDirection = 0.35f;
    public float freezeMaxYDirection = 0.9f;

    public float freezeTargetMinX = 0.45f;
    public float freezeTargetMaxX = 0.55f;

    // Fire
    public float fireSpeedBonus = 3f;
    public float fireMaxDuration = 8f;
    public int fireHitLimit = 3;

    // Bomb
    public int bombHitLimit = 2;
    public int bombScoreMultiplier = 3;
    public float bombFlashHalfCycle = 0.15f;
    public float bombRotationSpeed = 360f;

    // Mirage
    public float mirageDuration = 6f;
    public float mirageRadius = 2f;
    public float mirageRotationSpeed = 400f;

    public float mirageBallMinAlpha = 0.1f;
    public float mirageFadeDuration = 1f;

    // Quick mode countdown
    public float firstQuickCountdownStep = 0.9f;
    public float normalQuickCountdownStep = 0.7f;
    public float lateQuickCountdownStep = 0.4f;
    public float lateQuickThresholdRatio = 0.25f;

    // Countdown visual
    public float countdownFadeStart = 0.2f;
    public float countdownFinalAlpha = 0.4f;
    public float countdownFinalScale = 0.8f;

    // Paddle visual
    public float swingPoseDuration = 0.5f;

    // Paddle Angle
    public float paddleHitAngleInfluence = 0.35f;

    // Portal spawn regions
    public float portalLeftMinX = 0.43f;
    public float portalLeftMaxX = 0.47f;

    public float portalRightMinX = 0.53f;
    public float portalRightMaxX = 0.57f;

    public float portalMinY = 0.27f;
    public float portalMaxY = 0.63f;

    public float portalExitVisualLifetime = 0.45f;

    // Portal timing
    public float portalTravelDelay = 1f;
    public float portalExitDelay = 0.3f;

    // Character selection
    public float selectedCharacterScale = 1.1f;

    public float selectionArrowDistance = 12f;
    public float selectionArrowCycle = 0.7f;

    // Pause
    public float pauseCountdownStep = 0.8f;

    [Header("Match entrance")]
    [Min(0.1f)] public float sideReadyCountdownStep = 0.8f;
    [Min(0.1f)] public float menuExitDuration = 0.65f;
    [Min(0.1f)] public float characterTravelDuration = 0.85f;
    [Min(0.1f)] public float selectionGlowFadeDuration = 0.45f;
    [Min(0.1f)] public float scoreboardEntranceDuration = 0.75f;
    [Min(0f)] public float entranceScreenMargin = 30f;
    [Min(0f)] public float characterEntranceHopHeight = 0.5f;
    [Min(0.1f)] public float characterEntranceHopUp = 0.45f;
    [Min(0.1f)] public float characterEntranceFlipDuration = 0.3f;
    [Min(0.1f)] public float characterEntranceHopDown = 0.45f;
    [Min(0.1f)] public float ballEntranceDropDuration = 0.65f;
    [Min(0f)] public float ballEntranceBounceHeight = 0.65f;
    [Min(0.1f)] public float ballEntranceBounceDuration = 0.42f;
    [Min(0.1f)] public float goalEntranceFlickerDuration = 0.7f;
    [Range(0.01f, 1f)] public float ballShadowMinimumScale = 0.08f;
    [Range(0f, 0.1f)] public float servePromptPulseAmount = 0.025f;
    [Min(0.1f)] public float servePromptPulsePeriod = 1.2f;
}
