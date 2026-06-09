using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PrinterSortingMiniGameManager : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private bool listenToClickableDeskObjects = true;
    [SerializeField] private ClickableDeskObject printerObject;

    [Header("Prefabs")]
    [SerializeField] private GameObject printedPaperPrefab;
    [SerializeField] private GameObject folderPrefab;

    [Header("Game Setup")]
    [SerializeField] private List<string> companyNames = new List<string>
    {
        "Arda Tekstil",
        "Mavi Lojistik",
        "Kuzey Gida",
        "Nova Yazilim",
        "Atlas Insaat",
        "Luna Medikal",
    };
    [SerializeField] private int paperCount = 12;

    [Header("Task Timer")]
    [SerializeField] private TaskTimer taskTimer;
    [SerializeField] private float taskDurationSeconds = 90f;
    [SerializeField] private TimeoutBehavior timeoutBehavior = TimeoutBehavior.FailAndShowResult;

    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private RectTransform paperParent;
    [SerializeField] private RectTransform folderParent;
    [SerializeField] private TextMeshProUGUI scoreLabel;
    [SerializeField] private TextMeshProUGUI mistakeLabel;
    [SerializeField] private TextMeshProUGUI statusLabel;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultLabel;

    private readonly List<DropZone> folderZones = new List<DropZone>();
    private int score;
    private int mistakes;
    private int sortedCount;

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
        UnsubscribeFolderZones();

        if (taskTimer != null)
        {
            taskTimer.TimerExpired -= HandleTaskTimerExpired;
        }
    }

    public void StartMiniGame()
    {
        EnsureUi();
        EnsureCompanyList();
        ResetGame();
        SpawnFolders();
        SpawnPrintedPapers();
        panelRoot.SetActive(true);
        resultPanel.SetActive(false);
        StartTaskTimer();
        RefreshHud("Yazicidan cikan kagitlari dogru sirket dosyasina birak.");
    }

    public void CloseMiniGame()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        if (taskTimer != null)
        {
            taskTimer.StopTimer();
        }
    }

    public void FinishMiniGame()
    {
        if (taskTimer != null)
        {
            taskTimer.StopTimer();
        }

        resultPanel.SetActive(true);
        resultLabel.text = $"Sonuc\nPuan: {score}\nHata: {mistakes}";
        RefreshHud("Sirket dosyalama gorevi tamamlandi.");
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
    }

    private void HandleTaskTimerExpired(TaskTimer expiredTimer)
    {
        if (timeoutBehavior == TimeoutBehavior.FinishMiniGame)
        {
            FinishMiniGame();
            return;
        }

        resultPanel.SetActive(true);
        resultLabel.text = $"Sure Bitti\nPuan: {score}\nHata: {mistakes}\nGorev basarisiz.";
        RefreshHud("Sure bitti. Gorev basarisiz.");
    }

    private void HandleDeskObjectClicked(ClickableDeskObject clickedObject)
    {
        if (OfficeMiniGameUi.MatchesClickedObject(clickedObject, printerObject, "printer", "yazici"))
        {
            StartMiniGame();
        }
    }

    private void ResetGame()
    {
        score = 0;
        mistakes = 0;
        sortedCount = 0;

        UnsubscribeFolderZones();
        folderZones.Clear();

        if (paperParent != null)
        {
            OfficeMiniGameUi.ClearChildren(paperParent);
        }

        if (folderParent != null)
        {
            OfficeMiniGameUi.ClearChildren(folderParent);
        }
    }

    private void EnsureCompanyList()
    {
        companyNames.RemoveAll(string.IsNullOrWhiteSpace);
        if (companyNames.Count > 0)
        {
            return;
        }

        companyNames.Add("Arda Tekstil");
        companyNames.Add("Mavi Lojistik");
        companyNames.Add("Kuzey Gida");
        companyNames.Add("Nova Yazilim");
        companyNames.Add("Atlas Insaat");
        companyNames.Add("Luna Medikal");
    }

    private void SpawnFolders()
    {
        foreach (string companyName in companyNames)
        {
            GameObject folderObject = CreateFolderObject(companyName);
            DropZone dropZone = folderObject.GetComponent<DropZone>();
            if (dropZone == null)
            {
                dropZone = folderObject.AddComponent<DropZone>();
            }

            dropZone.Configure(companyName, companyName, false);
            dropZone.ItemEvaluated += HandleFolderDrop;
            folderZones.Add(dropZone);
        }
    }

    private void SpawnPrintedPapers()
    {
        for (int i = 0; i < paperCount; i++)
        {
            string companyName = companyNames[i % companyNames.Count];
            GameObject paperObject = CreatePrintedPaperObject(i + 1, companyName);

            RectTransform paperRect = paperObject.GetComponent<RectTransform>();
            paperRect.anchoredPosition = new Vector2(Random.Range(-500f, -130f), Random.Range(-250f, 250f));
            paperRect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-6f, 6f));

            DraggableItem draggableItem = paperObject.GetComponent<DraggableItem>();
            if (draggableItem == null)
            {
                draggableItem = paperObject.AddComponent<DraggableItem>();
            }

            draggableItem.Configure(companyName, true, false);
        }
    }

    private GameObject CreatePrintedPaperObject(int index, string companyName)
    {
        GameObject paperObject;
        if (printedPaperPrefab != null)
        {
            paperObject = Instantiate(printedPaperPrefab, paperParent);
        }
        else
        {
            paperObject = OfficeMiniGameUi.CreateImage($"Printed Paper {index}", paperParent, new Color(0.98f, 0.96f, 0.9f, 1f));
            RectTransform rect = paperObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(190f, 130f);
        }

        TextMeshProUGUI label = paperObject.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label == null)
        {
            label = OfficeMiniGameUi.CreateLabel("CompanyLabel", paperObject.transform, string.Empty, 18f, new Color(0.12f, 0.1f, 0.08f, 1f));
            OfficeMiniGameUi.Stretch(label.GetComponent<RectTransform>(), new Vector2(10f, 10f), new Vector2(-10f, -10f));
        }

        label.text = companyName;
        label.alignment = TextAlignmentOptions.Center;
        return paperObject;
    }

    private GameObject CreateFolderObject(string companyName)
    {
        GameObject folderObject;
        if (folderPrefab != null)
        {
            folderObject = Instantiate(folderPrefab, folderParent);
        }
        else
        {
            folderObject = OfficeMiniGameUi.CreateImage($"{companyName} Folder", folderParent, new Color(0.82f, 0.62f, 0.28f, 1f));
            RectTransform rect = folderObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(230f, 112f);
        }

        TextMeshProUGUI label = folderObject.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label == null)
        {
            label = OfficeMiniGameUi.CreateLabel("FolderLabel", folderObject.transform, string.Empty, 18f, Color.white);
            OfficeMiniGameUi.Stretch(label.GetComponent<RectTransform>(), new Vector2(10f, 8f), new Vector2(-10f, -8f));
        }

        label.text = companyName;
        label.alignment = TextAlignmentOptions.Center;
        return folderObject;
    }

    private void HandleFolderDrop(DraggableItem item, DropZone zone, bool accepted)
    {
        if (item == null)
        {
            return;
        }

        if (accepted)
        {
            score += 10;
            sortedCount++;
            RefreshHud("Dogru dosya.");
            Destroy(item.gameObject);
        }
        else
        {
            mistakes++;
            RefreshHud("Yanlis dosya. Kagit eski yerine dondu.");
        }

        if (sortedCount >= paperCount)
        {
            FinishMiniGame();
        }
    }

    private void RefreshHud(string statusText)
    {
        if (scoreLabel != null)
        {
            scoreLabel.text = $"Puan: {score}";
        }

        if (mistakeLabel != null)
        {
            mistakeLabel.text = $"Hata: {mistakes}";
        }

        if (statusLabel != null)
        {
            statusLabel.text = statusText;
        }
    }

    private void EnsureUi()
    {
        if (panelRoot != null && paperParent != null && folderParent != null)
        {
            return;
        }

        Canvas canvas = OfficeMiniGameUi.CreateOverlayCanvas("Printer Sorting Canvas", transform, 1012);
        panelRoot = OfficeMiniGameUi.CreateImage("Printer Sorting Panel", canvas.transform, new Color(0f, 0f, 0f, 0.58f));
        OfficeMiniGameUi.Stretch(panelRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        GameObject board = OfficeMiniGameUi.CreateImage("Printer Sorting Board", panelRoot.transform, new Color(0.62f, 0.67f, 0.66f, 1f));
        RectTransform boardRect = board.GetComponent<RectTransform>();
        boardRect.sizeDelta = new Vector2(1360f, 820f);
        boardRect.anchoredPosition = Vector2.zero;

        paperParent = new GameObject("Printed Paper Parent", typeof(RectTransform)).GetComponent<RectTransform>();
        paperParent.transform.SetParent(board.transform, false);
        paperParent.anchorMin = new Vector2(0f, 0f);
        paperParent.anchorMax = new Vector2(0.48f, 1f);
        paperParent.offsetMin = new Vector2(34f, 96f);
        paperParent.offsetMax = new Vector2(-20f, -82f);

        folderParent = new GameObject("Folder Grid", typeof(RectTransform), typeof(GridLayoutGroup)).GetComponent<RectTransform>();
        folderParent.transform.SetParent(board.transform, false);
        folderParent.anchorMin = new Vector2(0.52f, 0f);
        folderParent.anchorMax = new Vector2(1f, 1f);
        folderParent.offsetMin = new Vector2(20f, 118f);
        folderParent.offsetMax = new Vector2(-34f, -104f);

        GridLayoutGroup grid = folderParent.GetComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        grid.cellSize = new Vector2(250f, 118f);
        grid.spacing = new Vector2(24f, 28f);

        scoreLabel = OfficeMiniGameUi.CreateLabel("Score", board.transform, "Puan: 0", 24f, Color.white);
        RectTransform scoreRect = scoreLabel.GetComponent<RectTransform>();
        scoreRect.anchorMin = new Vector2(0f, 1f);
        scoreRect.anchorMax = new Vector2(0f, 1f);
        scoreRect.sizeDelta = new Vector2(170f, 42f);
        scoreRect.anchoredPosition = new Vector2(120f, -42f);

        mistakeLabel = OfficeMiniGameUi.CreateLabel("Mistakes", board.transform, "Hata: 0", 24f, Color.white);
        RectTransform mistakeRect = mistakeLabel.GetComponent<RectTransform>();
        mistakeRect.anchorMin = new Vector2(0f, 1f);
        mistakeRect.anchorMax = new Vector2(0f, 1f);
        mistakeRect.sizeDelta = new Vector2(170f, 42f);
        mistakeRect.anchoredPosition = new Vector2(300f, -42f);

        statusLabel = OfficeMiniGameUi.CreateLabel("Status", board.transform, string.Empty, 22f, Color.white);
        RectTransform statusRect = statusLabel.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0f, 0f);
        statusRect.anchorMax = new Vector2(1f, 0f);
        statusRect.offsetMin = new Vector2(44f, 30f);
        statusRect.offsetMax = new Vector2(-260f, 82f);
        statusLabel.alignment = TextAlignmentOptions.Left;

        Button finishButton = OfficeMiniGameUi.CreateButton("Finish Printer", board.transform, "BITIR", new Vector2(150f, 54f), new Color(0.22f, 0.42f, 0.55f, 1f), FinishMiniGame);
        RectTransform finishRect = finishButton.GetComponent<RectTransform>();
        finishRect.anchorMin = new Vector2(1f, 0f);
        finishRect.anchorMax = new Vector2(1f, 0f);
        finishRect.anchoredPosition = new Vector2(-130f, 58f);

        CreateResultPanel(board.transform);
        CreateCloseButton(panelRoot.transform);
    }

    private void CreateResultPanel(Transform parent)
    {
        resultPanel = OfficeMiniGameUi.CreateImage("Printer Result Panel", parent, new Color(0.95f, 0.93f, 0.86f, 0.97f));
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
        Button closeButton = OfficeMiniGameUi.CreateButton("Close Printer Game", parent, "X", new Vector2(54f, 54f), new Color(0.08f, 0.08f, 0.09f, 0.95f), CloseMiniGame);
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.5f, 0.5f);
        closeRect.anchorMax = new Vector2(0.5f, 0.5f);
        closeRect.anchoredPosition = new Vector2(710f, 445f);
    }

    private void UnsubscribeFolderZones()
    {
        foreach (DropZone zone in folderZones)
        {
            if (zone != null)
            {
                zone.ItemEvaluated -= HandleFolderDrop;
            }
        }
    }

    private enum TimeoutBehavior
    {
        FailAndShowResult,
        FinishMiniGame,
    }
}
