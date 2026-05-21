using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public sealed class ClickableDeskObject : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public static event System.Action<ClickableDeskObject> Clicked;

    [SerializeField] private string objectId;
    [SerializeField] private string displayName;
    [SerializeField] private Color hoverOutlineColor = new Color(1f, 0.85f, 0.3f);
    [SerializeField] private float hoverOutlineThickness = 0.11f;
    [SerializeField] private UnityEvent<string> onClicked = new UnityEvent<string>();

    private const string HoverFrameName = "_HoverOutline";
    private static readonly Vector2[] OutlineOffsets =
    {
        new Vector2(1f, 0f),
        new Vector2(-1f, 0f),
        new Vector2(0f, 1f),
        new Vector2(0f, -1f),
        new Vector2(0.7f, 0.7f),
        new Vector2(-0.7f, 0.7f),
        new Vector2(0.7f, -0.7f),
        new Vector2(-0.7f, -0.7f),
        new Vector2(1f, 0.45f),
        new Vector2(-1f, 0.45f),
        new Vector2(1f, -0.45f),
        new Vector2(-1f, -0.45f),
        new Vector2(0.45f, 1f),
        new Vector2(-0.45f, 1f),
        new Vector2(0.45f, -1f),
        new Vector2(-0.45f, -1f),
    };

    private GameObject hoverFrame;
    private readonly List<SpriteRenderer> hoverOutlineRenderers = new List<SpriteRenderer>();
    private int lastClickFrame = -1;

    public string ObjectId => objectId;
    public string DisplayName => displayName;

    private void Awake()
    {
        EnsureClickableCollider();
        EnsureHoverFrame();
        SetHoverFrameVisible(false);
    }

    private void OnEnable()
    {
        EnsureClickableCollider();
        EnsureHoverFrame();
        SetHoverFrameVisible(false);
    }

    public void Initialize(string id, string readableName)
    {
        objectId = id;
        displayName = readableName;
        gameObject.name = readableName;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        TriggerClick();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowHoverFrame();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHoverFrameVisible(false);
    }

    private void OnMouseEnter()
    {
        ShowHoverFrame();
    }

    private void OnMouseExit()
    {
        SetHoverFrameVisible(false);
    }

    private void OnMouseDown()
    {
        TriggerClick();
    }

    private void TriggerClick()
    {
        if (lastClickFrame == Time.frameCount)
        {
            return;
        }

        lastClickFrame = Time.frameCount;
        Debug.Log($"Clicked desk object: {displayName} ({objectId})", this);
        onClicked.Invoke(objectId);
        Clicked?.Invoke(this);
    }

    private void ShowHoverFrame()
    {
        EnsureHoverFrame();
        UpdateHoverFrameShape();
        SetHoverFrameVisible(true);
    }

    private void EnsureClickableCollider()
    {
        if (GetComponent<Collider2D>() != null)
        {
            return;
        }

        if (!TryGetLocalBounds(out Bounds localBounds))
        {
            return;
        }

        BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.offset = localBounds.center;
        box.size = localBounds.size;
    }

    private void EnsureHoverFrame()
    {
        if (hoverFrame != null)
        {
            return;
        }

        Transform existingFrame = transform.Find(HoverFrameName);
        hoverFrame = existingFrame != null ? existingFrame.gameObject : new GameObject(HoverFrameName);
        hoverFrame.transform.SetParent(transform, false);
        hoverFrame.transform.localPosition = Vector3.zero;
        hoverFrame.transform.localRotation = Quaternion.identity;
        hoverFrame.transform.localScale = Vector3.one;

        UpdateHoverFrameShape();
    }

    private void UpdateHoverFrameShape()
    {
        SpriteRenderer sourceRenderer = GetComponent<SpriteRenderer>();
        if (hoverFrame == null || sourceRenderer == null || sourceRenderer.sprite == null)
        {
            return;
        }

        EnsureOutlineRendererCount(OutlineOffsets.Length);
        Vector3 worldScale = transform.lossyScale;
        float averageScale = (Mathf.Abs(worldScale.x) + Mathf.Abs(worldScale.y)) * 0.5f;
        float localOutlineThickness = hoverOutlineThickness / Mathf.Max(averageScale, 0.0001f);

        for (int i = 0; i < hoverOutlineRenderers.Count; i++)
        {
            SpriteRenderer outlineRenderer = hoverOutlineRenderers[i];
            Vector2 offset = OutlineOffsets[i].normalized * localOutlineThickness;

            outlineRenderer.sprite = sourceRenderer.sprite;
            outlineRenderer.drawMode = sourceRenderer.drawMode;
            outlineRenderer.size = sourceRenderer.size;
            outlineRenderer.flipX = sourceRenderer.flipX;
            outlineRenderer.flipY = sourceRenderer.flipY;
            outlineRenderer.maskInteraction = sourceRenderer.maskInteraction;
            outlineRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
            outlineRenderer.sortingOrder = sourceRenderer.sortingOrder - 1;
            outlineRenderer.color = hoverOutlineColor;

            Transform outlineTransform = outlineRenderer.transform;
            outlineTransform.localPosition = new Vector3(offset.x, offset.y, 0.02f);
            outlineTransform.localRotation = Quaternion.identity;
            outlineTransform.localScale = Vector3.one;
        }
    }

    private void EnsureOutlineRendererCount(int count)
    {
        while (hoverOutlineRenderers.Count < count)
        {
            GameObject outlineObject = new GameObject($"Outline {hoverOutlineRenderers.Count + 1}");
            outlineObject.transform.SetParent(hoverFrame.transform, false);

            SpriteRenderer outlineRenderer = outlineObject.AddComponent<SpriteRenderer>();
            hoverOutlineRenderers.Add(outlineRenderer);
        }
    }

    private void SetHoverFrameVisible(bool isVisible)
    {
        if (hoverFrame != null)
        {
            hoverFrame.SetActive(isVisible);
        }
    }

    private bool TryGetLocalBounds(out Bounds localBounds)
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            localBounds = new Bounds(box.offset, box.size);
            return true;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        localBounds = default;
        bool hasBounds = false;
        Transform outlineRoot = hoverFrame != null ? hoverFrame.transform : transform.Find(HoverFrameName);

        foreach (Renderer renderer in renderers)
        {
            if (outlineRoot != null && renderer.transform.IsChildOf(outlineRoot))
            {
                continue;
            }

            if (!renderer.transform.IsChildOf(transform))
            {
                continue;
            }

            Bounds rendererBounds = renderer.bounds;
            EncapsulateWorldPoint(ref localBounds, ref hasBounds, rendererBounds.min);
            EncapsulateWorldPoint(ref localBounds, ref hasBounds, rendererBounds.max);
            EncapsulateWorldPoint(ref localBounds, ref hasBounds, new Vector3(rendererBounds.min.x, rendererBounds.max.y, rendererBounds.min.z));
            EncapsulateWorldPoint(ref localBounds, ref hasBounds, new Vector3(rendererBounds.max.x, rendererBounds.min.y, rendererBounds.min.z));
        }

        return hasBounds;
    }

    private void EncapsulateWorldPoint(ref Bounds bounds, ref bool hasBounds, Vector3 worldPoint)
    {
        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);

        if (!hasBounds)
        {
            bounds = new Bounds(localPoint, Vector3.zero);
            hasBounds = true;
            return;
        }

        bounds.Encapsulate(localPoint);
    }

    private void OnDrawGizmos()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null)
        {
            return;
        }

        Gizmos.color = new Color(0.1f, 0.65f, 1f, 0.85f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(box.offset, box.size);
    }
}
