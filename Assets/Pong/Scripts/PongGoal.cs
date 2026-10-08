using UnityEngine;

public class PongGoal : MonoBehaviour
{
    public PongGameManager gameManager;
    
    public PaddleSide goalSide;

    public GameObject goalShockwavePrefab;

    private void OnTriggerEnter2D(Collider2D other)
    {        
        PongBallController ball = other.GetComponent<PongBallController>();

        if (ball != null)
        {
            Vector3 goalPosition = other.transform.position;

            Instantiate(goalShockwavePrefab, goalPosition, Quaternion.identity);

            gameManager.ScoreGoal(goalSide);
        }
    }
}