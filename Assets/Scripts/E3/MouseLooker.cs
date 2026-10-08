using UnityEngine;

public class MouseLooker : MonoBehaviour
{
    private MouseInputReader mouseInput;

    private Vector3 mousePosition;

    void Start()
    {
        mouseInput = FindFirstObjectByType<MouseInputReader>();
    }

    void Update()
    {
        LookAt();
    }

    private void LookAt()
    {
        mousePosition = mouseInput.MousePosition;

        Vector3 localLookVect =
            mousePosition - transform.position;

        float lookAngle =
            Mathf.Atan2(localLookVect.y, localLookVect.x)
            * Mathf.Rad2Deg - 90f;

        transform.rotation =
            Quaternion.Euler(0f, 0f, lookAngle);
    }
}