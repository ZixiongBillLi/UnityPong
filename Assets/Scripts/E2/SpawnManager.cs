using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public GameObject ballPrefab;

    private GameObject currentBall;

    void Update()
    {
        if (currentBall == null)
        {
            Spawn();
        }
    }

    void Spawn()
    {
        currentBall = Instantiate(
            ballPrefab,
            transform.position,
            Quaternion.identity
        );
    }
}