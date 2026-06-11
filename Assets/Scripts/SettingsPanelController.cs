using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class SettingsPanelController : MonoBehaviour
{
    private const string ControlsRootName = "Audio Settings Controls";
    private const string CloseButtonName = "CloseButton";
    private static readonly Color TrackColor = new Color(0.27f, 0.18f, 0.1f, 0.86f);
    private static readonly Color FillColor = new Color(0.84f, 0.52f, 0.12f, 0.92f);
    private static readonly Color HandleColor = new Color(0.96f, 0.64f, 0.12f, 1f);
    private static readonly Color HandleEdgeColor = new Color(0.43f, 0.22f, 0.05f, 0.95f);
    private static readonly Color ToggleOnColor = new Color(0.9f, 0.55f, 0.12f, 1f);
    private static readonly Color ToggleOffColor = new Color(0.48f, 0.36f, 0.24f, 0.95f);
    private static readonly Color ToggleTextColor = new Color(1f, 0.92f, 0.72f, 1f);

    private Slider soundSlider;
    private Slider musicSlider;
    private Button soundToggleButton;
    private Button musicToggleButton;
    private TextMeshProUGUI soundToggleLabel;
    private TextMeshProUGUI musicToggleLabel;

    private void Awake()
    {
        BuildControls();
        BindExistingCloseButton();
    }

    private void OnEnable()
    {
        AudioManager.LoadSettings();
        RefreshControls();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void BuildControls()
    {
        EnsureEventSystem();

        Transform existingRoot = transform.Find(ControlsRootName);
        GameObject controlsRoot = existingRoot != null
            ? existingRoot.gameObject
            : new GameObject(ControlsRootName, typeof(RectTransform));

        controlsRoot.transform.SetParent(transform, false);
        controlsRoot.transform.SetAsLastSibling();

        RectTransform rootRect = controlsRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = new Vector2(700f, 700f);

        ClearChildren(controlsRoot.transform);

        CreateControlRow(controlsRoot.transform, "SOUNDS", 22f, true);
        CreateControlRow(controlsRoot.transform, "MUSIC", -78f, false);

        RefreshControls();
    }

    private void BindExistingCloseButton()
    {
        Transform closeTransform = transform.Find(CloseButtonName);
        if (closeTransform == null)
        {
            return;
        }

        Button closeButton = closeTransform.GetComponent<Button>();
        if (closeButton == null)
        {
            closeButton = closeTransform.gameObject.AddComponent<Button>();
        }

        Image closeImage = closeTransform.GetComponent<Image>();
        if (closeImage != null)
        {
            closeButton.targetGraphic = closeImage;
        }

        closeButton.onClick.RemoveListener(Close);
        closeButton.onClick.AddListener(Close);
        closeTransform.SetAsLastSibling();
    }

    private void CreateControlRow(Transform parent, string name, float y, bool isSoundRow)
    {
        Slider slider = CreateSlider($"{name} Slider", parent);
        RectTransform sliderRect = slider.GetComponent<RectTransform>();
        sliderRect.anchoredPosition = new Vector2(22f, y);
        sliderRect.sizeDelta = new Vector2(300f, 36f);

        Button toggleButton = CreateButton($"{name} Toggle", parent, "ON", new Vector2(82f, 38f), ToggleOnColor);
        RectTransform toggleRect = toggleButton.GetComponent<RectTransform>();
        toggleRect.anchoredPosition = new Vector2(248f, y);

        TextMeshProUGUI toggleLabel = toggleButton.GetComponentInChildren<TextMeshProUGUI>();
        toggleLabel.fontSize = 17f;
        toggleLabel.color = ToggleTextColor;

        if (isSoundRow)
        {
            soundSlider = slider;
            soundToggleButton = toggleButton;
            soundToggleLabel = toggleLabel;
            soundSlider.onValueChanged.AddListener(AudioManager.SetSoundVolume);
            soundToggleButton.onClick.AddListener(() =>
            {
                AudioManager.SetSoundEnabled(!AudioManager.SoundEnabled);
                RefreshControls();
            });
        }
        else
        {
            musicSlider = slider;
            musicToggleButton = toggleButton;
            musicToggleLabel = toggleLabel;
            musicSlider.onValueChanged.AddListener(AudioManager.SetMusicVolume);
            musicToggleButton.onClick.AddListener(() =>
            {
                AudioManager.SetMusicEnabled(!AudioManager.MusicEnabled);
                RefreshControls();
            });
        }
    }

    private void RefreshControls()
    {
        if (soundSlider != null)
        {
            soundSlider.SetValueWithoutNotify(AudioManager.SoundVolume);
        }

        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(AudioManager.MusicVolume);
        }

        UpdateToggle(soundToggleButton, soundToggleLabel, AudioManager.SoundEnabled);
        UpdateToggle(musicToggleButton, musicToggleLabel, AudioManager.MusicEnabled);
    }

    private static void UpdateToggle(Button button, TextMeshProUGUI label, bool isOn)
    {
        if (button == null || label == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = isOn ? ToggleOnColor : ToggleOffColor;
        }

        label.text = isOn ? "ON" : "OFF";
    }

    private static Slider CreateSlider(string name, Transform parent)
    {
        GameObject sliderObject = new GameObject(name, typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(parent, false);

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;

        GameObject background = CreateImage("Background", sliderObject.transform, TrackColor);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        Stretch(backgroundRect, new Vector2(0f, 15f), new Vector2(0f, -15f));

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        Stretch(fillAreaRect, new Vector2(0f, 15f), new Vector2(0f, -15f));

        GameObject fill = CreateImage("Fill", fillArea.transform, FillColor);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        Stretch(fillRect, Vector2.zero, Vector2.zero);

        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderObject.transform, false);
        RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
        Stretch(handleAreaRect, Vector2.zero, Vector2.zero);

        GameObject handle = CreateImage("Handle", handleArea.transform, HandleColor);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(32f, 32f);

        Outline handleOutline = handle.AddComponent<Outline>();
        handleOutline.effectColor = HandleEdgeColor;
        handleOutline.effectDistance = new Vector2(2f, -2f);

        Shadow handleShadow = handle.AddComponent<Shadow>();
        handleShadow.effectColor = new Color(0.15f, 0.08f, 0.02f, 0.35f);
        handleShadow.effectDistance = new Vector2(3f, -3f);

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;

        return slider;
    }

    private static Button CreateButton(string name, Transform parent, string text, Vector2 size, Color color)
    {
        GameObject buttonObject = CreateImage(name, parent, color);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = size;

        Button button = buttonObject.AddComponent<Button>();

        Outline outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.42f, 0.22f, 0.06f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);

        Shadow shadow = buttonObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.16f, 0.08f, 0.02f, 0.35f);
        shadow.effectDistance = new Vector2(3f, -3f);

        TextMeshProUGUI label = CreateLabel("Label", buttonObject.transform, text, 22f, ToggleTextColor);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        Stretch(labelRect, Vector2.zero, Vector2.zero);

        return button;
    }

    private static GameObject CreateImage(string name, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = true;

        return imageObject;
    }

    private static TextMeshProUGUI CreateLabel(string name, Transform parent, string text, float fontSize, Color color)
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

    private static void Stretch(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }

    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }
}
