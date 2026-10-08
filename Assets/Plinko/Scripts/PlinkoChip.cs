using UnityEngine;

public class PlinkoChip : MonoBehaviour
{
    private bool hasScored;

    public bool TryScore()
    {
        if (hasScored)
        {
            return false;
        }

        hasScored = true;
        return true;
    }
}
