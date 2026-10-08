using UnityEngine;

public class Bucket : MonoBehaviour
{
    [Min(0)] public int bucketValue = 100;
    public ScoreManager scoreManager;

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlinkoChip chip = other.GetComponent<PlinkoChip>();

        if (chip == null || !chip.TryScore())
        {
            return;
        }

        if (scoreManager != null)
        {
            scoreManager.AddScore(bucketValue);
        }

        Destroy(other.gameObject);
    }
}
