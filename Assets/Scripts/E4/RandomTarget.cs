using UnityEngine;

public class RandomTarget : MonoBehaviour
{
    public int score = 0;
    public TextMesh scoreText;

    public GameObject targetPrefab;
    public GameObject currentTarget;

    public Vector2 targetBoundsCenter;
    public Vector2 targetBoundsSize;

    private Vector2 screenBounds;


    private void SpawnNewTarget()
    {
        Vector2 randomPosition = new Vector2(
            Random.Range(targetBoundsCenter.x - targetBoundsSize.x / 2f, targetBoundsCenter.x + targetBoundsSize.x / 2f),
            Random.Range(targetBoundsCenter.y - targetBoundsSize.y / 2f, targetBoundsCenter.y + targetBoundsSize.y / 2f)
        );

        currentTarget = Instantiate(targetPrefab, randomPosition, Quaternion.identity);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject == currentTarget)
        {
            score += 10;
            Destroy(currentTarget);
            currentTarget = null;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(targetBoundsCenter, targetBoundsSize);
    }

    private void Start()
    {
        screenBounds.y = Camera.main.orthographicSize;
        screenBounds.x = screenBounds.y * Camera.main.aspect;

        targetBoundsCenter = Camera.main.transform.position;
        targetBoundsSize = screenBounds * 2f;
    }

    private void Update()
    {
        if (currentTarget == null)
        {
            SpawnNewTarget();
        }
        scoreText.text = "Score: " + score;
    }
}