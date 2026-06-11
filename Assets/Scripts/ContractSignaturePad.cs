using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ContractSignaturePad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private static readonly Vector2[][] SignatureTemplates =
    {
        new[] { new Vector2(-0.47f, -0.03f), new Vector2(-0.43f, 0.18f), new Vector2(-0.34f, 0.23f), new Vector2(-0.29f, 0.05f), new Vector2(-0.34f, -0.15f), new Vector2(-0.45f, -0.1f), new Vector2(-0.31f, 0.02f), new Vector2(-0.18f, 0.17f), new Vector2(-0.1f, -0.12f), new Vector2(0.02f, 0.12f), new Vector2(0.1f, -0.09f), new Vector2(0.22f, 0.09f), new Vector2(0.33f, -0.04f), new Vector2(0.47f, 0.04f) },
        new[] { new Vector2(-0.46f, -0.11f), new Vector2(-0.37f, 0.22f), new Vector2(-0.27f, 0.11f), new Vector2(-0.32f, -0.11f), new Vector2(-0.2f, -0.01f), new Vector2(-0.08f, 0.2f), new Vector2(-0.01f, -0.18f), new Vector2(0.09f, 0.09f), new Vector2(0.18f, -0.08f), new Vector2(0.3f, 0.11f), new Vector2(0.43f, -0.05f), new Vector2(0.48f, 0f) },
        new[] { new Vector2(-0.44f, 0.11f), new Vector2(-0.36f, -0.15f), new Vector2(-0.25f, -0.08f), new Vector2(-0.23f, 0.16f), new Vector2(-0.33f, 0.2f), new Vector2(-0.21f, 0.03f), new Vector2(-0.08f, -0.09f), new Vector2(0.02f, 0.16f), new Vector2(0.13f, -0.11f), new Vector2(0.25f, 0.08f), new Vector2(0.38f, -0.04f), new Vector2(0.47f, 0.02f) },
        new[] { new Vector2(-0.47f, 0.01f), new Vector2(-0.38f, -0.17f), new Vector2(-0.28f, 0.2f), new Vector2(-0.19f, -0.16f), new Vector2(-0.1f, 0.1f), new Vector2(-0.02f, -0.08f), new Vector2(0.09f, 0.16f), new Vector2(0.18f, -0.04f), new Vector2(0.29f, 0.08f), new Vector2(0.39f, -0.06f), new Vector2(0.47f, 0.08f) },
        new[] { new Vector2(-0.45f, -0.1f), new Vector2(-0.37f, 0.14f), new Vector2(-0.28f, 0.2f), new Vector2(-0.22f, -0.12f), new Vector2(-0.33f, -0.16f), new Vector2(-0.17f, -0.02f), new Vector2(-0.05f, 0.19f), new Vector2(0.07f, -0.03f), new Vector2(0.17f, 0.14f), new Vector2(0.29f, -0.13f), new Vector2(0.41f, 0.03f), new Vector2(0.48f, -0.02f) },
        new[] { new Vector2(-0.44f, 0.04f), new Vector2(-0.35f, 0.23f), new Vector2(-0.27f, -0.15f), new Vector2(-0.17f, 0.06f), new Vector2(-0.06f, -0.19f), new Vector2(0.04f, 0.1f), new Vector2(0.12f, -0.07f), new Vector2(0.2f, 0.18f), new Vector2(0.31f, -0.09f), new Vector2(0.42f, 0.12f), new Vector2(0.48f, 0.06f) },
        new[] { new Vector2(-0.46f, -0.02f), new Vector2(-0.39f, 0.19f), new Vector2(-0.28f, 0.03f), new Vector2(-0.21f, -0.18f), new Vector2(-0.12f, 0.12f), new Vector2(-0.02f, -0.03f), new Vector2(0.06f, 0.17f), new Vector2(0.17f, -0.11f), new Vector2(0.28f, 0.07f), new Vector2(0.39f, -0.04f), new Vector2(0.47f, -0.02f) },
        new[] { new Vector2(-0.45f, 0.13f), new Vector2(-0.39f, -0.14f), new Vector2(-0.29f, -0.08f), new Vector2(-0.25f, 0.18f), new Vector2(-0.15f, 0.04f), new Vector2(-0.06f, -0.18f), new Vector2(0.05f, 0.19f), new Vector2(0.16f, -0.1f), new Vector2(0.28f, 0.05f), new Vector2(0.4f, 0.08f), new Vector2(0.48f, 0.02f) },
        new[] { new Vector2(-0.47f, -0.06f), new Vector2(-0.38f, 0.1f), new Vector2(-0.3f, -0.18f), new Vector2(-0.19f, 0.21f), new Vector2(-0.08f, -0.1f), new Vector2(0.03f, 0.12f), new Vector2(0.13f, -0.03f), new Vector2(0.21f, 0.16f), new Vector2(0.31f, -0.15f), new Vector2(0.42f, 0.01f), new Vector2(0.48f, 0.02f) },
        new[] { new Vector2(-0.44f, 0f), new Vector2(-0.34f, -0.18f), new Vector2(-0.24f, 0.17f), new Vector2(-0.14f, -0.03f), new Vector2(-0.03f, 0.15f), new Vector2(0.07f, -0.04f), new Vector2(0.16f, 0.2f), new Vector2(0.27f, -0.12f), new Vector2(0.39f, 0.1f), new Vector2(0.48f, 0.05f) },
        new[] { new Vector2(-0.46f, -0.13f), new Vector2(-0.37f, 0.2f), new Vector2(-0.28f, 0.08f), new Vector2(-0.31f, -0.14f), new Vector2(-0.18f, -0.05f), new Vector2(-0.06f, 0.11f), new Vector2(0.04f, -0.19f), new Vector2(0.16f, 0.08f), new Vector2(0.29f, -0.03f), new Vector2(0.41f, -0.01f), new Vector2(0.48f, -0.02f) },
        new[] { new Vector2(-0.45f, 0.08f), new Vector2(-0.36f, -0.1f), new Vector2(-0.27f, 0.21f), new Vector2(-0.17f, -0.15f), new Vector2(-0.06f, 0.08f), new Vector2(0.04f, -0.07f), new Vector2(0.14f, 0.1f), new Vector2(0.25f, -0.08f), new Vector2(0.35f, 0.14f), new Vector2(0.47f, -0.04f) }
    };

    [SerializeField] private RectTransform drawingArea;
    [SerializeField] private float strokeWidth = 5f;
    [SerializeField] private float guideStrokeWidth = 4f;
    [SerializeField] private float guideMatchTolerance = 22f;
    [SerializeField] private float requiredGuideCoverage = 0.58f;
    [SerializeField] private float requiredStrokeAccuracy = 0.52f;
    [SerializeField] private float minSignatureLengthRatio = 0.55f;
    [SerializeField] private float maxSignatureLengthRatio = 2.25f;

    private ContractSigningController controller;
    private RectTransform guideLayer;
    private RectTransform strokeLayer;
    private readonly List<Vector2> drawnPoints = new List<Vector2>();
    private Vector2[] currentTemplatePoints;
    private Vector2 lastPoint;
    private bool isDrawing;
    private bool currentStrokeIsValid;
    private float currentStrokeDistance;
    private float currentGuideLength;

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
        currentTemplatePoints = new Vector2[template.Length];

        for (int i = 0; i < template.Length; i++)
        {
            currentTemplatePoints[i] = ScaleTemplatePoint(template[i], rect);
        }

        currentGuideLength = CalculatePolylineLength(currentTemplatePoints);

        for (int i = 1; i < currentTemplatePoints.Length; i++)
        {
            AddStrokeSegment(guideLayer, currentTemplatePoints[i - 1], currentTemplatePoints[i], guideStrokeWidth, new Color(0.05f, 0.05f, 0.06f, 0.24f), "SignatureGuide");
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
        drawnPoints.Clear();
        drawnPoints.Add(point);
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
        drawnPoints.Add(point);
        lastPoint = point;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDrawing)
        {
            return;
        }

        isDrawing = false;

        if (currentStrokeIsValid && IsSignatureCloseEnough())
        {
            controller.CompleteCurrentSignature();
            return;
        }

        ClearSignature();
        controller.ReportSignatureMistake("Rehber imzaya daha yakın çiz.");
    }

    public void ClearSignature()
    {
        EnsureLayers();

        if (strokeLayer == null)
        {
            return;
        }

        ClearLayer(strokeLayer);
        drawnPoints.Clear();
    }

    private bool IsSignatureCloseEnough()
    {
        if (currentTemplatePoints == null || currentTemplatePoints.Length < 2 || drawnPoints.Count < 2)
        {
            return false;
        }

        float minimumLength = Mathf.Max(25f, currentGuideLength * minSignatureLengthRatio);
        if (currentStrokeDistance < minimumLength || currentStrokeDistance > currentGuideLength * maxSignatureLengthRatio)
        {
            return false;
        }

        List<Vector2> guideSamples = SamplePolyline(currentTemplatePoints, Mathf.Max(18, Mathf.CeilToInt(currentGuideLength / 8f)));
        List<Vector2> strokeSamples = SamplePolyline(drawnPoints, Mathf.Max(18, Mathf.CeilToInt(currentStrokeDistance / 8f)));

        float guideCoverage = GetNearPointRatio(guideSamples, strokeSamples, guideMatchTolerance);
        float strokeAccuracy = GetNearPointRatio(strokeSamples, guideSamples, guideMatchTolerance * 1.15f);

        return guideCoverage >= requiredGuideCoverage && strokeAccuracy >= requiredStrokeAccuracy;
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

    private static float CalculatePolylineLength(IReadOnlyList<Vector2> points)
    {
        float length = 0f;
        for (int i = 1; i < points.Count; i++)
        {
            length += Vector2.Distance(points[i - 1], points[i]);
        }

        return length;
    }

    private static List<Vector2> SamplePolyline(IReadOnlyList<Vector2> points, int sampleCount)
    {
        List<Vector2> samples = new List<Vector2>(sampleCount);
        float totalLength = CalculatePolylineLength(points);

        if (points.Count == 0 || totalLength <= 0.001f)
        {
            return samples;
        }

        int segmentIndex = 1;
        float distanceBeforeSegment = 0f;

        for (int sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
        {
            float targetDistance = sampleCount == 1 ? 0f : totalLength * sampleIndex / (sampleCount - 1);

            while (segmentIndex < points.Count - 1)
            {
                float segmentLength = Vector2.Distance(points[segmentIndex - 1], points[segmentIndex]);
                if (distanceBeforeSegment + segmentLength >= targetDistance)
                {
                    break;
                }

                distanceBeforeSegment += segmentLength;
                segmentIndex++;
            }

            Vector2 from = points[segmentIndex - 1];
            Vector2 to = points[segmentIndex];
            float currentSegmentLength = Vector2.Distance(from, to);
            float t = currentSegmentLength <= 0.001f ? 0f : (targetDistance - distanceBeforeSegment) / currentSegmentLength;
            samples.Add(Vector2.Lerp(from, to, Mathf.Clamp01(t)));
        }

        return samples;
    }

    private static float GetNearPointRatio(IReadOnlyList<Vector2> sourcePoints, IReadOnlyList<Vector2> targetPoints, float tolerance)
    {
        if (sourcePoints.Count == 0 || targetPoints.Count == 0)
        {
            return 0f;
        }

        float toleranceSqr = tolerance * tolerance;
        int nearCount = 0;

        for (int i = 0; i < sourcePoints.Count; i++)
        {
            if (HasNearbyPoint(sourcePoints[i], targetPoints, toleranceSqr))
            {
                nearCount++;
            }
        }

        return nearCount / (float)sourcePoints.Count;
    }

    private static bool HasNearbyPoint(Vector2 point, IReadOnlyList<Vector2> candidates, float toleranceSqr)
    {
        for (int i = 0; i < candidates.Count; i++)
        {
            if ((point - candidates[i]).sqrMagnitude <= toleranceSqr)
            {
                return true;
            }
        }

        return false;
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
