using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CoffeeEffectManager : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private bool listenToClickableDeskObjects = true;
    [SerializeField] private ClickableDeskObject coffeeObject;

    [Header("Effect Values")]
    [SerializeField] private float boostAmount = 45f;
    [SerializeField] private float sleeplessnessAmount = 35f;
    [SerializeField] private float boostMax = 100f;
    [SerializeField] private float sleeplessnessMax = 100f;
    [SerializeField] private float boostDecayPerSecond = 12f;
    [SerializeField] private float sleeplessnessDecayPerSecond = 6f;
    [SerializeField] private float boostDuration = 5f;

    [Header("UI")]
    [SerializeField] private GameObject barsRoot;
    [SerializeField] private Image sleeplessnessFillImage;
    [SerializeField] private Image boostFillImage;
    [SerializeField] private TextMeshProUGUI sleeplessnessValueLabel;
    [SerializeField] private TextMeshProUGUI boostValueLabel;

    private float currentBoost;
    private float currentSleeplessness;
    private float boostTimer;

    public float CurrentBoost => currentBoost;
    public float CurrentSleeplessness => currentSleeplessness;

    private void OnEnable()
    {
        if (listenToClickableDeskObjects)
        {
            ClickableDeskObject.Clicked += HandleDeskObjectClicked;
        }
    }

    private void OnDisable()
    {
        ClickableDeskObject.Clicked -= HandleDeskObjectClicked;
    }

    private void Start()
    {
        EnsureUi();
        RefreshUi();
    }

    private void Update()
    {
        if (currentBoost <= 0f && currentSleeplessness <= 0f)
        {
            RefreshUi();
            return;
        }

        float deltaTime = Time.deltaTime;
        if (boostTimer > 0f)
        {
            boostTimer -= deltaTime;
        }

        currentBoost = Mathf.Max(0f, currentBoost - boostDecayPerSecond * deltaTime);
        currentSleeplessness = Mathf.Max(0f, currentSleeplessness - sleeplessnessDecayPerSecond * deltaTime);
        RefreshUi();
    }

    public void DrinkCoffee()
    {
        EnsureUi();
        currentBoost = Mathf.Clamp(currentBoost + boostAmount, 0f, boostMax);
        currentSleeplessness = Mathf.Clamp(currentSleeplessness + sleeplessnessAmount, 0f, sleeplessnessMax);
        boostTimer = Mathf.Max(boostTimer, boostDuration);
        RefreshUi();
    }

    private void HandleDeskObjectClicked(ClickableDeskObject clickedObject)
    {
        if (OfficeMiniGameUi.MatchesClickedObject(clickedObject, coffeeObject, "coffee", "kahve"))
        {
            DrinkCoffee();
        }
    }

    private void RefreshUi()
    {
        bool hasEffect = currentBoost > 0.01f || currentSleeplessness > 0.01f;
        if (barsRoot != null)
        {
            barsRoot.SetActive(hasEffect);
        }

        if (sleeplessnessFillImage != null)
        {
            sleeplessnessFillImage.fillAmount = sleeplessnessMax > 0f ? currentSleeplessness / sleeplessnessMax : 0f;
        }

        if (boostFillImage != null)
        {
            boostFillImage.fillAmount = boostMax > 0f ? currentBoost / boostMax : 0f;
        }

        if (sleeplessnessValueLabel != null)
        {
            sleeplessnessValueLabel.text = $"Sleeplessness {Mathf.RoundToInt(currentSleeplessness)}";
        }

        if (boostValueLabel != null)
        {
            boostValueLabel.text = $"Boost {Mathf.RoundToInt(currentBoost)}";
        }
    }

    private void EnsureUi()
    {
        if (barsRoot != null && sleeplessnessFillImage != null && boostFillImage != null)
        {
            return;
        }

        Canvas canvas = OfficeMiniGameUi.CreateOverlayCanvas("Coffee Effect Canvas", transform, 1004);

        barsRoot = new GameObject("Coffee Bars", typeof(RectTransform));
        barsRoot.transform.SetParent(canvas.transform, false);

        RectTransform rootRect = barsRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(0f, 1f);
        rootRect.sizeDelta = new Vector2(360f, 106f);
        rootRect.anchoredPosition = new Vector2(205f, -82f);

        CreateBar(rootRect, "Sleeplessness Bar", "Sleeplessness", 0f, new Color(0.83f, 0.36f, 0.27f, 1f), out sleeplessnessFillImage, out sleeplessnessValueLabel);
        CreateBar(rootRect, "Boost Bar", "Boost", -52f, new Color(0.25f, 0.62f, 0.4f, 1f), out boostFillImage, out boostValueLabel);
    }

    private static void CreateBar(Transform parent, string name, string labelText, float y, Color fillColor, out Image fill, out TextMeshProUGUI label)
    {
        GameObject frame = OfficeMiniGameUi.CreateImage(name, parent, new Color(0.08f, 0.08f, 0.08f, 0.78f));
        RectTransform frameRect = frame.GetComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0f, 1f);
        frameRect.anchorMax = new Vector2(0f, 1f);
        frameRect.sizeDelta = new Vector2(340f, 38f);
        frameRect.anchoredPosition = new Vector2(170f, y);

        GameObject fillObject = OfficeMiniGameUi.CreateImage("Fill", frame.transform, fillColor);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = new Vector2(4f, 4f);
        fillRect.offsetMax = new Vector2(-4f, -4f);

        fill = fillObject.GetComponent<Image>();
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;

        label = OfficeMiniGameUi.CreateLabel("Label", frame.transform, labelText, 18f, Color.white);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        OfficeMiniGameUi.Stretch(labelRect, Vector2.zero, Vector2.zero);
        label.alignment = TextAlignmentOptions.Center;
    }
}
