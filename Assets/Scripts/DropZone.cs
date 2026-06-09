using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class DropZone : MonoBehaviour, IDropHandler
{
    private static readonly List<DropZone> RegisteredZones = new List<DropZone>();

    [SerializeField] private string zoneId;
    [SerializeField] private string acceptedItemId;
    [SerializeField] private bool allowAnyItem;
    [SerializeField] private bool canReceiveThrownItems = true;
    [SerializeField] private bool returnRejectedItems = true;
    [SerializeField] private UnityEvent<DraggableItem> onAccepted = new UnityEvent<DraggableItem>();
    [SerializeField] private UnityEvent<DraggableItem> onRejected = new UnityEvent<DraggableItem>();

    private RectTransform rectTransform;

    public static IReadOnlyList<DropZone> ActiveZones => RegisteredZones;

    public event Action<DraggableItem, DropZone, bool> ItemEvaluated;

    public string ZoneId => zoneId;
    public string AcceptedItemId => acceptedItemId;
    public bool CanReceiveThrownItems => canReceiveThrownItems;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        EnableRaycastTarget();
    }

    private void OnEnable()
    {
        if (!RegisteredZones.Contains(this))
        {
            RegisteredZones.Add(this);
        }

        EnableRaycastTarget();
    }

    private void OnDisable()
    {
        RegisteredZones.Remove(this);
    }

    public void Configure(string newZoneId, string newAcceptedItemId, bool acceptsAnyItem = false)
    {
        zoneId = newZoneId;
        acceptedItemId = newAcceptedItemId;
        allowAnyItem = acceptsAnyItem;
        EnableRaycastTarget();
    }

    public void OnDrop(PointerEventData eventData)
    {
        DraggableItem draggedItem = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<DraggableItem>()
            : null;

        TryReceive(draggedItem, false);
    }

    public bool TryReceive(DraggableItem item, bool fromThrow)
    {
        if (item == null)
        {
            return false;
        }

        bool accepted = allowAnyItem || string.Equals(item.ItemId, acceptedItemId, StringComparison.Ordinal);
        item.HandleDropResult(accepted, returnRejectedItems);

        if (accepted)
        {
            onAccepted.Invoke(item);
        }
        else
        {
            onRejected.Invoke(item);
        }

        ItemEvaluated?.Invoke(item, this, accepted);
        return accepted;
    }

    public bool Overlaps(RectTransform itemRect)
    {
        if (itemRect == null)
        {
            return false;
        }

        rectTransform = rectTransform != null ? rectTransform : GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            return false;
        }

        Rect zoneWorldRect = GetWorldRect(rectTransform);
        Rect itemWorldRect = GetWorldRect(itemRect);
        return zoneWorldRect.Overlaps(itemWorldRect);
    }

    private static Rect GetWorldRect(RectTransform target)
    {
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);

        float minX = corners[0].x;
        float maxX = corners[0].x;
        float minY = corners[0].y;
        float maxY = corners[0].y;

        for (int i = 1; i < corners.Length; i++)
        {
            minX = Mathf.Min(minX, corners[i].x);
            maxX = Mathf.Max(maxX, corners[i].x);
            minY = Mathf.Min(minY, corners[i].y);
            maxY = Mathf.Max(maxY, corners[i].y);
        }

        return Rect.MinMaxRect(minX, minY, maxX, maxY);
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
