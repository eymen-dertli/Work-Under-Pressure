using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private string itemId;
    [SerializeField] private bool correctItem;
    [SerializeField] private bool returnToHomeOnEnd = true;
    [SerializeField] private bool canBeThrown;
    [SerializeField] private float throwMultiplier = 1.15f;
    [SerializeField] private float throwDamping = 4.5f;
    [SerializeField] private float maxThrowSpeed = 1800f;

    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Transform homeParent;
    private Vector2 homeAnchoredPosition;
    private Vector2 lastDragPosition;
    private Vector2 currentVelocity;
    private float lastDragTime;
    private bool isDragging;
    private bool isFlying;
    private bool dropHandled;

    public event Action<DraggableItem> DragBegan;
    public event Action<DraggableItem> DragEnded;

    public string ItemId => itemId;
    public bool IsCorrectItem => correctItem;
    public RectTransform RectTransform => rectTransform != null ? rectTransform : GetComponent<RectTransform>();

    private void Awake()
    {
        CacheComponents();
        CaptureHome();
    }

    private void OnEnable()
    {
        CacheComponents();
        EnableRaycastTarget();
    }

    private void Update()
    {
        if (!isFlying || isDragging || rectTransform == null)
        {
            return;
        }

        float deltaTime = Time.unscaledDeltaTime;
        rectTransform.anchoredPosition += currentVelocity * deltaTime;
        currentVelocity = Vector2.Lerp(currentVelocity, Vector2.zero, throwDamping * deltaTime);

        foreach (DropZone dropZone in DropZone.ActiveZones)
        {
            if (dropZone != null && dropZone.CanReceiveThrownItems && dropZone.Overlaps(RectTransform))
            {
                dropZone.TryReceive(this, true);
                break;
            }
        }

        if (currentVelocity.sqrMagnitude < 25f)
        {
            isFlying = false;
            if (returnToHomeOnEnd && !dropHandled)
            {
                ReturnToHome();
            }
        }
    }

    public void Configure(string newItemId, bool isCorrectItem, bool allowThrow = false)
    {
        itemId = newItemId;
        correctItem = isCorrectItem;
        canBeThrown = allowThrow;
        CacheComponents();
        EnableRaycastTarget();
        CaptureHome();
    }

    public void CaptureHome()
    {
        CacheComponents();
        if (rectTransform == null)
        {
            return;
        }

        homeParent = transform.parent;
        homeAnchoredPosition = rectTransform.anchoredPosition;
    }

    public void ReturnToHome()
    {
        CacheComponents();
        isFlying = false;
        currentVelocity = Vector2.zero;

        if (homeParent != null)
        {
            transform.SetParent(homeParent, false);
        }

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = homeAnchoredPosition;
        }

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }
    }

    public void StopMotion()
    {
        isFlying = false;
        currentVelocity = Vector2.zero;
    }

    public void HandleDropResult(bool accepted, bool returnWhenRejected)
    {
        dropHandled = accepted || returnWhenRejected;
        StopMotion();

        if (!accepted && returnWhenRejected)
        {
            ReturnToHome();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        CacheComponents();
        if (rectTransform == null)
        {
            return;
        }

        rootCanvas = GetComponentInParent<Canvas>();
        CaptureHome();

        isDragging = true;
        isFlying = false;
        dropHandled = false;
        currentVelocity = Vector2.zero;
        lastDragPosition = rectTransform.anchoredPosition;
        lastDragTime = Time.unscaledTime;

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
        }

        if (rootCanvas != null)
        {
            transform.SetParent(rootCanvas.transform, true);
            transform.SetAsLastSibling();
        }

        DragBegan?.Invoke(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rectTransform == null || rootCanvas == null)
        {
            return;
        }

        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        Camera eventCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, eventCamera, out Vector2 localPoint))
        {
            return;
        }

        float now = Time.unscaledTime;
        float deltaTime = Mathf.Max(now - lastDragTime, 0.0001f);
        currentVelocity = Vector2.ClampMagnitude((localPoint - lastDragPosition) / deltaTime * throwMultiplier, maxThrowSpeed);

        rectTransform.anchoredPosition = localPoint;
        lastDragPosition = localPoint;
        lastDragTime = now;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }

        if (!dropHandled && canBeThrown && currentVelocity.sqrMagnitude > 9000f)
        {
            isFlying = true;
        }
        else if (!dropHandled && returnToHomeOnEnd)
        {
            ReturnToHome();
        }

        DragEnded?.Invoke(this);
    }

    private void CacheComponents()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    private void EnableRaycastTarget()
    {
        Graphic graphic = GetComponent<Graphic>();
        if (graphic != null)
        {
            graphic.raycastTarget = true;
        }
    }
}
