using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class ContractSigningController : MonoBehaviour
{
    private const string ControllerName = "_ContractSigningController";
    private const int TotalPages = 12;

    private Canvas canvas;
    private GameObject panelRoot;
    private TextMeshProUGUI pageLabel;
    private TextMeshProUGUI statusLabel;
    private ContractSignaturePad signaturePad;
    private int currentPage;
    private int mistakeCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void RegisterSceneLoadedHook()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        ClickableDeskObject.Clicked -= HandleDeskObjectClicked;
        ClickableDeskObject.Clicked += HandleDeskObjectClicked;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallForInitialScene()
    {
        EnsureController();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureController();
    }

    private static void HandleDeskObjectClicked(ClickableDeskObject clickedObject)
    {
        if (clickedObject == null || !IsContractObject(clickedObject))
        {
            return;
        }

        EnsureController().Open();
    }

    private static ContractSigningController EnsureController()
    {
        ContractSigningController controller = FindAnyObjectByType<ContractSigningController>();
        if (controller != null)
        {
            return controller;
        }

        GameObject controllerObject = new GameObject(ControllerName);
        controller = controllerObject.AddComponent<ContractSigningController>();
        controller.BuildUi();
        return controller;
    }

    private static bool IsContractObject(ClickableDeskObject clickedObject)
    {
        string text = $"{clickedObject.ObjectId} {clickedObject.DisplayName} {clickedObject.gameObject.name}";
        text = NormalizeTurkish(text);
        return text.Contains("sozlesme");
    }

    private static string NormalizeTurkish(string text)
    {
        return text.ToLowerInvariant()
            .Replace("ö", "o")
            .Replace("ü", "u")
            .Replace("ğ", "g")
            .Replace("ş", "s")
            .Replace("ı", "i")
            .Replace("ç", "c");
    }

    private void Awake()
    {
        if (panelRoot == null)
        {
            BuildUi();
        }
    }

    private void Open()
    {
        currentPage = 1;
        mistakeCount = 0;
        panelRoot.SetActive(true);
        signaturePad.ShowGuideForPage(currentPage);
        signaturePad.ClearSignature();
        UpdateLabels("Sag alttaki kutunun icine imza at.");
    }

    public void CompleteCurrentSignature()
    {
        signaturePad.ClearSignature();

        if (currentPage >= TotalPages)
        {
            UpdateLabels("Tum sozlesmeler imzalandi.");
            panelRoot.SetActive(false);
            return;
        }

        currentPage++;
        signaturePad.ShowGuideForPage(currentPage);
        UpdateLabels("Imza kabul edildi. Sonraki sayfa.");
    }

    public void ReportSignatureMistake()
    {
        mistakeCount++;
        UpdateLabels("Imza kutunun disina tasti. Tekrar dene.");
    }

    private void BuildUi()
    {
        EnsureEventSystem();

        GameObject canvasObject = new GameObject("Contract Signing Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        panelRoot = new GameObject("Contract Signing Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelRoot.transform.SetParent(canvasObject.transform, false);

        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = panelRoot.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.55f);

        CreatePaperStack(panelRoot.transform);
        panelRoot.SetActive(false);
    }

    private void CreatePaperStack(Transform parent)
    {
        for (int i = 4; i >= 1; i--)
        {
            GameObject shadowPaper = CreateUiImage($"Paper Back {i}", parent, new Color(0.82f, 0.8f, 0.74f, 1f));
            RectTransform shadowRect = shadowPaper.GetComponent<RectTransform>();
            shadowRect.sizeDelta = new Vector2(640f, 820f);
            shadowRect.anchoredPosition = new Vector2(i * 12f, -i * 10f);
        }

        GameObject paper = CreateUiImage("Signed Paper", parent, new Color(0.98f, 0.96f, 0.9f, 1f));
        RectTransform paperRect = paper.GetComponent<RectTransform>();
        paperRect.sizeDelta = new Vector2(640f, 820f);
        paperRect.anchoredPosition = Vector2.zero;

        pageLabel = CreateLabel("PageLabel", paper.transform, "Sozlesme 1/12", 30f, new Color(0.12f, 0.12f, 0.12f, 1f));
        RectTransform pageRect = pageLabel.GetComponent<RectTransform>();
        pageRect.anchorMin = new Vector2(0f, 1f);
        pageRect.anchorMax = new Vector2(1f, 1f);
        pageRect.offsetMin = new Vector2(44f, -100f);
        pageRect.offsetMax = new Vector2(-44f, -42f);

        statusLabel = CreateLabel("StatusLabel", paper.transform, string.Empty, 22f, new Color(0.18f, 0.18f, 0.18f, 1f));
        RectTransform statusRect = statusLabel.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0f, 0f);
        statusRect.anchorMax = new Vector2(1f, 0f);
        statusRect.offsetMin = new Vector2(44f, 38f);
        statusRect.offsetMax = new Vector2(-44f, 92f);

        CreateDocumentLines(paper.transform);
        CreateSignatureArea(paper.transform);
        CreateCloseButton(parent);
    }

    private void CreateDocumentLines(Transform paper)
    {
        for (int i = 0; i < 13; i++)
        {
            GameObject line = CreateUiImage($"Document Line {i + 1}", paper, new Color(0.68f, 0.66f, 0.6f, 0.55f));
            RectTransform lineRect = line.GetComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0f, 1f);
            lineRect.anchorMax = new Vector2(1f, 1f);
            lineRect.offsetMin = new Vector2(58f, -155f - i * 34f);
            lineRect.offsetMax = new Vector2(-58f - (i % 4) * 38f, -151f - i * 34f);
        }
    }

    private void CreateSignatureArea(Transform paper)
    {
        GameObject box = CreateUiImage("Signature Box", paper, new Color(1f, 1f, 1f, 0.92f));
        RectTransform boxRect = box.GetComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(1f, 0f);
        boxRect.anchorMax = new Vector2(1f, 0f);
        boxRect.sizeDelta = new Vector2(240f, 120f);
        boxRect.anchoredPosition = new Vector2(-170f, 155f);

        Outline outline = box.AddComponent<Outline>();
        outline.effectColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        outline.effectDistance = new Vector2(2f, -2f);

        TextMeshProUGUI label = CreateLabel("SignatureBoxLabel", box.transform, "IMZA", 16f, new Color(0.2f, 0.2f, 0.2f, 0.75f));
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = new Vector2(8f, -30f);
        labelRect.offsetMax = new Vector2(-8f, -6f);

        GameObject drawingArea = new GameObject("Signature Drawing Area", typeof(RectTransform));
        drawingArea.transform.SetParent(box.transform, false);
        RectTransform drawingRect = drawingArea.GetComponent<RectTransform>();
        drawingRect.anchorMin = Vector2.zero;
        drawingRect.anchorMax = Vector2.one;
        drawingRect.offsetMin = new Vector2(8f, 8f);
        drawingRect.offsetMax = new Vector2(-8f, -28f);

        signaturePad = box.AddComponent<ContractSignaturePad>();
        signaturePad.Initialize(this, drawingRect);
    }

    private void CreateCloseButton(Transform parent)
    {
        GameObject buttonObject = CreateUiImage("Close Contract Signing", parent, new Color(0.08f, 0.08f, 0.09f, 0.95f));
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(54f, 54f);
        buttonRect.anchoredPosition = new Vector2(365f, 435f);

        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(() => panelRoot.SetActive(false));

        TextMeshProUGUI label = CreateLabel("CloseLabel", buttonObject.transform, "X", 26f, Color.white);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
    }

    private static GameObject CreateUiImage(string name, Transform parent, Color color)
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

    private void UpdateLabels(string statusText)
    {
        pageLabel.text = $"Sozlesme {currentPage}/{TotalPages}";
        statusLabel.text = mistakeCount > 0 ? $"{statusText} Hata: {mistakeCount}" : statusText;
    }

    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }
}
