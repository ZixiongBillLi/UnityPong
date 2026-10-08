using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class InputSpawner : MonoBehaviour
{
    public GameObject trianglePrefab;

    public InputActionReference clickAction;

    public MouseInputReader mouseInput;

    public List<GameObject> triangleInstanceList =
        new List<GameObject>();

    private void OnEnable()
    {
        clickAction.action.performed += OnClick;
        clickAction.action.Enable();
    }

    private void OnDisable()
    {
        clickAction.action.performed -= OnClick;
        clickAction.action.Disable();
    }

    private void OnClick(InputAction.CallbackContext context)
    {
        SpawnTriangle();
    }

    private void SpawnTriangle()
    {
        Vector3 spawnPosition =
            mouseInput.MousePosition;

        GameObject newTriangle =
            Instantiate(
                trianglePrefab,
                spawnPosition,
                Quaternion.identity
            );

        triangleInstanceList.Add(newTriangle);
    }
}