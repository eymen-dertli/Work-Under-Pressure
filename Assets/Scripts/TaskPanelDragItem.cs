using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class TaskPanelDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Transform originalParent;
    private Vector2 originalAnchoredPosition;

    public string ValueType { get; private set; }
    public string DisplayText { get; private set; }
    public int NumericValue { get; private set; }

    public void Configure(string valueType, int numericValue, string displayText)
    {
        ValueType = valueType;
        NumericValue = numericValue;
        DisplayText = displayText;

        Image image = GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = true;
        }

        EnsureText(displayText);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        rectTransform = GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        originalParent = transform.parent;
        originalAnchoredPosition = rectTransform.anchoredPosition;
        canvasGroup.blocksRaycasts = false;

        if (rootCanvas != null)
        {
            transform.SetParent(rootCanvas.transform, true);
            transform.SetAsLastSibling();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rectTransform == null || rootCanvas == null)
        {
            return;
        }

        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        if (canvasRect == null)
        {
            return;
        }

        Camera eventCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, eventCamera, out Vector2 localPoint))
        {
            rectTransform.anchoredPosition = localPoint;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }

        if (originalParent != null && rectTransform != null)
        {
            transform.SetParent(originalParent, false);
            rectTransform.anchoredPosition = originalAnchoredPosition;
        }
    }

    private void EnsureText(string displayText)
    {
        TextMeshProUGUI label = GetComponentInChildren<TextMeshProUGUI>(true);
        if (label == null)
        {
            GameObject labelObject = new GameObject("ValueLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(transform, false);

            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            label = labelObject.GetComponent<TextMeshProUGUI>();
        }

        RectTransform currentLabelRect = label.transform as RectTransform;
        if (currentLabelRect != null)
        {
            currentLabelRect.localRotation = Quaternion.Inverse(transform.localRotation);
        }

        label.text = displayText;
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 22f;
        label.fontStyle = FontStyles.Bold;
        label.color = Color.black;
        label.raycastTarget = false;
    }
}
