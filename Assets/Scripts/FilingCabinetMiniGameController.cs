using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class FilingCabinetMiniGameController : MonoBehaviour
{
    private const string ControllerName = "_FilingCabinetMiniGameController";
    private const OfficeTaskKind TaskKind = OfficeTaskKind.Filing;
    private static readonly CultureInfo TurkishCulture = new CultureInfo("tr-TR");

    private static readonly string[] FolderNames =
    {
        "Kuzey Mobilya",
        "Liman Kırtasiye",
        "Nova Sigorta",
        "Pera Ajans",
    };

    private static readonly FileTask[] FileTasks =
    {
        new FileTask(
            "Teslimat Tutanağı",
            "Sabah gelen teslimatta iki masa, dört sandalye ve yedek ayak takımı depo tarafından sayıldı.\n\nÜrünlerin showroom için ayrıldığı not edildi. Bu belge mobilya işiyle ilgili müşteri dosyasına eklenmeli.",
            "Kuzey Mobilya"),
        new FileTask(
            "Sipariş Formu",
            "Ofis sarf malzemeleri listesinde kalem, klasör ve zarf adedi yeniden düzenlendi.\n\nSatın alma onayı alındı. Belge kırtasiye işiyle ilgili dosyada saklanacak.",
            "Liman Kırtasiye"),
        new FileTask(
            "Poliçe Notu",
            "Yenileme kaydında başlangıç tarihi, bitiş tarihi ve müşteri numarası karşılaştırıldı.\n\nBu evrak sigorta işiyle ilgili olduğu için uygun müşteri dosyasına kaldırılmalı.",
            "Nova Sigorta"),
        new FileTask(
            "Kampanya Briefi",
            "Sosyal medya görselleri için renk paleti, slogan ve paylaşım takvimi not edildi.\n\nBrief, ajans işiyle ilgili müşteri dosyasına konulacak.",
            "Pera Ajans"),
        new FileTask(
            "İade Formu",
            "Showroom ürünlerinden birinin iade kaydı tamamlandı ve depo giriş fişine işaretlendi.\n\nForm, mobilya işiyle ilgili aynı müşteri dosyasında saklanmalı.",
            "Kuzey Mobilya"),
        new FileTask(
            "Fatura Kopyası",
            "Fatura kopyasındaki kalem, kağıt ve dosya tutarları satın alma kaydıyla eşleşti.\n\nBelge, kırtasiye işiyle ilgili müşteri dosyasına eklenmeli.",
            "Liman Kırtasiye"),
    };

    private GameObject panelRoot;
    private TextMeshProUGUI titleLabel;
    private TextMeshProUGUI bodyLabel;
    private TextMeshProUGUI progressLabel;
    private TextMeshProUGUI statusLabel;
    private TextMeshProUGUI mistakeLabel;
    private TextMeshProUGUI resultLabel;
    private TextMeshProUGUI resultStatsLabel;
    private GameObject resultPanel;
    private TaskTimer taskTimer;
    private readonly List<FileTask> taskOrder = new List<FileTask>();

    private int currentTaskIndex;
    private int correctCount;
    private int mistakeCount;
    private bool taskStarted;
    private bool taskEnded;

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
        if (clickedObject == null || !IsFilingCabinetObject(clickedObject))
        {
            return;
        }

        EnsureController().Open();
    }

    private static FilingCabinetMiniGameController EnsureController()
    {
        FilingCabinetMiniGameController controller = FindAnyObjectByType<FilingCabinetMiniGameController>();
        if (controller != null)
        {
            return controller;
        }

        GameObject controllerObject = new GameObject(ControllerName);
        return controllerObject.AddComponent<FilingCabinetMiniGameController>();
    }

    private static bool IsFilingCabinetObject(ClickableDeskObject clickedObject)
    {
        string text = NormalizeTurkish($"{clickedObject.ObjectId} {clickedObject.DisplayName} {clickedObject.gameObject.name}");
        return text.Contains("dosyalik") || text.Contains("tamamlananlardosyasi") || text.Contains("tamamlananlar dosyasi");
    }

    private static string NormalizeTurkish(string text)
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

    private void Awake()
    {
        BuildUi();
    }

    private void Open()
    {
        if (!TaskAssignmentSession.IsTaskEnabled(TaskKind))
        {
            return;
        }

        if (taskStarted && !taskEnded)
        {
            panelRoot.SetActive(true);
            return;
        }

        ShuffleTasks();
        currentTaskIndex = 0;
        correctCount = 0;
        mistakeCount = 0;
        taskStarted = true;
        taskEnded = false;
        panelRoot.SetActive(true);
        resultPanel.SetActive(false);
        StartTaskTimer();
        ShowCurrentFile("Belgeyi oku ve doğru müşteri dosyasını seç.");
    }

    private void Close()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private void SelectFolder(string folderName)
    {
        if (resultPanel.activeSelf || currentTaskIndex >= taskOrder.Count)
        {
            return;
        }

        FileTask task = taskOrder[currentTaskIndex];
        if (folderName != task.TargetFolder)
        {
            mistakeCount++;
            RefreshHud($"Yanlış dosya. Bu belge {task.TargetFolder} dosyasına ait.");
            return;
        }

        correctCount++;
        currentTaskIndex++;

        if (currentTaskIndex >= taskOrder.Count)
        {
            ShowResult();
            return;
        }

        ShowCurrentFile("Doğru dosya. Sıradaki belgeyi yerleştir.");
    }

    private void ShowCurrentFile(string statusText)
    {
        FileTask task = taskOrder[currentTaskIndex];
        titleLabel.text = task.Title;
        bodyLabel.text = task.Body;
        RefreshHud(statusText);
    }

    private void RefreshHud(string statusText)
    {
        int totalTasks = Mathf.Max(1, taskOrder.Count);
        int attempts = correctCount + mistakeCount;
        int accuracy = attempts > 0 ? Mathf.RoundToInt(correctCount / (float)attempts * 100f) : 0;
        progressLabel.text = $"{Mathf.Min(currentTaskIndex + 1, totalTasks)}/{totalTasks}";
        mistakeLabel.text = $"Yanlış: {mistakeCount}\nDoğruluk: %{accuracy}";
        statusLabel.text = statusText;
    }

    private void ShowResult()
    {
        TaskAssignmentSession.MarkTaskCompleted(TaskKind);
        resultPanel.SetActive(true);
        int attempts = correctCount + mistakeCount;
        int accuracy = attempts > 0 ? Mathf.RoundToInt(correctCount / (float)attempts * 100f) : 0;
        resultLabel.text = "Dosyalama Tamamlandı";
        resultStatsLabel.text = $"Doğru: {correctCount}\nYanlış: {mistakeCount}\n{TaskAssignmentSession.BuildAccuracyLine(TaskKind, accuracy)}";
        RefreshHud("Tüm belgeler dosyalandı.");

        if (taskTimer != null)
        {
            taskTimer.StopTimer();
        }

        taskEnded = true;
    }

    private void StartTaskTimer()
    {
        if (!TaskAssignmentSession.TryGetAssignment(TaskKind, out TaskAssignment assignment))
        {
            return;
        }

        if (taskTimer == null)
        {
            taskTimer = gameObject.AddComponent<TaskTimer>();
        }

        taskTimer.TimerExpired -= HandleTaskTimerExpired;
        taskTimer.TimerExpired += HandleTaskTimerExpired;
        taskTimer.StartTimer(assignment.TimeLimitSeconds);
        TaskAssignmentSession.RegisterTaskTimer(TaskKind, taskTimer);
    }

    private void HandleTaskTimerExpired(TaskTimer expiredTimer)
    {
        TaskAssignmentSession.MarkTaskFailed(TaskKind);
        resultPanel.SetActive(true);
        taskEnded = true;
        int accuracy = TaskAssignmentSession.CalculateAccuracyPercent(correctCount, mistakeCount);
        resultLabel.text = "Süre Bitti";
        resultStatsLabel.text = $"Doğru: {correctCount}\nYanlış: {mistakeCount}\n{TaskAssignmentSession.BuildAccuracyLine(TaskKind, accuracy)}";
        RefreshHud("Süre bitti. Görev başarısız.");
    }

    private void ShuffleTasks()
    {
        taskOrder.Clear();
        taskOrder.AddRange(FileTasks);

        for (int i = 0; i < taskOrder.Count; i++)
        {
            int swapIndex = Random.Range(i, taskOrder.Count);
            (taskOrder[i], taskOrder[swapIndex]) = (taskOrder[swapIndex], taskOrder[i]);
        }
    }

    private void BuildUi()
    {
        if (panelRoot != null)
        {
            return;
        }

        Canvas canvas = OfficeMiniGameUi.CreateOverlayCanvas("Filing Cabinet Canvas", transform, 1013);
        panelRoot = OfficeMiniGameUi.CreateImage("Filing Cabinet Panel", canvas.transform, new Color(0f, 0f, 0f, 0.58f));
        OfficeMiniGameUi.Stretch(panelRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        GameObject board = OfficeMiniGameUi.CreateImage("Filing Cabinet Board", panelRoot.transform, new Color(0.87f, 0.81f, 0.7f, 1f));
        RectTransform boardRect = board.GetComponent<RectTransform>();
        boardRect.sizeDelta = new Vector2(1120f, 720f);
        boardRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI header = OfficeMiniGameUi.CreateLabel("Header", board.transform, "DOSYA TAMAMLAMA", 34f, new Color(0.12f, 0.1f, 0.08f, 1f));
        RectTransform headerRect = header.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.offsetMin = new Vector2(44f, -80f);
        headerRect.offsetMax = new Vector2(-44f, -28f);

        CreateDocumentCard(board.transform);
        CreateFolderButtons(board.transform);
        CreateFooter(board.transform);
        CreateResultPanel(board.transform);
        CreateCloseButton(panelRoot.transform);

        panelRoot.SetActive(false);
    }

    private void CreateDocumentCard(Transform parent)
    {
        GameObject card = OfficeMiniGameUi.CreateImage("File Document", parent, new Color(0.98f, 0.96f, 0.9f, 1f));
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0f, 0.5f);
        cardRect.anchorMax = new Vector2(0f, 0.5f);
        cardRect.sizeDelta = new Vector2(640f, 470f);
        cardRect.anchoredPosition = new Vector2(390f, 20f);

        titleLabel = OfficeMiniGameUi.CreateLabel("FileTitle", card.transform, string.Empty, 30f, new Color(0.1f, 0.09f, 0.08f, 1f));
        RectTransform titleRect = titleLabel.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(38f, -82f);
        titleRect.offsetMax = new Vector2(-38f, -28f);
        titleLabel.alignment = TextAlignmentOptions.Left;

        bodyLabel = OfficeMiniGameUi.CreateLabel("FileBody", card.transform, string.Empty, 23f, new Color(0.12f, 0.11f, 0.1f, 1f));
        RectTransform bodyRect = bodyLabel.GetComponent<RectTransform>();
        bodyRect.anchorMin = Vector2.zero;
        bodyRect.anchorMax = Vector2.one;
        bodyRect.offsetMin = new Vector2(42f, 72f);
        bodyRect.offsetMax = new Vector2(-42f, -112f);
        bodyLabel.alignment = TextAlignmentOptions.TopLeft;
        bodyLabel.textWrappingMode = TextWrappingModes.Normal;
        bodyLabel.fontStyle = FontStyles.Normal;

        progressLabel = OfficeMiniGameUi.CreateLabel("Progress", card.transform, "1/6", 20f, new Color(0.28f, 0.24f, 0.18f, 1f));
        RectTransform progressRect = progressLabel.GetComponent<RectTransform>();
        progressRect.anchorMin = new Vector2(1f, 0f);
        progressRect.anchorMax = new Vector2(1f, 0f);
        progressRect.sizeDelta = new Vector2(110f, 40f);
        progressRect.anchoredPosition = new Vector2(-74f, 42f);
    }

    private void CreateFolderButtons(Transform parent)
    {
        for (int i = 0; i < FolderNames.Length; i++)
        {
            string folderName = FolderNames[i];
            Button button = OfficeMiniGameUi.CreateButton(
                $"Folder {folderName}",
                parent,
                folderName.ToUpper(TurkishCulture),
                new Vector2(300f, 78f),
                new Color(0.56f, 0.38f, 0.18f, 1f),
                () => SelectFolder(folderName));

            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-220f, 185f - i * 102f);

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
            label.enableAutoSizing = true;
            label.fontSizeMin = 15f;
            label.fontSizeMax = 20f;
        }
    }

    private void CreateFooter(Transform parent)
    {
        statusLabel = OfficeMiniGameUi.CreateLabel("Status", parent, string.Empty, 22f, new Color(0.15f, 0.11f, 0.08f, 1f));
        RectTransform statusRect = statusLabel.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0f, 0f);
        statusRect.anchorMax = new Vector2(1f, 0f);
        statusRect.offsetMin = new Vector2(58f, 42f);
        statusRect.offsetMax = new Vector2(-300f, 98f);
        statusLabel.alignment = TextAlignmentOptions.Left;

        mistakeLabel = OfficeMiniGameUi.CreateLabel("Mistakes", parent, "Yanlış: 0\nDoğruluk: %0", 22f, new Color(0.62f, 0.08f, 0.06f, 1f));
        RectTransform mistakeRect = mistakeLabel.GetComponent<RectTransform>();
        mistakeRect.anchorMin = new Vector2(1f, 0f);
        mistakeRect.anchorMax = new Vector2(1f, 0f);
        mistakeRect.sizeDelta = new Vector2(230f, 64f);
        mistakeRect.anchoredPosition = new Vector2(-170f, 62f);
        mistakeLabel.alignment = TextAlignmentOptions.Right;
    }

    private void CreateResultPanel(Transform parent)
    {
        resultPanel = OfficeMiniGameUi.CreateImage("Filing Result Panel", parent, new Color(0.96f, 0.93f, 0.84f, 0.98f));
        RectTransform resultRect = resultPanel.GetComponent<RectTransform>();
        resultRect.sizeDelta = new Vector2(460f, 280f);
        resultRect.anchoredPosition = Vector2.zero;

        resultLabel = OfficeMiniGameUi.CreateLabel("ResultLabel", resultPanel.transform, string.Empty, 28f, new Color(0.12f, 0.1f, 0.08f, 1f));
        RectTransform resultLabelRect = resultLabel.GetComponent<RectTransform>();
        resultLabelRect.anchorMin = new Vector2(0f, 1f);
        resultLabelRect.anchorMax = new Vector2(1f, 1f);
        resultLabelRect.offsetMin = new Vector2(24f, -86f);
        resultLabelRect.offsetMax = new Vector2(-24f, -34f);

        resultStatsLabel = OfficeMiniGameUi.CreateLabel("ResultStats", resultPanel.transform, string.Empty, 20f, new Color(0.12f, 0.1f, 0.08f, 1f));
        RectTransform statsRect = resultStatsLabel.GetComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(1f, 0f);
        statsRect.anchorMax = new Vector2(1f, 0f);
        statsRect.sizeDelta = new Vector2(220f, 128f);
        statsRect.anchoredPosition = new Vector2(-124f, 72f);
        resultStatsLabel.alignment = TextAlignmentOptions.Right;

        Button closeResult = OfficeMiniGameUi.CreateButton("Close Result", resultPanel.transform, "KAPAT", new Vector2(150f, 50f), new Color(0.16f, 0.16f, 0.18f, 1f), Close);
        RectTransform closeRect = closeResult.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0f, 0f);
        closeRect.anchorMax = new Vector2(0f, 0f);
        closeRect.anchoredPosition = new Vector2(104f, 48f);
    }

    private void CreateCloseButton(Transform parent)
    {
        Button closeButton = OfficeMiniGameUi.CreateButton("Close Filing Cabinet", parent, "X", new Vector2(54f, 54f), new Color(0.08f, 0.08f, 0.09f, 0.95f), Close);
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.5f, 0.5f);
        closeRect.anchorMax = new Vector2(0.5f, 0.5f);
        closeRect.anchoredPosition = new Vector2(585f, 390f);
    }

    private readonly struct FileTask
    {
        public readonly string Title;
        public readonly string Body;
        public readonly string TargetFolder;

        public FileTask(string title, string body, string targetFolder)
        {
            Title = title;
            Body = body;
            TargetFolder = targetFolder;
        }
    }
}
