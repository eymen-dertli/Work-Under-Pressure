using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class TrashMiniGameManager : MonoBehaviour
{
    private const OfficeTaskKind TaskKind = OfficeTaskKind.Trash;

    [Header("Trigger")]
    [SerializeField] private bool listenToClickableDeskObjects = true;
    [SerializeField] private ClickableDeskObject trashObject;

    [Header("Prefabs")]
    [SerializeField] private GameObject paperPrefab;

    [Header("Game Setup")]
    [SerializeField] private int paperCount = 8;
    [SerializeField] private Vector2 spawnAreaMin = new Vector2(-560f, -210f);
    [SerializeField] private Vector2 spawnAreaMax = new Vector2(260f, 220f);

    [Header("Task Timer")]
    [SerializeField] private TaskTimer taskTimer;
    [SerializeField] private float taskDurationSeconds = 60f;
    [SerializeField] private TimeoutBehavior timeoutBehavior = TimeoutBehavior.FailAndShowResult;

    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private RectTransform paperSpawnParent;
    [SerializeField] private DropZone trashDropZone;
    [SerializeField] private TextMeshProUGUI scoreLabel;
    [SerializeField] private TextMeshProUGUI mistakeLabel;
    [SerializeField] private TextMeshProUGUI statusLabel;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultLabel;

    private readonly HashSet<DraggableItem> processedPapers = new HashSet<DraggableItem>();
    private int score;
    private int mistakes;
    private int correctPaperTarget;
    private int correctPapersThrown;

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
        if (trashDropZone != null)
        {
            trashDropZone.ItemEvaluated -= HandleTrashDrop;
        }

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
        ResetGame();
        SpawnPapers();
        panelRoot.SetActive(true);
        resultPanel.SetActive(false);
        if (TaskAssignmentSession.TryGetAssignment(TaskKind, out TaskAssignment assignment))
        {
            taskDurationSeconds = assignment.TimeLimitSeconds;
        }

        StartTaskTimer();
        RefreshHud("Yeşil kelime içeren kağıtları çöpe fırlat.");
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
        EnsureUi();
        if (taskTimer != null)
        {
            taskTimer.StopTimer();
        }

        TaskAssignmentSession.MarkTaskCompleted(TaskKind);
        resultPanel.SetActive(true);
        int accuracy = TaskAssignmentSession.CalculateAccuracyPercent(correctPapersThrown, mistakes);
        resultLabel.text = $"Sonuç\nPuan: {score}\nHata: {mistakes}\n{TaskAssignmentSession.BuildAccuracyLine(TaskKind, accuracy)}";
        statusLabel.text = "Mini oyun tamamlandı.";
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

        EnsureUi();
        TaskAssignmentSession.MarkTaskFailed(TaskKind);
        resultPanel.SetActive(true);
        int accuracy = TaskAssignmentSession.CalculateAccuracyPercent(correctPapersThrown, mistakes);
        resultLabel.text = $"Süre Bitti\nPuan: {score}\nHata: {mistakes}\n{TaskAssignmentSession.BuildAccuracyLine(TaskKind, accuracy)}\nGörev başarısız.";
        RefreshHud("Süre bitti. Görev başarısız.");
    }

    private void HandleDeskObjectClicked(ClickableDeskObject clickedObject)
    {
        if (OfficeMiniGameUi.MatchesClickedObject(clickedObject, trashObject, "trash", "cop"))
        {
            StartMiniGame();
        }
    }

    private void ResetGame()
    {
        score = 0;
        mistakes = 0;
        correctPaperTarget = 0;
        correctPapersThrown = 0;
        processedPapers.Clear();

        if (paperSpawnParent != null)
        {
            OfficeMiniGameUi.ClearChildren(paperSpawnParent);
        }
    }

    private void SpawnPapers()
    {
        for (int i = 0; i < paperCount; i++)
        {
            bool shouldThrow = i % 2 == 0;
            if (shouldThrow)
            {
                correctPaperTarget++;
            }

            GameObject paperObject = CreatePaperObject(i + 1, shouldThrow);
            RectTransform paperRect = paperObject.GetComponent<RectTransform>();
            paperRect.anchoredPosition = new Vector2(Random.Range(spawnAreaMin.x, spawnAreaMax.x), Random.Range(spawnAreaMin.y, spawnAreaMax.y));
            paperRect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-8f, 8f));

            DraggableItem draggableItem = paperObject.GetComponent<DraggableItem>();
            if (draggableItem == null)
            {
                draggableItem = paperObject.AddComponent<DraggableItem>();
            }

            draggableItem.Configure("paper", shouldThrow, true);
        }
    }

    private GameObject CreatePaperObject(int index, bool shouldThrow)
    {
        GameObject paperObject;
        if (paperPrefab != null)
        {
            paperObject = Instantiate(paperPrefab, paperSpawnParent);
        }
        else
        {
            paperObject = OfficeMiniGameUi.CreateImage($"Paper {index}", paperSpawnParent, new Color(0.98f, 0.96f, 0.9f, 1f));
            RectTransform rect = paperObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(170f, 230f);
        }

        TextMeshProUGUI label = paperObject.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label == null)
        {
            label = OfficeMiniGameUi.CreateLabel("PaperText", paperObject.transform, string.Empty, 18f, new Color(0.12f, 0.1f, 0.08f, 1f));
            OfficeMiniGameUi.Stretch(label.GetComponent<RectTransform>(), new Vector2(14f, 14f), new Vector2(-14f, -14f));
        }

        label.richText = true;
        label.alignment = TextAlignmentOptions.Center;
        label.fontStyle = FontStyles.Bold;
        label.text = BuildPaperText(shouldThrow, index);
        return paperObject;
    }

    private string BuildPaperText(bool shouldThrow, int index)
    {
        string[] greenWords = { "İPTAL", "ÇÖP", "ESKİ", "TASLAK", "SİL" };
        string[] redWords = { "SAKLA", "ÖNEMLİ", "İMZA", "ARŞİV", "KAYIT" };
        string word = shouldThrow
            ? $"<color=#2FA84F>{greenWords[index % greenWords.Length]}</color>"
            : $"<color=#D83B35>{redWords[index % redWords.Length]}</color>";

        return $"A4 NOT\n\n{word}\n\nKısa ofis yazısı\nkontrol edildi";
    }

    private void HandleTrashDrop(DraggableItem item, DropZone zone, bool accepted)
    {
        if (item == null || processedPapers.Contains(item))
        {
            return;
        }

        processedPapers.Add(item);

        if (item.IsCorrectItem)
        {
            score += 10;
            correctPapersThrown++;
            RefreshHud("Doğru kağıt çöpe girdi.");
        }
        else
        {
            mistakes++;
            RefreshHud("Kırmızı kelimeli kağıt atıldı. Hata yazıldı.");
        }

        Destroy(item.gameObject);

        if (correctPapersThrown >= correctPaperTarget)
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
        if (panelRoot != null && paperSpawnParent != null && trashDropZone != null)
        {
            trashDropZone.ItemEvaluated -= HandleTrashDrop;
            trashDropZone.Configure("trash", "paper", true);
            trashDropZone.ItemEvaluated += HandleTrashDrop;
            return;
        }

        Canvas canvas = OfficeMiniGameUi.CreateOverlayCanvas("Trash Mini Game Canvas", transform, 1010);
        panelRoot = OfficeMiniGameUi.CreateImage("Trash Mini Game Panel", canvas.transform, new Color(0f, 0f, 0f, 0.58f));
        OfficeMiniGameUi.Stretch(panelRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        GameObject desk = OfficeMiniGameUi.CreateImage("Desk Area", panelRoot.transform, new Color(0.56f, 0.45f, 0.34f, 1f));
        RectTransform deskRect = desk.GetComponent<RectTransform>();
        deskRect.sizeDelta = new Vector2(1280f, 760f);
        deskRect.anchoredPosition = Vector2.zero;

        paperSpawnParent = new GameObject("Paper Spawn Parent", typeof(RectTransform)).GetComponent<RectTransform>();
        paperSpawnParent.transform.SetParent(desk.transform, false);
        OfficeMiniGameUi.Stretch(paperSpawnParent, Vector2.zero, Vector2.zero);

        GameObject trash = OfficeMiniGameUi.CreateImage("Trash Drop Zone", desk.transform, new Color(0.12f, 0.13f, 0.14f, 0.92f));
        RectTransform trashRect = trash.GetComponent<RectTransform>();
        trashRect.anchorMin = new Vector2(1f, 0f);
        trashRect.anchorMax = new Vector2(1f, 0f);
        trashRect.sizeDelta = new Vector2(210f, 250f);
        trashRect.anchoredPosition = new Vector2(-165f, 145f);
        trashDropZone = trash.AddComponent<DropZone>();
        trashDropZone.Configure("trash", "paper", true);
        trashDropZone.ItemEvaluated += HandleTrashDrop;

        TextMeshProUGUI trashLabel = OfficeMiniGameUi.CreateLabel("TrashLabel", trash.transform, "ÇÖP\nKUTUSU", 24f, Color.white);
        OfficeMiniGameUi.Stretch(trashLabel.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        scoreLabel = OfficeMiniGameUi.CreateLabel("Score", desk.transform, "Puan: 0", 24f, Color.white);
        RectTransform scoreRect = scoreLabel.GetComponent<RectTransform>();
        scoreRect.anchorMin = new Vector2(0f, 1f);
        scoreRect.anchorMax = new Vector2(0f, 1f);
        scoreRect.sizeDelta = new Vector2(190f, 42f);
        scoreRect.anchoredPosition = new Vector2(120f, -40f);

        mistakeLabel = OfficeMiniGameUi.CreateLabel("Mistakes", desk.transform, "Hata: 0", 24f, Color.white);
        RectTransform mistakeRect = mistakeLabel.GetComponent<RectTransform>();
        mistakeRect.anchorMin = new Vector2(0f, 1f);
        mistakeRect.anchorMax = new Vector2(0f, 1f);
        mistakeRect.sizeDelta = new Vector2(190f, 42f);
        mistakeRect.anchoredPosition = new Vector2(320f, -40f);

        statusLabel = OfficeMiniGameUi.CreateLabel("Status", desk.transform, string.Empty, 22f, Color.white);
        RectTransform statusRect = statusLabel.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0f, 0f);
        statusRect.anchorMax = new Vector2(1f, 0f);
        statusRect.offsetMin = new Vector2(44f, 32f);
        statusRect.offsetMax = new Vector2(-320f, 80f);
        statusLabel.alignment = TextAlignmentOptions.Left;

        Button finishButton = OfficeMiniGameUi.CreateButton("Finish", desk.transform, "BİTİR", new Vector2(150f, 54f), new Color(0.22f, 0.42f, 0.55f, 1f), FinishMiniGame);
        RectTransform finishRect = finishButton.GetComponent<RectTransform>();
        finishRect.anchorMin = new Vector2(1f, 0f);
        finishRect.anchorMax = new Vector2(1f, 0f);
        finishRect.anchoredPosition = new Vector2(-120f, 58f);

        CreateResultPanel(desk.transform);
        CreateCloseButton(panelRoot.transform);
    }

    private void CreateResultPanel(Transform parent)
    {
        resultPanel = OfficeMiniGameUi.CreateImage("Result Panel", parent, new Color(0.95f, 0.93f, 0.86f, 0.97f));
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
        Button closeButton = OfficeMiniGameUi.CreateButton("Close Trash Game", parent, "X", new Vector2(54f, 54f), new Color(0.08f, 0.08f, 0.09f, 0.95f), CloseMiniGame);
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.5f, 0.5f);
        closeRect.anchorMax = new Vector2(0.5f, 0.5f);
        closeRect.anchoredPosition = new Vector2(675f, 420f);
    }

    private enum TimeoutBehavior
    {
        FailAndShowResult,
        FinishMiniGame,
    }
}
