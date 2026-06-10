using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class TaskPanelDropSlot : MonoBehaviour, IDropHandler
{
    [SerializeField] private string acceptedValueType;

    public string AcceptedValueType => acceptedValueType;
    public string DisplayText { get; private set; }
    public int NumericValue { get; private set; }
    public bool HasValue { get; private set; }

    public void Configure(string valueType)
    {
        acceptedValueType = valueType;

        Image image = GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = true;
        }

        EnsureText(string.Empty);
    }

    public void OnDrop(PointerEventData eventData)
    {
        TaskPanelDragItem draggedItem = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<TaskPanelDragItem>()
            : null;

        if (draggedItem == null || draggedItem.ValueType != acceptedValueType)
        {
            return;
        }

        SetValue(draggedItem.NumericValue, draggedItem.DisplayText);
    }

    private void SetValue(int numericValue, string displayText)
    {
        NumericValue = numericValue;
        DisplayText = displayText;
        HasValue = true;
        EnsureText(displayText);
    }

    private void EnsureText(string displayText)
    {
        TextMeshProUGUI label = GetComponentInChildren<TextMeshProUGUI>(true);
        if (label == null)
        {
            GameObject labelObject = new GameObject("DroppedValueLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
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
