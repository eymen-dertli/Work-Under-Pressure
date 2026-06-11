using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ContractSignaturePad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private static readonly Vector2[][] SignatureTemplates =
    {
        new[] { new Vector2(-0.44f, -0.05f), new Vector2(-0.34f, 0.2f), new Vector2(-0.25f, -0.16f), new Vector2(-0.12f, 0.16f), new Vector2(0.02f, -0.12f), new Vector2(0.18f, 0.08f), new Vector2(0.36f, -0.08f), new Vector2(0.46f, 0.04f) },
        new[] { new Vector2(-0.46f, -0.12f), new Vector2(-0.32f, 0.16f), new Vector2(-0.18f, -0.02f), new Vector2(-0.02f, 0.22f), new Vector2(0.08f, -0.18f), new Vector2(0.26f, 0.12f), new Vector2(0.44f, -0.06f) },
        new[] { new Vector2(-0.42f, 0.12f), new Vector2(-0.28f, -0.12f), new Vector2(-0.15f, 0.18f), new Vector2(-0.02f, -0.08f), new Vector2(0.11f, 0.1f), new Vector2(0.24f, -0.14f), new Vector2(0.42f, 0.02f) },
        new[] { new Vector2(-0.46f, 0.02f), new Vector2(-0.34f, -0.16f), new Vector2(-0.24f, 0.2f), new Vector2(-0.08f, -0.18f), new Vector2(0.08f, 0.16f), new Vector2(0.22f, -0.04f), new Vector2(0.44f, 0.1f) },
        new[] { new Vector2(-0.44f, -0.1f), new Vector2(-0.3f, 0.12f), new Vector2(-0.16f, -0.14f), new Vector2(-0.04f, 0.2f), new Vector2(0.1f, -0.02f), new Vector2(0.2f, 0.14f), new Vector2(0.34f, -0.12f), new Vector2(0.46f, 0.04f) },
        new[] { new Vector2(-0.43f, 0.05f), new Vector2(-0.3f, 0.22f), new Vector2(-0.22f, -0.16f), new Vector2(-0.05f, 0.1f), new Vector2(0.06f, -0.2f), new Vector2(0.2f, 0.18f), new Vector2(0.34f, -0.08f), new Vector2(0.45f, 0.12f) },
        new[] { new Vector2(-0.45f, -0.02f), new Vector2(-0.34f, 0.18f), new Vector2(-0.19f, 0.02f), new Vector2(-0.08f, -0.18f), new Vector2(0.04f, 0.17f), new Vector2(0.19f, -0.12f), new Vector2(0.31f, 0.07f), new Vector2(0.45f, -0.03f) },
        new[] { new Vector2(-0.44f, 0.14f), new Vector2(-0.36f, -0.14f), new Vector2(-0.2f, 0.08f), new Vector2(-0.07f, -0.18f), new Vector2(0.08f, 0.2f), new Vector2(0.23f, -0.1f), new Vector2(0.43f, 0.08f) },
        new[] { new Vector2(-0.46f, -0.06f), new Vector2(-0.34f, 0.09f), new Vector2(-0.25f, -0.18f), new Vector2(-0.1f, 0.22f), new Vector2(0.04f, -0.1f), new Vector2(0.16f, 0.16f), new Vector2(0.3f, -0.16f), new Vector2(0.46f, 0.02f) },
        new[] { new Vector2(-0.43f, 0f), new Vector2(-0.28f, -0.18f), new Vector2(-0.14f, 0.16f), new Vector2(0.02f, -0.04f), new Vector2(0.14f, 0.2f), new Vector2(0.28f, -0.12f), new Vector2(0.44f, 0.11f) },
        new[] { new Vector2(-0.45f, -0.14f), new Vector2(-0.31f, 0.2f), new Vector2(-0.18f, -0.05f), new Vector2(-0.02f, 0.12f), new Vector2(0.1f, -0.2f), new Vector2(0.27f, 0.08f), new Vector2(0.45f, -0.02f) },
        new[] { new Vector2(-0.44f, 0.08f), new Vector2(-0.32f, -0.1f), new Vector2(-0.18f, 0.22f), new Vector2(-0.04f, -0.16f), new Vector2(0.12f, 0.1f), new Vector2(0.24f, -0.08f), new Vector2(0.34f, 0.14f), new Vector2(0.46f, -0.04f) }
    };

    [SerializeField] private RectTransform drawingArea;
    [SerializeField] private float strokeWidth = 5f;
    [SerializeField] private float guideStrokeWidth = 4f;

    private ContractSigningController controller;
    private RectTransform guideLayer;
    private RectTransform strokeLayer;
    private Vector2 lastPoint;
    private bool isDrawing;
    private bool currentStrokeIsValid;
    private float currentStrokeDistance;

    public void Initialize(ContractSigningController owner, RectTransform area)
    {
        controller = owner;
        drawingArea = area;
        EnsureLayers();
    }

    public void ShowGuideForPage(int pageNumber)
    {
        EnsureLayers();

        if (guideLayer == null || drawingArea == null)
        {
            return;
        }

        ClearLayer(guideLayer);

        Vector2[] template = SignatureTemplates[Mathf.Abs(pageNumber - 1) % SignatureTemplates.Length];
        Rect rect = drawingArea.rect;

        for (int i = 1; i < template.Length; i++)
        {
            Vector2 from = ScaleTemplatePoint(template[i - 1], rect);
            Vector2 to = ScaleTemplatePoint(template[i], rect);
            AddStrokeSegment(guideLayer, from, to, guideStrokeWidth, new Color(0.05f, 0.05f, 0.06f, 0.24f), "SignatureGuide");
        }
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

        AddStrokeSegment(strokeLayer, lastPoint, point, strokeWidth, new Color(0.05f, 0.05f, 0.06f, 1f), "SignatureStroke");
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
        EnsureLayers();

        if (strokeLayer == null)
        {
            return;
        }

        ClearLayer(strokeLayer);
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

    private void EnsureLayers()
    {
        if (drawingArea == null)
        {
            return;
        }

        if (guideLayer == null)
        {
            guideLayer = CreateLayer("Signature Guide Layer");
        }

        if (strokeLayer == null)
        {
            strokeLayer = CreateLayer("Signature Stroke Layer");
        }

        guideLayer.SetAsFirstSibling();
        strokeLayer.SetAsLastSibling();
    }

    private RectTransform CreateLayer(string layerName)
    {
        GameObject layerObject = new GameObject(layerName, typeof(RectTransform));
        layerObject.transform.SetParent(drawingArea, false);

        RectTransform rect = layerObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static void ClearLayer(RectTransform layer)
    {
        for (int i = layer.childCount - 1; i >= 0; i--)
        {
            Destroy(layer.GetChild(i).gameObject);
        }
    }

    private static Vector2 ScaleTemplatePoint(Vector2 normalizedPoint, Rect rect)
    {
        return new Vector2(normalizedPoint.x * rect.width, normalizedPoint.y * rect.height);
    }

    private void AddStrokeSegment(RectTransform parent, Vector2 from, Vector2 to, float width, Color color, string segmentName)
    {
        float length = Vector2.Distance(from, to);
        if (parent == null || length < 0.1f)
        {
            return;
        }

        GameObject segment = new GameObject(segmentName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        segment.transform.SetParent(parent, false);

        RectTransform rect = segment.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(length, width);
        rect.anchoredPosition = (from + to) * 0.5f;
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg);

        Image image = segment.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }
}
