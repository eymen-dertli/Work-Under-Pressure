using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ContractSignaturePad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform drawingArea;
    [SerializeField] private float strokeWidth = 5f;

    private ContractSigningController controller;
    private Vector2 lastPoint;
    private bool isDrawing;
    private bool currentStrokeIsValid;
    private float currentStrokeDistance;

    public void Initialize(ContractSigningController owner, RectTransform area)
    {
        controller = owner;
        drawingArea = area;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!TryGetLocalPoint(eventData, out Vector2 point) || !IsPointInside(point))
        {
            return;
        }

        ClearSignature();
        isDrawing = true;
        currentStrokeIsValid = true;
        currentStrokeDistance = 0f;
        lastPoint = point;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDrawing)
        {
            return;
        }

        if (!TryGetLocalPoint(eventData, out Vector2 point) || !IsPointInside(point))
        {
            isDrawing = false;
            currentStrokeIsValid = false;
            ClearSignature();
            controller.ReportSignatureMistake();
            return;
        }

        AddStrokeSegment(lastPoint, point);
        currentStrokeDistance += Vector2.Distance(lastPoint, point);
        lastPoint = point;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDrawing)
        {
            return;
        }

        isDrawing = false;

        if (currentStrokeIsValid && currentStrokeDistance >= 25f)
        {
            controller.CompleteCurrentSignature();
        }
    }

    public void ClearSignature()
    {
        if (drawingArea == null)
        {
            return;
        }

        for (int i = drawingArea.childCount - 1; i >= 0; i--)
        {
            Destroy(drawingArea.GetChild(i).gameObject);
        }
    }

    private bool TryGetLocalPoint(PointerEventData eventData, out Vector2 point)
    {
        point = default;

        if (drawingArea == null)
        {
            return false;
        }

        Camera eventCamera = eventData.pressEventCamera;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(drawingArea, eventData.position, eventCamera, out point);
    }

    private bool IsPointInside(Vector2 point)
    {
        Rect rect = drawingArea.rect;
        return point.x >= rect.xMin && point.x <= rect.xMax && point.y >= rect.yMin && point.y <= rect.yMax;
    }

    private void AddStrokeSegment(Vector2 from, Vector2 to)
    {
        float length = Vector2.Distance(from, to);
        if (length < 0.1f)
        {
            return;
        }

        GameObject segment = new GameObject("SignatureStroke", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        segment.transform.SetParent(drawingArea, false);

        RectTransform rect = segment.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(length, strokeWidth);
        rect.anchoredPosition = (from + to) * 0.5f;
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg);

        Image image = segment.GetComponent<Image>();
        image.color = new Color(0.05f, 0.05f, 0.06f, 1f);
        image.raycastTarget = false;
    }
}
