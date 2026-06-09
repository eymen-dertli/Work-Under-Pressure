using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class OfficeMiniGameUi : MonoBehaviour
{
    public static Canvas CreateOverlayCanvas(string name, Transform parent, int sortingOrder)
    {
        EnsureEventSystem();

        GameObject canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(parent, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    public static GameObject CreateImage(string name, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = true;

        return imageObject;
    }

    public static TextMeshProUGUI CreateLabel(string name, Transform parent, string text, float fontSize, Color color)
    {
        GameObject labelObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        label.raycastTarget = false;

        return label;
    }

    public static Button CreateButton(string name, Transform parent, string text, Vector2 size, Color color, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = CreateImage(name, parent, color);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.sizeDelta = size;

        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(action);

        TextMeshProUGUI label = CreateLabel("Label", buttonObject.transform, text, 20f, Color.white);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        Stretch(labelRect, Vector2.zero, Vector2.zero);

        return button;
    }

    public static void Stretch(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
    }

    public static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Object.Destroy(parent.GetChild(i).gameObject);
        }
    }

    public static bool MatchesClickedObject(ClickableDeskObject clickedObject, ClickableDeskObject explicitObject, params string[] keywords)
    {
        if (clickedObject == null)
        {
            return false;
        }

        if (explicitObject != null && clickedObject == explicitObject)
        {
            return true;
        }

        string text = Normalize($"{clickedObject.ObjectId} {clickedObject.DisplayName} {clickedObject.gameObject.name}");
        foreach (string keyword in keywords)
        {
            if (!string.IsNullOrWhiteSpace(keyword) && text.Contains(Normalize(keyword)))
            {
                return true;
            }
        }

        return false;
    }

    private static string Normalize(string text)
    {
        return text.ToLowerInvariant()
            .Replace("ç", "c")
            .Replace("ğ", "g")
            .Replace("ı", "i")
            .Replace("ö", "o")
            .Replace("ş", "s")
            .Replace("ü", "u")
            .Replace("Ã§", "c")
            .Replace("ÄŸ", "g")
            .Replace("Ä±", "i")
            .Replace("Ã¶", "o")
            .Replace("ÅŸ", "s")
            .Replace("Ã¼", "u");
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }
}
