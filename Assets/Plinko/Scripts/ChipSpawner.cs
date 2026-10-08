using UnityEngine;
using UnityEngine.InputSystem;

public class ChipSpawner : MonoBehaviour
{
    public GameObject chipPrefab;
    public Collider2D spawnArea;

    private InputAction clickAction;
    private InputAction pointerPositionAction;

    private void Awake()
    {
        clickAction = new InputAction(
            name: "DropChip",
            type: InputActionType.Button,
            binding: "<Mouse>/leftButton"
        );

        pointerPositionAction = new InputAction(
            name: "PointerPosition",
            type: InputActionType.PassThrough,
            binding: "<Mouse>/position"
        );
    }

    private void OnEnable()
    {
        clickAction.performed += OnClick;
        pointerPositionAction.Enable();
        clickAction.Enable();
    }

    private void OnDisable()
    {
        clickAction.performed -= OnClick;
        clickAction.Disable();
        pointerPositionAction.Disable();
    }

    private void OnDestroy()
    {
        clickAction?.Dispose();
        pointerPositionAction?.Dispose();
    }

    private void OnClick(InputAction.CallbackContext context)
    {
        TrySpawnChip();
    }

    private void TrySpawnChip()
    {
        if (chipPrefab == null || spawnArea == null || Camera.main == null)
        {
            return;
        }

        Vector2 screenPosition = pointerPositionAction.ReadValue<Vector2>();
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, 0f)
        );
        worldPosition.z = 0f;

        if (spawnArea.OverlapPoint(worldPosition))
        {
            Instantiate(chipPrefab, worldPosition, Quaternion.identity);
        }
    }
}
