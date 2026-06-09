using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class StampMiniGameManager : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private bool listenToClickableDeskObjects = true;
    [SerializeField] private ClickableDeskObject stampDeskObject;

    [Header("Prefabs")]
    [SerializeField] private GameObject documentPrefab;
    [SerializeField] private GameObject stampPrefab;

    [Header("Game Setup")]
    [SerializeField] private int documentCount = 12;

    [Header("Task Timer")]
    [SerializeField] private TaskTimer taskTimer;
    [SerializeField] private float taskDurationSeconds = 75f;
    [SerializeField] private TimeoutBehavior timeoutBehavior = TimeoutBehavior.FailAndShowResult;

    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private RectTransform documentParent;
    [SerializeField] private RectTransform stampHome;
    [SerializeField] private TextMeshProUGUI correctLabel;
    [SerializeField] private TextMeshProUGUI wrongLabel;
    [SerializeField] private TextMeshProUGUI statusLabel;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultLabel;

    private readonly Dictionary<DropZone, DocumentRuntimeData> documentsByZone = new Dictionary<DropZone, DocumentRuntimeData>();
    private DraggableItem stampItem;
    private bool stampSelected;
    private int correctCount;
    private int wrongCount;
    private int signedDocumentTarget;
    private int signedDocumentsStamped;

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
        foreach (DropZone zone in documentsByZone.Keys)
        {
            if (zone != null)
            {
                zone.ItemEvaluated -= HandleStampDrop;
            }
        }

        if (taskTimer != null)
        {
            taskTimer.TimerExpired -= HandleTaskTimerExpired;
        }
    }

    public void StartMiniGame()
    {
        EnsureUi();
        ResetGame();
        SpawnStamp();
        SpawnDocuments();
        panelRoot.SetActive(true);
        resultPanel.SetActive(false);
        StartTaskTimer();
        RefreshHud("Sadece imzali evraklara kase vur.");
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
        resultLabel.text = $"Sonuc\nDogru: {correctCount}\nYanlis: {wrongCount}";
        RefreshHud("Kase gorevi tamamlandi.");
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
        resultLabel.text = $"Sure Bitti\nDogru: {correctCount}\nYanlis: {wrongCount}\nGorev basarisiz.";
        RefreshHud("Sure bitti. Gorev basarisiz.");
    }

    private void HandleDeskObjectClicked(ClickableDeskObject clickedObject)
    {
        if (OfficeMiniGameUi.MatchesClickedObject(clickedObject, stampDeskObject, "stamp", "kase", "muhur"))
        {
            StartMiniGame();
        }
    }

    private void ResetGame()
    {
        correctCount = 0;
        wrongCount = 0;
        signedDocumentTarget = 0;
        signedDocumentsStamped = 0;
        stampSelected = false;

        foreach (DropZone zone in documentsByZone.Keys)
        {
            if (zone != null)
            {
                zone.ItemEvaluated -= HandleStampDrop;
            }
        }

        documentsByZone.Clear();

        if (documentParent != null)
        {
            OfficeMiniGameUi.ClearChildren(documentParent);
        }

        if (stampHome != null)
        {
            OfficeMiniGameUi.ClearChildren(stampHome);
        }
    }

    private void SpawnStamp()
    {
        GameObject stampObject;
        if (stampPrefab != null)
        {
            stampObject = Instantiate(stampPrefab, stampHome);
        }
        else
        {
            stampObject = OfficeMiniGameUi.CreateImage("Stamp", stampHome, new Color(0.56f, 0.1f, 0.12f, 1f));
            RectTransform stampRect = stampObject.GetComponent<RectTransform>();
            stampRect.sizeDelta = new Vector2(130f, 82f);
            TextMeshProUGUI stampLabel = OfficeMiniGameUi.CreateLabel("StampLabel", stampObject.transform, "KASE", 24f, Color.white);
            OfficeMiniGameUi.Stretch(stampLabel.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        }

        stampItem = stampObject.GetComponent<DraggableItem>();
        if (stampItem == null)
        {
            stampItem = stampObject.AddComponent<DraggableItem>();
        }

        stampItem.Configure("stamp", true, false);

        Button stampButton = stampObject.GetComponent<Button>();
        if (stampButton == null)
        {
            stampButton = stampObject.AddComponent<Button>();
        }

        stampButton.onClick.RemoveAllListeners();
        stampButton.onClick.AddListener(ToggleStampSelected);
    }

    private void SpawnDocuments()
    {
        for (int i = 0; i < documentCount; i++)
        {
            bool hasSignature = i % 3 != 0;
            if (hasSignature)
            {
                signedDocumentTarget++;
            }

            GameObject documentObject = CreateDocumentObject(i + 1, hasSignature);
            DropZone dropZone = documentObject.GetComponent<DropZone>();
            if (dropZone == null)
            {
                dropZone = documentObject.AddComponent<DropZone>();
            }

            dropZone.Configure($"document_{i + 1}", "stamp", false);
            dropZone.ItemEvaluated += HandleStampDrop;

            TextMeshProUGUI stampMark = OfficeMiniGameUi.CreateLabel("StampMark", documentObject.transform, "KASELENDI", 16f, new Color(0.56f, 0.1f, 0.12f, 1f));
            RectTransform markRect = stampMark.GetComponent<RectTransform>();
            markRect.anchorMin = new Vector2(0.5f, 0f);
            markRect.anchorMax = new Vector2(0.5f, 0f);
            markRect.sizeDelta = new Vector2(150f, 32f);
            markRect.anchoredPosition = new Vector2(0f, 28f);
            stampMark.gameObject.SetActive(false);

            Button documentButton = documentObject.GetComponent<Button>();
            if (documentButton == null)
            {
                documentButton = documentObject.AddComponent<Button>();
            }

            DropZone capturedZone = dropZone;
            documentButton.onClick.RemoveAllListeners();
            documentButton.onClick.AddListener(() => TryClickStamp(capturedZone));

            documentsByZone.Add(dropZone, new DocumentRuntimeData(hasSignature, stampMark));
        }
    }

    private GameObject CreateDocumentObject(int index, bool hasSignature)
    {
        GameObject documentObject;
        if (documentPrefab != null)
        {
            documentObject = Instantiate(documentPrefab, documentParent);
        }
        else
        {
            documentObject = OfficeMiniGameUi.CreateImage($"Document {index}", documentParent, new Color(0.98f, 0.96f, 0.9f, 1f));
            RectTransform rect = documentObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(210f, 250f);
        }

        TextMeshProUGUI body = documentObject.GetComponentInChildren<TextMeshProUGUI>(true);
        if (body == null)
        {
            body = OfficeMiniGameUi.CreateLabel("DocumentText", documentObject.transform, string.Empty, 15f, new Color(0.12f, 0.1f, 0.08f, 1f));
            OfficeMiniGameUi.Stretch(body.GetComponent<RectTransform>(), new Vector2(14f, 16f), new Vector2(-14f, -56f));
        }

        body.text = $"EVRAK {index:00}\n\nTalep formu\nKontrol notu\nDepartman kaydi";
        body.alignment = TextAlignmentOptions.TopLeft;

        if (hasSignature)
        {
            TextMeshProUGUI signature = OfficeMiniGameUi.CreateLabel("Signature", documentObject.transform, "imza", 18f, new Color(0.08f, 0.08f, 0.08f, 1f));
            RectTransform signatureRect = signature.GetComponent<RectTransform>();
            signatureRect.anchorMin = new Vector2(1f, 0f);
            signatureRect.anchorMax = new Vector2(1f, 0f);
            signatureRect.sizeDelta = new Vector2(74f, 28f);
            signatureRect.anchoredPosition = new Vector2(-54f, 28f);
            signature.fontStyle = FontStyles.Italic;
        }

        return documentObject;
    }

    private void ToggleStampSelected()
    {
        stampSelected = !stampSelected;
        RefreshHud(stampSelected ? "Kase secildi. Bir evraka tikla." : "Kase secimi kapandi.");
    }

    private void TryClickStamp(DropZone zone)
    {
        if (!stampSelected)
        {
            return;
        }

        StampDocument(zone);
        stampSelected = false;
    }

    private void HandleStampDrop(DraggableItem item, DropZone zone, bool accepted)
    {
        if (!accepted)
        {
            return;
        }

        StampDocument(zone);

        if (item != null)
        {
            item.ReturnToHome();
        }
    }

    private void StampDocument(DropZone zone)
    {
        if (zone == null || !documentsByZone.TryGetValue(zone, out DocumentRuntimeData documentData) || documentData.Stamped)
        {
            return;
        }

        documentData.Stamped = true;
        documentData.StampMark.gameObject.SetActive(true);

        if (documentData.HasSignature)
        {
            correctCount++;
            signedDocumentsStamped++;
            RefreshHud("Dogru. Imzali evrak kaselendi.");
        }
        else
        {
            wrongCount++;
            RefreshHud("Imzasiz evraka kase vuruldu. Yanlis.");
        }

        if (signedDocumentsStamped >= signedDocumentTarget)
        {
            FinishMiniGame();
        }
    }

    private void RefreshHud(string statusText)
    {
        if (correctLabel != null)
        {
            correctLabel.text = $"Dogru: {correctCount}";
        }

        if (wrongLabel != null)
        {
            wrongLabel.text = $"Yanlis: {wrongCount}";
        }

        if (statusLabel != null)
        {
            statusLabel.text = statusText;
        }
    }

    private void EnsureUi()
    {
        if (panelRoot != null && documentParent != null && stampHome != null)
        {
            return;
        }

        Canvas canvas = OfficeMiniGameUi.CreateOverlayCanvas("Stamp Mini Game Canvas", transform, 1011);
        panelRoot = OfficeMiniGameUi.CreateImage("Stamp Mini Game Panel", canvas.transform, new Color(0f, 0f, 0f, 0.58f));
        OfficeMiniGameUi.Stretch(panelRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        GameObject board = OfficeMiniGameUi.CreateImage("Stamp Board", panelRoot.transform, new Color(0.78f, 0.72f, 0.62f, 1f));
        RectTransform boardRect = board.GetComponent<RectTransform>();
        boardRect.sizeDelta = new Vector2(1350f, 860f);
        boardRect.anchoredPosition = Vector2.zero;

        documentParent = new GameObject("Document Grid", typeof(RectTransform), typeof(GridLayoutGroup)).GetComponent<RectTransform>();
        documentParent.transform.SetParent(board.transform, false);
        documentParent.anchorMin = new Vector2(0f, 0f);
        documentParent.anchorMax = new Vector2(1f, 1f);
        documentParent.offsetMin = new Vector2(48f, 112f);
        documentParent.offsetMax = new Vector2(-260f, -74f);

        GridLayoutGroup grid = documentParent.GetComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;
        grid.cellSize = new Vector2(220f, 250f);
        grid.spacing = new Vector2(22f, 22f);

        stampHome = OfficeMiniGameUi.CreateImage("Stamp Home", board.transform, new Color(0.34f, 0.22f, 0.18f, 1f)).GetComponent<RectTransform>();
        stampHome.anchorMin = new Vector2(1f, 0.5f);
        stampHome.anchorMax = new Vector2(1f, 0.5f);
        stampHome.sizeDelta = new Vector2(180f, 130f);
        stampHome.anchoredPosition = new Vector2(-130f, 80f);

        correctLabel = OfficeMiniGameUi.CreateLabel("Correct", board.transform, "Dogru: 0", 24f, Color.white);
        RectTransform correctRect = correctLabel.GetComponent<RectTransform>();
        correctRect.anchorMin = new Vector2(0f, 1f);
        correctRect.anchorMax = new Vector2(0f, 1f);
        correctRect.sizeDelta = new Vector2(180f, 40f);
        correctRect.anchoredPosition = new Vector2(135f, -42f);

        wrongLabel = OfficeMiniGameUi.CreateLabel("Wrong", board.transform, "Yanlis: 0", 24f, Color.white);
        RectTransform wrongRect = wrongLabel.GetComponent<RectTransform>();
        wrongRect.anchorMin = new Vector2(0f, 1f);
        wrongRect.anchorMax = new Vector2(0f, 1f);
        wrongRect.sizeDelta = new Vector2(180f, 40f);
        wrongRect.anchoredPosition = new Vector2(330f, -42f);

        statusLabel = OfficeMiniGameUi.CreateLabel("Status", board.transform, string.Empty, 22f, Color.white);
        RectTransform statusRect = statusLabel.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0f, 0f);
        statusRect.anchorMax = new Vector2(1f, 0f);
        statusRect.offsetMin = new Vector2(48f, 32f);
        statusRect.offsetMax = new Vector2(-260f, 84f);
        statusLabel.alignment = TextAlignmentOptions.Left;

        Button finishButton = OfficeMiniGameUi.CreateButton("Finish Stamp", board.transform, "BITIR", new Vector2(150f, 54f), new Color(0.22f, 0.42f, 0.55f, 1f), FinishMiniGame);
        RectTransform finishRect = finishButton.GetComponent<RectTransform>();
        finishRect.anchorMin = new Vector2(1f, 0f);
        finishRect.anchorMax = new Vector2(1f, 0f);
        finishRect.anchoredPosition = new Vector2(-130f, 58f);

        CreateResultPanel(board.transform);
        CreateCloseButton(panelRoot.transform);
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
        closeRect.anchoredPosition = new Vector2(705f, 465f);
    }

    private sealed class DocumentRuntimeData
    {
        public readonly bool HasSignature;
        public readonly TextMeshProUGUI StampMark;
        public bool Stamped;

        public DocumentRuntimeData(bool hasSignature, TextMeshProUGUI stampMark)
        {
            HasSignature = hasSignature;
            StampMark = stampMark;
        }
    }

    private enum TimeoutBehavior
    {
        FailAndShowResult,
        FinishMiniGame,
    }
}
