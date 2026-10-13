using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
[RequireComponent(typeof(PongBallController), typeof(Rigidbody2D))]
public class PongBallTrail : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform visualAnchor;
    [SerializeField] private SpriteRenderer ballSprite;

    [Header("Appearance")]
    [SerializeField, Min(0.01f)] private float trailDuration = 0.34f;
    [SerializeField, Range(0.1f, 2f)] private float widthRatio = 1.03f;
    [SerializeField, Min(0.001f)] private float minVertexDistance = 0.02f;
    [SerializeField] private Color trailColor = new Color(0.92f, 0.97f, 1f, 0.8f);
    [SerializeField] private bool smoothMovement = true;

    [Header("Effect Colors (Core / Band / Glow)")]
    [SerializeField, Min(0f)] private float freezeTrailDuration = 2f;
    [SerializeField] private TrailPalette freezeColors = new TrailPalette(
        new Color(0.8f, 0.98f, 1f, 0.95f), new Color(0.04f, 0.73f, 1f, 0.7f), new Color(0.02f, 0.23f, 0.72f, 0.35f));
    [SerializeField] private TrailPalette fireColors = new TrailPalette(
        new Color(1f, 0.94f, 0.3f, 0.95f), new Color(1f, 0.4f, 0.03f, 0.75f), new Color(0.95f, 0.05f, 0.01f, 0.38f));
    [SerializeField] private TrailPalette bombColors = new TrailPalette(
        new Color(1f, 0.23f, 0.28f, 0.9f), new Color(0.85f, 0.025f, 0.09f, 0.75f), new Color(0.38f, 0.005f, 0.03f, 0.5f));
    [SerializeField] private TrailPalette mirageBlueColors = new TrailPalette(
        new Color(0.85f, 0.97f, 1f, 0.8f), new Color(0.36f, 0.77f, 1f, 0.5f), new Color(0.15f, 0.4f, 0.9f, 0.22f));
    [SerializeField] private TrailPalette miragePurpleColors = new TrailPalette(
        new Color(0.94f, 0.86f, 1f, 0.8f), new Color(0.7f, 0.46f, 1f, 0.5f), new Color(0.46f, 0.16f, 0.85f, 0.22f));

    [System.Serializable]
    private class TrailPalette
    {
        public Color core;
        public Color band;
        public Color glow;

        public TrailPalette(Color core, Color band, Color glow)
        {
            this.core = core;
            this.band = band;
            this.glow = glow;
        }
    }

    private enum TrailStyle { Normal, Freeze, Fire, Bomb, MirageBlue, MiragePurple }

    private class TrailGroup
    {
        public GameObject root;
        public SpriteRenderer sprite;
        // Widest glow first, then band, then the narrow bright core.
        public TrailRenderer[] layers;
        public TrailStyle style;
        public bool appearanceDirty = true;
    }

    private PongBallController ball;
    private PongBombVisuals bombVisuals;
    private SpriteRenderer[] fireRenderers;
    private Rigidbody2D body;
    private RigidbodyInterpolation2D originalInterpolation;
    private TrailGroup realTrail;
    private TrailGroup mirageBlueTrail;
    private TrailGroup miragePurpleTrail;
    private TrailPalette normalColors;
    private Material trailMaterial;
    private Texture2D trailTexture;
    private bool wasTeleporting;
    private bool appearanceDirty;
    private bool interpolationApplied;
    private TargetType? previousEffect;
    private float freezeTrailRemaining;

    private void Awake()
    {
        ball = GetComponent<PongBallController>();
        bombVisuals = GetComponent<PongBombVisuals>();
        PongFireVisuals fireVisuals = GetComponent<PongFireVisuals>();
        if (fireVisuals != null && fireVisuals.fireFrames != null)
        {
            fireRenderers = new SpriteRenderer[fireVisuals.fireFrames.Length];
            for (int i = 0; i < fireRenderers.Length; i++)
            {
                if (fireVisuals.fireFrames[i] != null)
                    fireRenderers[i] = fireVisuals.fireFrames[i].GetComponent<SpriteRenderer>();
            }
        }
        body = GetComponent<Rigidbody2D>();
        if (ballSprite == null && ball.ballVisual != null)
            ballSprite = ball.ballVisual.GetComponent<SpriteRenderer>();
        if (visualAnchor == null && ballSprite != null)
            visualAnchor = ballSprite.transform.parent;

        if (visualAnchor == null || ballSprite == null || ballSprite.sprite == null ||
            visualAnchor == transform || !visualAnchor.IsChildOf(transform))
        {
            Debug.LogError("PongBallTrail: assign Ball/HeightRoot as Visual Anchor and " +
                "Ball/HeightRoot/BallVisual's SpriteRenderer as Ball Sprite.", this);
            enabled = false;
            return;
        }

        // Reuse the existing sprite shader so it is already included in the build.
        Shader shader = ballSprite.sharedMaterial != null ? ballSprite.sharedMaterial.shader : null;
        if (shader == null || shader.name != "Sprites/Default")
        {
            Debug.LogError("PongBallTrail: Ball Sprite must use the project's Sprites/Default shader.", this);
            enabled = false;
            return;
        }

        trailTexture = CreateSoftTexture();
        trailMaterial = new Material(shader) { name = "Ball Trail (Runtime)", mainTexture = trailTexture };
        realTrail = CreateGroup(visualAnchor, ballSprite, "BallTrail (Runtime)");
        if (ball.mirageVisuals != null)
        {
            mirageBlueTrail = CreateMirageGroup(ball.mirageVisuals.mirageA, "MirageBlueTrail (Runtime)");
            miragePurpleTrail = CreateMirageGroup(ball.mirageVisuals.mirageB, "MiragePurpleTrail (Runtime)");
        }
        ApplyAppearance();
    }

    private void OnEnable()
    {
        if (realTrail == null)
            return;
        originalInterpolation = body.interpolation;
        if (smoothMovement)
        {
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            interpolationApplied = true;
        }
        ball.ServeReset += ResetTrailState;
        ResetTrailState();
    }

    private void LateUpdate()
    {
        if (realTrail == null)
            return;
        if (appearanceDirty)
            ApplyAppearance();

        bool teleporting = ball.IsTeleporting;
        TargetType? effect = ball.ActiveEffect;
        // Freeze emits nothing while stopped; its two-second color window starts at thaw.
        if (effect == TargetType.Freeze)
            freezeTrailRemaining = 0f;
        else if (effect != null)
            freezeTrailRemaining = 0f;
        else if (previousEffect == TargetType.Freeze && ball.IsMoving())
            freezeTrailRemaining = Mathf.Max(0f, freezeTrailDuration);
        else
            freezeTrailRemaining = Mathf.Max(0f, freezeTrailRemaining - Time.deltaTime);
        previousEffect = effect;

        if (teleporting || wasTeleporting || !ball.IsMoving() ||
            !visualAnchor.gameObject.activeInHierarchy ||
            (!ball.isActiveAndEnabled && Time.timeScale > 0f))
            ClearAllTrails();

        wasTeleporting = teleporting;
        bool available = ball.IsMoving() && visualAnchor.gameObject.activeInHierarchy &&
            (ball.isActiveAndEnabled || Time.timeScale == 0f) && !teleporting;
        bool mirageActive = effect == TargetType.Mirage;
        bool emitting = available && Time.timeScale > 0f && ball.isActiveAndEnabled &&
            ball.GetVelocity().sqrMagnitude > 0.0001f && effect != TargetType.Freeze;

        TrailStyle style = effect == TargetType.Fire ? TrailStyle.Fire :
            effect == TargetType.Bomb ? TrailStyle.Bomb :
            freezeTrailRemaining > 0f ? TrailStyle.Freeze : TrailStyle.Normal;
        UpdateGroup(realTrail, style, available && !mirageActive, emitting && !mirageActive,
            RealSortingRenderer(style));
        UpdateGroup(mirageBlueTrail, TrailStyle.MirageBlue,
            available && mirageActive && MirageVisible(mirageBlueTrail), emitting && mirageActive);
        UpdateGroup(miragePurpleTrail, TrailStyle.MiragePurple,
            available && mirageActive && MirageVisible(miragePurpleTrail), emitting && mirageActive);
    }

    private static bool MirageVisible(TrailGroup group) => group != null &&
        group.sprite.enabled && group.sprite.gameObject.activeInHierarchy;

    private void ResetTrailState()
    {
        previousEffect = null;
        freezeTrailRemaining = 0f;
        wasTeleporting = false;
        ClearAllTrails();
    }

    private void ClearAllTrails()
    {
        ClearGroup(realTrail);
        ClearGroup(mirageBlueTrail);
        ClearGroup(miragePurpleTrail);
    }

    private static void ClearGroup(TrailGroup group)
    {
        if (group == null)
            return;
        foreach (TrailRenderer layer in group.layers)
        {
            layer.emitting = false;
            layer.Clear();
        }
    }

    private void ApplyAppearance()
    {
        normalColors = new TrailPalette(trailColor,
            new Color(trailColor.r * 0.85f, trailColor.g * 0.95f, trailColor.b, trailColor.a * 0.55f),
            new Color(trailColor.r * 0.6f, trailColor.g * 0.8f, trailColor.b, trailColor.a * 0.22f));
        if (realTrail != null) realTrail.appearanceDirty = true;
        if (mirageBlueTrail != null) mirageBlueTrail.appearanceDirty = true;
        if (miragePurpleTrail != null) miragePurpleTrail.appearanceDirty = true;
        appearanceDirty = false;
    }

    private TrailPalette ColorsFor(TrailStyle style)
    {
        switch (style)
        {
            case TrailStyle.Freeze: return freezeColors;
            case TrailStyle.Fire: return fireColors;
            case TrailStyle.Bomb: return bombColors;
            case TrailStyle.MirageBlue: return mirageBlueColors;
            case TrailStyle.MiragePurple: return miragePurpleColors;
            default: return normalColors;
        }
    }

    private SpriteRenderer RealSortingRenderer(TrailStyle style)
    {
        SpriteRenderer renderer = null;
        if (style == TrailStyle.Bomb && bombVisuals != null)
            renderer = LowerRenderer(bombVisuals.blackBombRenderer, bombVisuals.redBombRenderer);
        else if (style == TrailStyle.Fire && fireRenderers != null)
        {
            foreach (SpriteRenderer frame in fireRenderers)
                renderer = LowerRenderer(renderer, frame);
        }
        return renderer != null ? renderer : ballSprite;
    }

    private static SpriteRenderer LowerRenderer(SpriteRenderer first, SpriteRenderer second)
    {
        if (first == null) return second;
        if (second == null) return first;
        int firstLayer = SortingLayer.GetLayerValueFromID(first.sortingLayerID);
        int secondLayer = SortingLayer.GetLayerValueFromID(second.sortingLayerID);
        return firstLayer < secondLayer ||
            (firstLayer == secondLayer && first.sortingOrder <= second.sortingOrder) ? first : second;
    }

    private void UpdateGroup(TrailGroup group, TrailStyle style, bool visible, bool emitting,
        SpriteRenderer sortingRenderer = null)
    {
        if (group == null)
            return;
        if (!visible)
        {
            if (group.layers[0].enabled)
                ClearGroup(group);
            foreach (TrailRenderer layer in group.layers)
            {
                layer.enabled = false;
                layer.emitting = false;
            }
            return;
        }

        bool recolor = group.appearanceDirty || group.style != style;
        TrailPalette palette = ColorsFor(style);
        if (sortingRenderer == null)
            sortingRenderer = group.sprite;
        // Width is measured in world space, including each illusion's own Inspector scale.
        Vector3 size = group.sprite.sprite.bounds.size;
        Vector3 scale = group.sprite.transform.lossyScale;
        float width = Mathf.Min(size.x * Mathf.Abs(scale.x), size.y * Mathf.Abs(scale.y)) * widthRatio;
        for (int i = 0; i < group.layers.Length; i++)
        {
            TrailRenderer layer = group.layers[i];
            layer.enabled = true;
            layer.emitting = emitting;
            layer.widthMultiplier = width * (i == 0 ? 1.75f : i == 1 ? 1f : 0.4f);
            layer.sortingLayerID = sortingRenderer.sortingLayerID;
            layer.sortingOrder = sortingRenderer.sortingOrder - 3 + i;
            if (recolor)
            {
                layer.time = Mathf.Max(0.01f, trailDuration);
                layer.minVertexDistance = Mathf.Max(0.001f, minVertexDistance);
                Color color = i == 0 ? palette.glow : i == 1 ? palette.band : palette.core;
                layer.startColor = color;
                color.a = 0f;
                layer.endColor = color;
            }
        }
        group.style = style;
        group.appearanceDirty = false;
    }

    private void OnValidate() => appearanceDirty = true;

    private void OnDisable()
    {
        if (ball != null)
            ball.ServeReset -= ResetTrailState;
        ResetTrailState();
        if (body != null && interpolationApplied)
        {
            body.interpolation = originalInterpolation;
            interpolationApplied = false;
        }
    }

    private void OnDestroy()
    {
        if (realTrail != null)
            Destroy(realTrail.root);
        if (mirageBlueTrail != null)
            Destroy(mirageBlueTrail.root);
        if (miragePurpleTrail != null)
            Destroy(miragePurpleTrail.root);
        if (trailMaterial != null)
            Destroy(trailMaterial);
        if (trailTexture != null)
            Destroy(trailTexture);
    }

    private TrailGroup CreateMirageGroup(GameObject illusion, string groupName)
    {
        if (illusion == null)
            return null;
        SpriteRenderer sprite = illusion.GetComponent<SpriteRenderer>();
        if (sprite == null || sprite.sprite == null)
            return null;
        return CreateGroup(illusion.transform, sprite, groupName);
    }

    private TrailGroup CreateGroup(Transform anchor, SpriteRenderer sprite, string groupName)
    {
        TrailGroup group = new TrailGroup
        {
            root = new GameObject(groupName),
            sprite = sprite,
            layers = new TrailRenderer[3]
        };
        group.root.layer = anchor.gameObject.layer;
        group.root.transform.SetParent(anchor, false);
        for (int i = 0; i < group.layers.Length; i++)
        {
            GameObject layerObject = new GameObject(i == 0 ? "Glow" : i == 1 ? "Band" : "Core");
            layerObject.layer = anchor.gameObject.layer;
            layerObject.transform.SetParent(group.root.transform, false);
            TrailRenderer layer = layerObject.AddComponent<TrailRenderer>();
            layer.enabled = false;
            layer.emitting = false;
            layer.autodestruct = false;
            layer.sharedMaterial = trailMaterial;
            layer.alignment = LineAlignment.View;
            layer.textureMode = LineTextureMode.Stretch;
            layer.numCornerVertices = 4;
            layer.numCapVertices = 4;
            layer.generateLightingData = false;
            layer.shadowCastingMode = ShadowCastingMode.Off;
            layer.receiveShadows = false;
            layer.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            group.layers[i] = layer;
        }
        return group;
    }

    private static Texture2D CreateSoftTexture()
    {
        // Uniform along the trail, with a white core and transparent outer edges.
        const int height = 64;
        Texture2D texture = new Texture2D(2, height, TextureFormat.RGBA32, false)
        {
            name = "Ball Trail Soft Edge (Runtime)",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        Color[] pixels = new Color[2 * height];
        for (int y = 0; y < height; y++)
        {
            float distance = Mathf.Abs(2f * y / (height - 1) - 1f);
            float alpha = Mathf.Clamp01((Mathf.Exp(-4f * distance * distance) - Mathf.Exp(-4f)) /
                (1f - Mathf.Exp(-4f)));
            pixels[2 * y] = pixels[2 * y + 1] = new Color(1f, 1f, 1f, alpha);
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }
}
