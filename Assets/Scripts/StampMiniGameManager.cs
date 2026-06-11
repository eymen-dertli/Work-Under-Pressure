using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class StampMiniGameManager : MonoBehaviour
{
    private const string ControllerName = "_StampMiniGameManager";
    private const OfficeTaskKind TaskKind = OfficeTaskKind.Stamp;

    private static readonly StampDefinition[] StampDefinitions =
    {
        new StampDefinition("aksoy", "AKSOY\nLOJİSTİK", new Color(0.11f, 0.36f, 0.62f, 1f), StampShape.Rectangle),
        new StampDefinition("mira", "MİRA\nTEKNOLOJİ", new Color(0.52f, 0.18f, 0.52f, 1f), StampShape.Rectangle),
        new StampDefinition("duru", "DURU\nGIDA", new Color(0.14f, 0.46f, 0.25f, 1f), StampShape.Rectangle),
        new StampDefinition("yilmaz", "YILMAZ\nHUKUK", new Color(0.72f, 0.16f, 0.12f, 1f), StampShape.Rectangle),
    };

    private static readonly DocumentCase[] DocumentCases =
    {
        new DocumentCase(
            "Teslimat Raporu",
            "Sabah gelen sevkiyat listesinde Aksoy Lojistik tarafından taşınan paketler kontrol edildi.\n\nDepo sorumlusu teslim saatini rapora ekledi. Evrak ilgili firma kaşesiyle kapatılacak.",
            "aksoy"),
        new DocumentCase(
            "Servis Formu",
            "Mira Teknoloji için hazırlanan teknik servis kaydında iki dizüstü bilgisayar ve bir monitör yer alıyor.\n\nCihazlar teslim alınmadan önce firma kaşesi belgeye basılmalı.",
            "mira"),
        new DocumentCase(
            "Tedarik Notu",
            "Mutfak stok listesi Duru Gıda siparişi için güncellendi.\n\nPeynir ve içecek kalemleri fatura ile eşleşiyor. Rapor ilgili tedarikçi kaşesiyle onaylanacak.",
            "duru"),
        new DocumentCase(
            "Vekalet Yazisi",
            "Yılmaz Hukuk tarafından gönderilen sözleşme eki arşiv kaydına alındı.\n\nDosya numarası kontrol edildi. Belge, hukuk bürosunun kaşesiyle kapatılacak.",
            "yilmaz"),
        new DocumentCase(
            "Kargo Iade Formu",
            "İade edilen koliler Aksoy Lojistik teslim tutanağında eksiksiz görünüyor.\n\nŞube kodu raporun altına yazıldı. Doğru firma kaşesini kullan.",
            "aksoy"),
        new DocumentCase(
            "Bakim Raporu",
            "Ofis ağı için kurulan yeni yazılım Mira Teknoloji ekibi tarafından test edildi.\n\nRaporun sistem kaydına alınması için ilgili şirket kaşesi gerekiyor.",
            "mira"),
        new DocumentCase(
            "Depo Fisi",
            "Duru Gıda teslimatında gelen ürünlerin son kullanma tarihleri kontrol edildi.\n\nEksik kalem yok. Depo fişi tedarikçi kaşesiyle dosyalanacak.",
            "duru"),
        new DocumentCase(
            "Dava Dosyasi",
            "Yılmaz Hukuk için hazırlanan görüşme notları sözleşme klasörüne eklendi.\n\nYetkili kişi imza attı. Şirket adına uygun kaşeyi seç.",
            "yilmaz"),
    };

    [Header("Trigger")]
    [SerializeField] private bool listenToClickableDeskObjects = true;
    [SerializeField] private ClickableDeskObject stampDeskObject;

    [Header("Game Setup")]
    [SerializeField] private int documentCount = 8;
    [SerializeField] private float nextDocumentDelay = 0.65f;

    [Header("Task Timer")]
    [SerializeField] private bool useTaskTimer;
    [SerializeField] private TaskTimer taskTimer;
    [SerializeField] private float taskDurationSeconds = 75f;
    [SerializeField] private TimeoutBehavior timeoutBehavior = TimeoutBehavior.FailAndShowResult;

    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private RectTransform documentArea;
    [SerializeField] private RectTransform stampHome;
    [SerializeField] private TextMeshProUGUI correctLabel;
    [SerializeField] private TextMeshProUGUI wrongLabel;
    [SerializeField] private TextMeshProUGUI statusLabel;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultLabel;

    private TextMeshProUGUI progressLabel;
    private TextMeshProUGUI documentTitleLabel;
    private TextMeshProUGUI documentBodyLabel;
    private TextMeshProUGUI stampMarkLabel;
    private Image stampMarkImage;
    private DocumentCase currentCase;
    private int currentDocumentIndex;
    private int correctCount;
    private int wrongCount;
    private bool awaitingNextDocument;
    private bool taskStarted;
    private bool taskEnded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void RegisterSceneLoadedHook()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallForInitialScene()
    {
        EnsureManagerForScene(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureManagerForScene(scene);
    }

    private static void EnsureManagerForScene(Scene scene)
    {
        if (!IsLevelScene(scene.name) || FindAnyObjectByType<StampMiniGameManager>() != null)
        {
            return;
        }

        GameObject managerObject = new GameObject(ControllerName);
        managerObject.AddComponent<StampMiniGameManager>();
    }

    private static bool IsLevelScene(string sceneName)
    {
        LevelDefinition level = LevelDatabase.Load().GetLevelBySceneName(sceneName);
        return level != null;
    }

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

        if (taskTimer != null)
        {
            taskTimer.TimerExpired -= HandleTaskTimerExpired;
        }
    }

    public void StartMiniGame()
    {
        if (!TaskAssignmentSession.IsTaskEnabled(TaskKind))
        {
            return;
        }

        EnsureUi();
        if (taskStarted && !taskEnded)
        {
            panelRoot.SetActive(true);
            return;
        }

        ResetGame();
        taskStarted = true;
        taskEnded = false;
        CreateStampButtons();
        panelRoot.SetActive(true);
        resultPanel.SetActive(false);
        if (TaskAssignmentSession.TryGetAssignment(TaskKind, out TaskAssignment assignment))
        {
            useTaskTimer = true;
            taskDurationSeconds = assignment.TimeLimitSeconds;
        }

        if (useTaskTimer)
        {
            StartTaskTimer();
        }
        ShowCurrentDocument();
    }

    public void CloseMiniGame()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    public void FinishMiniGame()
    {
        StopAllCoroutines();
        awaitingNextDocument = false;

        if (taskTimer != null)
        {
            taskTimer.StopTimer();
        }

        taskEnded = true;
        TaskAssignmentSession.MarkTaskCompleted(TaskKind);
        resultPanel.SetActive(true);
        int accuracy = TaskAssignmentSession.CalculateAccuracyPercent(correctCount, wrongCount);
        resultLabel.text = $"Sonuç\nDoğru: {correctCount}\nYanlış: {wrongCount}\n{TaskAssignmentSession.BuildAccuracyLine(TaskKind, accuracy)}";
        RefreshHud("Kaşe görevi tamamlandı.");
    }

    private void StartTaskTimer()
    {
        if (taskTimer == null)
        {
            taskTimer = gameObject.AddComponent<TaskTimer>();
        }

        taskTimer.TimerExpired -= HandleTaskTimerExpired;
        taskTimer.TimerExpired += HandleTaskTimerExpired;
        taskTimer.StartTimer(taskDurationSeconds);
        TaskAssignmentSession.RegisterTaskTimer(TaskKind, taskTimer);
    }

    private void HandleTaskTimerExpired(TaskTimer expiredTimer)
    {
        if (timeoutBehavior == TimeoutBehavior.FinishMiniGame)
        {
            FinishMiniGame();
            return;
        }

        StopAllCoroutines();
        awaitingNextDocument = false;
        taskEnded = true;
        TaskAssignmentSession.MarkTaskFailed(TaskKind);
        resultPanel.SetActive(true);
        int accuracy = TaskAssignmentSession.CalculateAccuracyPercent(correctCount, wrongCount);
        resultLabel.text = $"Süre Bitti\nDoğru: {correctCount}\nYanlış: {wrongCount}\n{TaskAssignmentSession.BuildAccuracyLine(TaskKind, accuracy)}\nGörev başarısız.";
        RefreshHud("Süre bitti. Görev başarısız.");
    }

    private void HandleDeskObjectClicked(ClickableDeskObject clickedObject)
    {
        if (IsMainStampObject(clickedObject))
        {
            StartMiniGame();
        }
    }

    private bool IsMainStampObject(ClickableDeskObject clickedObject)
    {
        if (clickedObject == null)
        {
            return false;
        }

        if (stampDeskObject != null && clickedObject == stampDeskObject)
        {
            return true;
        }

        string clickedText = NormalizeDeskText($"{clickedObject.ObjectId} {clickedObject.DisplayName} {clickedObject.gameObject.name}");
        if (clickedText.Contains("tarih") || clickedText.Contains("date"))
        {
            return false;
        }

        return clickedText.Contains("kase")
            || clickedText.Contains("kaseler")
            || clickedText.Contains("stamp")
            || clickedText.Contains("muhur");
    }

    private static string NormalizeDeskText(string text)
    {
        return text.ToLowerInvariant()
            .Replace(" ", string.Empty)
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .Replace("ş", "s")
            .Replace("ı", "i")
            .Replace("ğ", "g")
            .Replace("ü", "u")
            .Replace("ö", "o")
            .Replace("ç", "c")
            .Replace("åÿ", "s")
            .Replace("åş", "s")
            .Replace("å", "s")
            .Replace("ä±", "i")
            .Replace("ä°", "i")
            .Replace("äŸ", "g")
            .Replace("ã¼", "u")
            .Replace("ã¶", "o")
            .Replace("ã§", "c")
            .Replace("ãœ", "u")
            .Replace("ã–", "o")
            .Replace("ã‡", "c");
    }

    private void ResetGame()
    {
        StopAllCoroutines();
        correctCount = 0;
        wrongCount = 0;
        currentDocumentIndex = 0;
        awaitingNextDocument = false;

        if (stampHome != null)
        {
            OfficeMiniGameUi.ClearChildren(stampHome);
        }

        HideStampMark();
        RefreshHud("Belgedeki kişi veya şirket adıyla eşleşen kaşeyi seç.");
    }

    private void CreateStampButtons()
    {
        for (int i = 0; i < StampDefinitions.Length; i++)
        {
            StampDefinition stamp = StampDefinitions[i];
            Button button = CreateStampButton(stampHome, stamp);
            StampDefinition capturedStamp = stamp;
            button.onClick.AddListener(() => SelectStamp(capturedStamp));
        }
    }

    private Button CreateStampButton(Transform parent, StampDefinition stamp)
    {
        GameObject buttonObject = OfficeMiniGameUi.CreateImage($"Stamp {stamp.Label}", parent, new Color(1f, 1f, 1f, 0f));
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(230f, 92f);

        Button button = buttonObject.AddComponent<Button>();
        Image targetImage = buttonObject.GetComponent<Image>();
        targetImage.raycastTarget = true;
        button.targetGraphic = targetImage;

        GameObject visual = OfficeMiniGameUi.CreateImage("StampVisual", buttonObject.transform, stamp.Color);
        RectTransform visualRect = visual.GetComponent<RectTransform>();
        visualRect.anchorMin = new Vector2(0.5f, 0.5f);
        visualRect.anchorMax = new Vector2(0.5f, 0.5f);
        visualRect.sizeDelta = GetStampVisualSize(stamp.Shape);
        visualRect.anchoredPosition = Vector2.zero;
        visualRect.localRotation = Quaternion.identity;

        Outline outline = visual.AddComponent<Outline>();
        outline.effectColor = new Color(0.08f, 0.04f, 0.03f, 0.85f);
        outline.effectDistance = new Vector2(3f, -3f);

        TextMeshProUGUI label = OfficeMiniGameUi.CreateLabel("StampLabel", buttonObject.transform, stamp.Label, 25f, Color.white);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        OfficeMiniGameUi.Stretch(labelRect, new Vector2(16f, 12f), new Vector2(-16f, -12f));
        label.enableAutoSizing = true;
        label.fontSizeMin = 16f;
        label.fontSizeMax = 25f;

        return button;
    }

    private void SelectStamp(StampDefinition selectedStamp)
    {
        if (awaitingNextDocument || resultPanel.activeSelf)
        {
            return;
        }

        ApplyStampMark(selectedStamp);

        bool isCorrect = selectedStamp.Id == currentCase.RequiredStampId;
        if (isCorrect)
        {
            correctCount++;
            RefreshHud("Doğru isimli kaşe basıldı.");
        }
        else
        {
            wrongCount++;
            RefreshHud($"Yanlış kaşe. Bu belge için {GetStampLabel(currentCase.RequiredStampId)} gerekiyordu.");
        }

        currentDocumentIndex++;
        if (currentDocumentIndex >= Mathf.Max(1, documentCount))
        {
            StartCoroutine(FinishAfterDelay());
            return;
        }

        StartCoroutine(ShowNextDocumentAfterDelay());
    }

    private IEnumerator ShowNextDocumentAfterDelay()
    {
        awaitingNextDocument = true;
        yield return new WaitForSecondsRealtime(nextDocumentDelay);
        awaitingNextDocument = false;
        ShowCurrentDocument();
    }

    private IEnumerator FinishAfterDelay()
    {
        awaitingNextDocument = true;
        yield return new WaitForSecondsRealtime(nextDocumentDelay);
        FinishMiniGame();
    }

    private void ShowCurrentDocument()
    {
        currentCase = DocumentCases[currentDocumentIndex % DocumentCases.Length];
        HideStampMark();

        if (documentTitleLabel != null)
        {
            documentTitleLabel.text = currentCase.Title;
        }

        if (documentBodyLabel != null)
        {
            documentBodyLabel.text = currentCase.Body;
        }

        RefreshHud("Belgedeki kişi veya şirket adıyla eşleşen kaşeyi seç.");
    }

    private void ApplyStampMark(StampDefinition stamp)
    {
        if (stampMarkImage == null || stampMarkLabel == null)
        {
            return;
        }

        stampMarkImage.color = new Color(stamp.Color.r, stamp.Color.g, stamp.Color.b, 0.22f);
        stampMarkLabel.text = stamp.Label;
        stampMarkLabel.color = stamp.Color;
        stampMarkLabel.gameObject.SetActive(true);
        stampMarkImage.gameObject.SetActive(true);
    }

    private void HideStampMark()
    {
        if (stampMarkImage != null)
        {
            stampMarkImage.gameObject.SetActive(false);
        }
    }

    private void RefreshHud(string statusText)
    {
        if (correctLabel != null)
        {
            correctLabel.text = $"Doğru: {correctCount}";
        }

        if (wrongLabel != null)
        {
            wrongLabel.text = $"Yanlış: {wrongCount}";
        }

        if (progressLabel != null)
        {
            progressLabel.text = $"Belge: {Mathf.Min(currentDocumentIndex + 1, Mathf.Max(1, documentCount))}/{Mathf.Max(1, documentCount)}";
        }

        if (statusLabel != null)
        {
            statusLabel.text = statusText;
        }
    }

    private void EnsureUi()
    {
        if (panelRoot != null && documentArea != null && stampHome != null && documentTitleLabel != null && stampMarkImage != null)
        {
            return;
        }

        Canvas canvas = OfficeMiniGameUi.CreateOverlayCanvas("Stamp Mini Game Canvas", transform, 1011);
        panelRoot = OfficeMiniGameUi.CreateImage("Stamp Mini Game Panel", canvas.transform, new Color(0f, 0f, 0f, 0.58f));
        OfficeMiniGameUi.Stretch(panelRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        GameObject board = OfficeMiniGameUi.CreateImage("Stamp Board", panelRoot.transform, new Color(0.77f, 0.71f, 0.62f, 1f));
        RectTransform boardRect = board.GetComponent<RectTransform>();
        boardRect.sizeDelta = new Vector2(1280f, 800f);
        boardRect.anchoredPosition = Vector2.zero;

        CreateHud(board.transform);
        CreateDocumentArea(board.transform);
        CreateStampRack(board.transform);
        CreateResultPanel(board.transform);
        CreateCloseButton(panelRoot.transform);

        panelRoot.SetActive(false);
    }

    private void CreateHud(Transform board)
    {
        correctLabel = OfficeMiniGameUi.CreateLabel("Correct", board, "Doğru: 0", 24f, Color.white);
        RectTransform correctRect = correctLabel.GetComponent<RectTransform>();
        correctRect.anchorMin = new Vector2(0f, 1f);
        correctRect.anchorMax = new Vector2(0f, 1f);
        correctRect.sizeDelta = new Vector2(180f, 40f);
        correctRect.anchoredPosition = new Vector2(120f, -42f);

        wrongLabel = OfficeMiniGameUi.CreateLabel("Wrong", board, "Yanlış: 0", 24f, Color.white);
        RectTransform wrongRect = wrongLabel.GetComponent<RectTransform>();
        wrongRect.anchorMin = new Vector2(0f, 1f);
        wrongRect.anchorMax = new Vector2(0f, 1f);
        wrongRect.sizeDelta = new Vector2(180f, 40f);
        wrongRect.anchoredPosition = new Vector2(310f, -42f);

        progressLabel = OfficeMiniGameUi.CreateLabel("Progress", board, "Belge: 1/8", 24f, Color.white);
        RectTransform progressRect = progressLabel.GetComponent<RectTransform>();
        progressRect.anchorMin = new Vector2(0.5f, 1f);
        progressRect.anchorMax = new Vector2(0.5f, 1f);
        progressRect.sizeDelta = new Vector2(220f, 40f);
        progressRect.anchoredPosition = new Vector2(0f, -42f);

        statusLabel = OfficeMiniGameUi.CreateLabel("Status", board, string.Empty, 22f, Color.white);
        RectTransform statusRect = statusLabel.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0f, 0f);
        statusRect.anchorMax = new Vector2(1f, 0f);
        statusRect.offsetMin = new Vector2(60f, 28f);
        statusRect.offsetMax = new Vector2(-340f, 84f);
        statusLabel.alignment = TextAlignmentOptions.Left;
    }

    private void CreateDocumentArea(Transform board)
    {
        GameObject paper = OfficeMiniGameUi.CreateImage("Report Document", board, new Color(0.98f, 0.96f, 0.9f, 1f));
        documentArea = paper.GetComponent<RectTransform>();
        documentArea.anchorMin = new Vector2(0f, 0.5f);
        documentArea.anchorMax = new Vector2(0f, 0.5f);
        documentArea.sizeDelta = new Vector2(760f, 610f);
        documentArea.anchoredPosition = new Vector2(450f, 10f);

        Outline outline = paper.AddComponent<Outline>();
        outline.effectColor = new Color(0.3f, 0.24f, 0.16f, 0.72f);
        outline.effectDistance = new Vector2(3f, -3f);

        documentTitleLabel = OfficeMiniGameUi.CreateLabel("DocumentTitle", paper.transform, string.Empty, 34f, new Color(0.12f, 0.1f, 0.08f, 1f));
        RectTransform titleRect = documentTitleLabel.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(44f, -92f);
        titleRect.offsetMax = new Vector2(-44f, -34f);
        documentTitleLabel.alignment = TextAlignmentOptions.Left;

        documentBodyLabel = OfficeMiniGameUi.CreateLabel("DocumentBody", paper.transform, string.Empty, 25f, new Color(0.16f, 0.13f, 0.09f, 1f));
        RectTransform bodyRect = documentBodyLabel.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.offsetMin = new Vector2(48f, 150f);
        bodyRect.offsetMax = new Vector2(-48f, -126f);
        documentBodyLabel.alignment = TextAlignmentOptions.TopLeft;
        documentBodyLabel.textWrappingMode = TextWrappingModes.Normal;

        CreateDocumentLines(paper.transform);
        CreateStampMark(paper.transform);
    }

    private void CreateDocumentLines(Transform paper)
    {
        for (int i = 0; i < 8; i++)
        {
            GameObject line = OfficeMiniGameUi.CreateImage($"Document Line {i + 1}", paper, new Color(0.65f, 0.61f, 0.52f, 0.25f));
            RectTransform lineRect = line.GetComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0f, 0f);
            lineRect.anchorMax = new Vector2(1f, 0f);
            lineRect.offsetMin = new Vector2(48f, 118f + i * 42f);
            lineRect.offsetMax = new Vector2(-48f - (i % 3) * 46f, 122f + i * 42f);
        }
    }

    private void CreateStampMark(Transform paper)
    {
        GameObject markObject = OfficeMiniGameUi.CreateImage("Selected Stamp Mark", paper, new Color(0.73f, 0.12f, 0.11f, 0.22f));
        RectTransform markRect = markObject.GetComponent<RectTransform>();
        markRect.anchorMin = new Vector2(1f, 0f);
        markRect.anchorMax = new Vector2(1f, 0f);
        markRect.sizeDelta = new Vector2(210f, 86f);
        markRect.anchoredPosition = new Vector2(-172f, 96f);
        markRect.localRotation = Quaternion.Euler(0f, 0f, -8f);

        Outline markOutline = markObject.AddComponent<Outline>();
        markOutline.effectColor = new Color(0.55f, 0.08f, 0.08f, 0.75f);
        markOutline.effectDistance = new Vector2(2f, -2f);

        stampMarkImage = markObject.GetComponent<Image>();
        stampMarkLabel = OfficeMiniGameUi.CreateLabel("StampMarkLabel", markObject.transform, string.Empty, 28f, new Color(0.73f, 0.12f, 0.11f, 1f));
        OfficeMiniGameUi.Stretch(stampMarkLabel.GetComponent<RectTransform>(), new Vector2(12f, 8f), new Vector2(-12f, -8f));
        stampMarkLabel.enableAutoSizing = true;
        stampMarkLabel.fontSizeMin = 18f;
        stampMarkLabel.fontSizeMax = 28f;
        HideStampMark();
    }

    private void CreateStampRack(Transform board)
    {
        GameObject rack = OfficeMiniGameUi.CreateImage("Stamp Rack", board, new Color(0.34f, 0.22f, 0.18f, 1f));
        stampHome = rack.GetComponent<RectTransform>();
        stampHome.anchorMin = new Vector2(1f, 0.5f);
        stampHome.anchorMax = new Vector2(1f, 0.5f);
        stampHome.sizeDelta = new Vector2(310f, 610f);
        stampHome.anchoredPosition = new Vector2(-210f, 10f);

        VerticalLayoutGroup layout = rack.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.spacing = 20f;
        layout.padding = new RectOffset(0, 0, 28, 28);

        TextMeshProUGUI title = OfficeMiniGameUi.CreateLabel("StampRackTitle", board, "KAŞELER", 26f, Color.white);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(1f, 0.5f);
        titleRect.anchorMax = new Vector2(1f, 0.5f);
        titleRect.sizeDelta = new Vector2(310f, 42f);
        titleRect.anchoredPosition = new Vector2(-210f, 342f);
    }

    private void CreateResultPanel(Transform parent)
    {
        resultPanel = OfficeMiniGameUi.CreateImage("Stamp Result Panel", parent, new Color(0.95f, 0.93f, 0.86f, 0.97f));
        RectTransform resultRect = resultPanel.GetComponent<RectTransform>();
        resultRect.sizeDelta = new Vector2(420f, 260f);
        resultRect.anchoredPosition = Vector2.zero;

        resultLabel = OfficeMiniGameUi.CreateLabel("ResultLabel", resultPanel.transform, string.Empty, 28f, new Color(0.12f, 0.1f, 0.08f, 1f));
        OfficeMiniGameUi.Stretch(resultLabel.GetComponent<RectTransform>(), new Vector2(24f, 34f), new Vector2(-24f, -84f));

        Button closeResult = OfficeMiniGameUi.CreateButton("Close Result", resultPanel.transform, "KAPAT", new Vector2(150f, 50f), new Color(0.16f, 0.16f, 0.18f, 1f), CloseMiniGame);
        RectTransform closeRect = closeResult.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.5f, 0f);
        closeRect.anchorMax = new Vector2(0.5f, 0f);
        closeRect.anchoredPosition = new Vector2(0f, 46f);
    }

    private void CreateCloseButton(Transform parent)
    {
        Button closeButton = OfficeMiniGameUi.CreateButton("Close Stamp Game", parent, "X", new Vector2(54f, 54f), new Color(0.08f, 0.08f, 0.09f, 0.95f), CloseMiniGame);
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.5f, 0.5f);
        closeRect.anchorMax = new Vector2(0.5f, 0.5f);
        closeRect.anchoredPosition = new Vector2(665f, 435f);
    }

    private static Vector2 GetStampVisualSize(StampShape shape)
    {
        return new Vector2(194f, 72f);
    }

    private static string GetStampLabel(string stampId)
    {
        for (int i = 0; i < StampDefinitions.Length; i++)
        {
            if (StampDefinitions[i].Id == stampId)
            {
                return StampDefinitions[i].Label.Replace("\n", " ");
            }
        }

        return stampId;
    }

    private readonly struct StampDefinition
    {
        public readonly string Id;
        public readonly string Label;
        public readonly Color Color;
        public readonly StampShape Shape;

        public StampDefinition(string id, string label, Color color, StampShape shape)
        {
            Id = id;
            Label = label;
            Color = color;
            Shape = shape;
        }
    }

    private readonly struct DocumentCase
    {
        public readonly string Title;
        public readonly string Body;
        public readonly string RequiredStampId;

        public DocumentCase(string title, string body, string requiredStampId)
        {
            Title = title;
            Body = body;
            RequiredStampId = requiredStampId;
        }
    }

    private enum StampShape
    {
        Rectangle,
        Diamond,
        Tall,
        Wide,
    }

    private enum TimeoutBehavior
    {
        FailAndShowResult,
        FinishMiniGame,
    }
}
