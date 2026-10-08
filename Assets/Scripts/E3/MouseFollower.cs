using UnityEngine;

public class MouseFollower : MonoBehaviour
{
    public MouseInputReader mouseInput;

    private Vector3 mousePosition;

    void Update()
    {
        MoveToMouse();
    }

    private void MoveToMouse()
    {
        mousePosition = mouseInput.MousePosition;
        transform.position = mousePosition;
    }
}