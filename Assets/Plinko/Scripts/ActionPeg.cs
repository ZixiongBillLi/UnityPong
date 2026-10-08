using System.Collections.Generic;
using UnityEngine;

public class ActionPeg : MonoBehaviour
{
    [Min(0)] public int bonusPoints = 25;
    public ScoreManager scoreManager;

    // Prevents one chip from farming the same bonus peg repeatedly.
    private readonly HashSet<int> scoredChipIds = new HashSet<int>();

    private void OnCollisionEnter2D(Collision2D collision)
    {
        PlinkoChip chip = collision.gameObject.GetComponent<PlinkoChip>();

        if (chip == null)
        {
            return;
        }

        int chipId = chip.gameObject.GetInstanceID();
        if (!scoredChipIds.Add(chipId))
        {
            return;
        }

        if (scoreManager != null)
        {
            scoreManager.AddScore(bonusPoints);
        }
    }
}
