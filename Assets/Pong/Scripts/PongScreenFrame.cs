using UnityEngine;

// Keeps the world camera and its Screen Space - Camera Canvas on the same 16:10 frame.
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
[DefaultExecutionOrder(-200)]
public class PongScreenFrame : MonoBehaviour
{
    [SerializeField] private Vector2 designSize = new Vector2(1600f, 1000f);
    private Camera gameCamera;
    private Camera backgroundCamera;
    private Rect originalRect;
    private CameraClearFlags originalClearFlags;
    private Color originalBackground;
    private int lastWidth;
    private int lastHeight;

    private void Awake()
    {
        gameCamera = GetComponent<Camera>();
        originalRect = gameCamera.rect;
        originalClearFlags = gameCamera.clearFlags;
        originalBackground = gameCamera.backgroundColor;
    }

    private void OnEnable()
    {
        var background = new GameObject("Pong frame background (runtime)");
        background.transform.SetParent(transform, false);
        backgroundCamera = background.AddComponent<Camera>();
        backgroundCamera.cullingMask = 0;
        backgroundCamera.clearFlags = CameraClearFlags.SolidColor;
        backgroundCamera.backgroundColor = Color.black;
        backgroundCamera.depth = gameCamera.depth - 1f;
        backgroundCamera.targetDisplay = gameCamera.targetDisplay;
        gameCamera.clearFlags = CameraClearFlags.SolidColor;
        gameCamera.backgroundColor = Color.black;
        UpdateFrame();
    }

    private void LateUpdate()
    {
        if (Screen.width != lastWidth || Screen.height != lastHeight)
            UpdateFrame();
    }

    private void UpdateFrame()
    {
        lastWidth = Screen.width;
        lastHeight = Screen.height;
        float targetAspect = Mathf.Max(1f, designSize.x) / Mathf.Max(1f, designSize.y);
        float screenAspect = (float)Mathf.Max(1, Screen.width) / Mathf.Max(1, Screen.height);
        gameCamera.rect = CalculateFrame(screenAspect, targetAspect);
        gameCamera.aspect = targetAspect;
    }

    internal static Rect CalculateFrame(float screenAspect, float targetAspect)
    {
        if (screenAspect > targetAspect)
        {
            float width = targetAspect / screenAspect;
            return new Rect((1f - width) * 0.5f, 0f, width, 1f);
        }
        else
        {
            float height = screenAspect / targetAspect;
            return new Rect(0f, (1f - height) * 0.5f, 1f, height);
        }
    }

    private void OnDisable()
    {
        if (backgroundCamera != null)
        {
            backgroundCamera.enabled = false;
            Destroy(backgroundCamera.gameObject);
        }
        gameCamera.rect = originalRect;
        gameCamera.ResetAspect();
        gameCamera.clearFlags = originalClearFlags;
        gameCamera.backgroundColor = originalBackground;
    }
}
