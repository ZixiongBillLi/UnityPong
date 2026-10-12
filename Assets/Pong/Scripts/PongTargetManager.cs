using System.Collections;
using UnityEngine;

public class PongTargetManager : MonoBehaviour
{
    public PongSettings settings;
    public GameObject targetPrefab;

    public GameObject freezeTargetPrefab;
    public GameObject fireTargetPrefab;
    public GameObject bombTargetPrefab;
    public GameObject mirageTargetPrefab;
    public GameObject portalTargetPrefab;

    public Camera gameCamera;

    private GameObject currentTarget;

    private Coroutine spawnRoutine;
    private Coroutine targetLifetimeRoutine;

    private bool firstServe = true;

    private TargetType lastTargetType;
    private bool hasLastTarget = false;

    private bool effectActive = false;


    public void OnBallLaunched()
    {
        float delay;

        if (firstServe)
        {
            delay = settings.firstServeTargetDelay;
            firstServe = false;
        }
        else
        {
            delay = settings.normalServeTargetDelay;
        }

        StartSpawnCountdown(delay);
    }


    public void OnGoalScored()
    {
        effectActive = false;

        StopSpawnCountdown();
        ClearCurrentTarget();
    }


    public void ResetForNewMatch()
    {
        firstServe = true;
        effectActive = false;

        StopSpawnCountdown();
        ClearCurrentTarget();

        hasLastTarget = false;
    }


    private void StartSpawnCountdown(float delay)
    {
        StopSpawnCountdown();

        if (effectActive){return;}

        spawnRoutine = StartCoroutine( SpawnAfterDelay(delay));
    }


    private IEnumerator SpawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        SpawnTarget();

        spawnRoutine = null;
    }


    private void SpawnTarget()
{
    TargetType nextType = GetNextTargetType();

    float minX = settings.targetMinX;
    float maxX = settings.targetMaxX;

    if (nextType == TargetType.Freeze)
    {
        minX = settings.freezeTargetMinX;
        maxX = settings.freezeTargetMaxX;
    }

    float viewportX = Random.Range(minX, maxX);

    float viewportY = Random.Range(settings.targetMinY, settings.targetMaxY);

    Vector3 viewportPosition = new Vector3(viewportX, viewportY, 0f);

    Vector3 worldPosition =  gameCamera.ViewportToWorldPoint(viewportPosition);

    worldPosition.z = 0f;

    GameObject selectedPrefab = GetTargetPrefab(nextType);

    if (selectedPrefab == null)
    {
        Debug.LogError("Missing prefab for target: " + nextType);

        return;
    }

    currentTarget = Instantiate(selectedPrefab, worldPosition, Quaternion.identity);

    PongTargetPickup pickup = currentTarget.GetComponent<PongTargetPickup>();

    pickup.Initialize(nextType, this);

    Debug.Log(
        "Spawned target: " + nextType + " at " + Time.time
    );

    targetLifetimeRoutine = StartCoroutine(TargetLifetime());
}


    private IEnumerator TargetLifetime()
    {
        SpriteRenderer[] targetRenderers = currentTarget.GetComponentsInChildren<SpriteRenderer>(true);

        yield return new WaitForSeconds(settings.targetBlinkStartTime);

        float blinkDuration = settings.targetLifetime - settings.targetBlinkStartTime;

        float elapsedTime = 0f;

        while (elapsedTime < blinkDuration)
        {
            foreach (SpriteRenderer renderer in targetRenderers)
            {
                renderer.enabled = !renderer.enabled;
            }

            yield return new WaitForSeconds(0.15f);

            elapsedTime += 0.15f;
        }

        targetLifetimeRoutine = null;

        if (currentTarget != null)
        {
            Destroy(currentTarget);
            currentTarget = null;
        }

        StartSpawnCountdown(settings.targetRespawnDelay);
    }


    private void ClearCurrentTarget()
    {
        if (targetLifetimeRoutine != null)
        {
            StopCoroutine(targetLifetimeRoutine);
            targetLifetimeRoutine = null;
        }

        if (currentTarget != null)
        {
            Destroy(currentTarget);
            currentTarget = null;
        }
    }


    private void StopSpawnCountdown()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }


    private GameObject GetTargetPrefab(TargetType targetType)
    {
        switch (targetType)
        {
            case TargetType.Freeze:
                return freezeTargetPrefab;

            case TargetType.Fire:
                return fireTargetPrefab;

            case TargetType.Bomb:
                return bombTargetPrefab;

            case TargetType.Mirage:
                return mirageTargetPrefab;

            case TargetType.Portal:
                return portalTargetPrefab;

            default:
                return null;
        }
    }

    private TargetType GetNextTargetType()
    {
        return TargetType.Portal;
        // return TargetType.Mirage;
        // return TargetType.Freeze;
        // return TargetType.Bomb;
        // return TargetType.Fire;
        // TargetType[] targetTypes =
        //     (TargetType[])System.Enum.GetValues(
        //         typeof(TargetType)
        //     );

        // TargetType nextType;

        // do
        // {
        //     int randomIndex = Random.Range(0, targetTypes.Length);

        //     nextType = targetTypes[randomIndex];
        // }
        // while (hasLastTarget && nextType == lastTargetType
        // );

        // lastTargetType = nextType;
        // hasLastTarget = true;

        // return nextType;
    }

    public void OnTargetCollected()
    {
        effectActive = true;

        StopSpawnCountdown();
        ClearCurrentTarget();
    }

    public void OnEffectEnded()
    {
        if (!effectActive)
        {
            return;
        }

        effectActive = false;

        StartSpawnCountdown(settings.targetRespawnAfterEffect);
    }

    public Vector2 GetPortalExitPosition( Vector2 entranceWorldPosition, Vector2 ballDirection)
    {
        Vector3 entranceViewport = gameCamera.WorldToViewportPoint(entranceWorldPosition);

        float exitX;
        float exitY;

        bool entranceIsLeft = entranceViewport.x < 0.5f;

        bool ballMovingRight = ballDirection.x > 0f;

        // reflect vertically
        exitY = 0.9f - entranceViewport.y;

        if ((entranceIsLeft && ballMovingRight) || (!entranceIsLeft && !ballMovingRight))
        {
            // use center symmetry
            exitX = 1f - entranceViewport.x;
        }
        else
        {
            // stay in the same half
            exitX = entranceViewport.x;
        }

        Vector3 exitViewport = new Vector3(exitX, exitY, 0f);

        Vector3 exitWorld = gameCamera.ViewportToWorldPoint(exitViewport);

        return new Vector2(exitWorld.x, exitWorld.y);
    }
}