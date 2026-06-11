using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Globalization;

public sealed class TaskNotesGuideController : MonoBehaviour
{
    private const string ControllerName = "_TaskNotesGuideController";
    private const string NotebookPaperResourcePath = "NotebookPaper";
    private static readonly CultureInfo TurkishCulture = new CultureInfo("tr-TR");

    private static readonly GuideEntry[] GuideEntries =
    {
        new GuideEntry(
            "Evrakları Ayırma",
            "Gelen evrakı oku ve işin aciliyetini belirle.\n\nMetindeki tarih, teslimat, onay ve bekleme ipuçlarına göre evrakı doğru rafa ayır."),
        new GuideEntry(
            "Mailleri Cevaplama",
            "Gelen mailleri tek tek incele.\n\nHer mail için uygun saat seç ve aynı saate iki farklı mail yerleştirmemeye dikkat et."),
        new GuideEntry(
            "Belgeleri Kaşeleme",
            "Belge metnindeki kişi, kurum veya şirket ipucunu bul.\n\nSağ taraftaki kaşelerden belgeye ait olanı seç. Seçtiğin kaşe belgeye basılır."),
        new GuideEntry(
            "Müşteri Dosyalarını Sıralama",
            "Müşteri dosyalarını alfabetik sıraya koy.\n\nSeçili dosyayı yukarı veya aşağı taşı ve liste doğru sıraya geldiğinde tamamla."),
        new GuideEntry(
            "Telefon Cevaplayıp Not Alma",
            "Telefonu aç, konuşmayı dinle ve istenen bilgileri not al.\n\nİsim, departman ve telefon numarasını doğru alanlara yaz. Gerekirse tekrar dinle."),
        new GuideEntry(
            "Dosya Tamamlama",
            "Belgeyi oku ve hangi iş türüne ait olduğunu anla.\n\nMobilya, kırtasiye, sigorta veya ajans ipuçlarına göre belgeyi doğru dosyaya ekle."),
        new GuideEntry(
            "Takvim Düzenleme",
            "Planlama notundaki gün ve saat bilgisini dikkatlice oku.\n\nTakvimden doğru günü ve doğru saati seçip planı ekle."),
        new GuideEntry(
            "Sözleşme İmzalama",
            "İmza kutusundaki soluk rehberi takip et.\n\nÇizgin rehbere yeterince yakın olursa imza kabul edilir."),
    };

    private GameObject panelRoot;
    private TextMeshProUGUI titleLabel;
    private TextMeshProUGUI bodyLabel;
    private TextMeshProUGUI indexLabel;
    private int selectedIndex;

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
        if (clickedObject == null || !IsNotesObject(clickedObject))
        {
            return;
        }

        EnsureController().Open();
    }

    private static TaskNotesGuideController EnsureController()
    {
        TaskNotesGuideController controller = FindAnyObjectByType<TaskNotesGuideController>();
        if (controller != null)
        {
            return controller;
        }

        GameObject controllerObject = new GameObject(ControllerName);
        return controllerObject.AddComponent<TaskNotesGuideController>();
    }

    private static bool IsNotesObject(ClickableDeskObject clickedObject)
    {
        return OfficeMiniGameUi.MatchesClickedObject(clickedObject, null, "notlar", "not", "notes", "sticky");
    }

    private void Awake()
    {
        BuildUi();
    }

    private void Open()
    {
        selectedIndex = 0;
        panelRoot.SetActive(true);
        ShowEntry(selectedIndex);
    }

    private void Close()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private void ShowEntry(int index)
    {
        selectedIndex = Mathf.Clamp(index, 0, GuideEntries.Length - 1);
        GuideEntry entry = GuideEntries[selectedIndex];
        titleLabel.text = entry.Title;
        bodyLabel.text = entry.Body;
        indexLabel.text = $"{selectedIndex + 1}/{GuideEntries.Length}";
    }

    private void BuildUi()
    {
        if (panelRoot != null)
        {
            return;
        }

        Canvas canvas = OfficeMiniGameUi.CreateOverlayCanvas("Task Notes Guide Canvas", transform, 1014);
        panelRoot = OfficeMiniGameUi.CreateImage("Task Notes Guide Panel", canvas.transform, new Color(0f, 0f, 0f, 0.55f));
        OfficeMiniGameUi.Stretch(panelRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        GameObject board = OfficeMiniGameUi.CreateImage("Notes Board", panelRoot.transform, new Color(0.45f, 0.31f, 0.18f, 0.96f));
        RectTransform boardRect = board.GetComponent<RectTransform>();
        boardRect.sizeDelta = new Vector2(1180f, 760f);
        boardRect.anchoredPosition = Vector2.zero;

        Shadow boardShadow = board.AddComponent<Shadow>();
        boardShadow.effectColor = new Color(0f, 0f, 0f, 0.32f);
        boardShadow.effectDistance = new Vector2(8f, -8f);

        TextMeshProUGUI header = OfficeMiniGameUi.CreateLabel("Header", board.transform, "GÖREV NOTLARI", 30f, new Color(1f, 0.9f, 0.62f, 1f));
        RectTransform headerRect = header.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(0f, 1f);
        headerRect.sizeDelta = new Vector2(360f, 56f);
        headerRect.anchoredPosition = new Vector2(222f, -36f);
        header.alignment = TextAlignmentOptions.Left;

        CreateTabs(board.transform);
        CreateGuidePaper(board.transform);
        CreateCloseButton(panelRoot.transform);

        panelRoot.SetActive(false);
    }

    private void CreateTabs(Transform board)
    {
        RectTransform tabRoot = new GameObject("Guide Tabs", typeof(RectTransform)).GetComponent<RectTransform>();
        tabRoot.transform.SetParent(board, false);
        tabRoot.anchorMin = new Vector2(0f, 0f);
        tabRoot.anchorMax = new Vector2(0f, 1f);
        tabRoot.offsetMin = new Vector2(42f, 72f);
        tabRoot.offsetMax = new Vector2(306f, -122f);

        for (int i = 0; i < GuideEntries.Length; i++)
        {
            int capturedIndex = i;
            Color noteColor = i % 3 == 0
                ? new Color(0.98f, 0.84f, 0.35f, 1f)
                : i % 3 == 1
                    ? new Color(0.97f, 0.72f, 0.38f, 1f)
                    : new Color(0.9f, 0.78f, 0.48f, 1f);

            Button tab = OfficeMiniGameUi.CreateButton($"Guide {GuideEntries[i].Title}", tabRoot, GuideEntries[i].Title.ToUpper(TurkishCulture), new Vector2(210f, 56f), noteColor, () => ShowEntry(capturedIndex));
            RectTransform tabRect = tab.GetComponent<RectTransform>();
            tabRect.anchorMin = new Vector2(0f, 1f);
            tabRect.anchorMax = new Vector2(0f, 1f);
            tabRect.anchoredPosition = new Vector2(116f + (i % 2) * 12f, -36f - i * 72f);
            tabRect.localRotation = Quaternion.Euler(0f, 0f, i % 2 == 0 ? -2.5f : 2f);

            Shadow tabShadow = tab.gameObject.AddComponent<Shadow>();
            tabShadow.effectColor = new Color(0f, 0f, 0f, 0.22f);
            tabShadow.effectDistance = new Vector2(4f, -4f);

            TextMeshProUGUI label = tab.GetComponentInChildren<TextMeshProUGUI>();
            label.fontSize = 18f;
            label.color = new Color(0.18f, 0.11f, 0.05f, 1f);
        }
    }

    private void CreateGuidePaper(Transform board)
    {
        GameObject paper = OfficeMiniGameUi.CreateImage("Guide Paper", board, Color.white);
        Image paperImage = paper.GetComponent<Image>();
        Sprite notebookPaperSprite = Resources.Load<Sprite>(NotebookPaperResourcePath);
        if (notebookPaperSprite != null)
        {
            paperImage.sprite = notebookPaperSprite;
            paperImage.preserveAspect = false;
        }

        RectTransform paperRect = paper.GetComponent<RectTransform>();
        paperRect.anchorMin = new Vector2(1f, 0.5f);
        paperRect.anchorMax = new Vector2(1f, 0.5f);
        paperRect.sizeDelta = new Vector2(540f, 640f);
        paperRect.anchoredPosition = new Vector2(-430f, -2f);
        paperRect.localRotation = Quaternion.Euler(0f, 0f, -1.5f);

        Shadow paperShadow = paper.AddComponent<Shadow>();
        paperShadow.effectColor = new Color(0f, 0f, 0f, 0.28f);
        paperShadow.effectDistance = new Vector2(7f, -7f);

        titleLabel = OfficeMiniGameUi.CreateLabel("GuideTitle", paper.transform, string.Empty, 31f, new Color(0.08f, 0.06f, 0.04f, 1f));
        RectTransform titleRect = titleLabel.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(118f, -106f);
        titleRect.offsetMax = new Vector2(-44f, -52f);
        titleLabel.alignment = TextAlignmentOptions.Left;
        titleLabel.enableAutoSizing = true;
        titleLabel.fontStyle = FontStyles.Bold;
        titleLabel.fontSizeMin = 23f;
        titleLabel.fontSizeMax = 31f;

        bodyLabel = OfficeMiniGameUi.CreateLabel("GuideBody", paper.transform, string.Empty, 22f, new Color(0.09f, 0.07f, 0.05f, 1f));
        RectTransform bodyRect = bodyLabel.GetComponent<RectTransform>();
        bodyRect.anchorMin = Vector2.zero;
        bodyRect.anchorMax = Vector2.one;
        bodyRect.offsetMin = new Vector2(118f, 122f);
        bodyRect.offsetMax = new Vector2(-44f, -122f);
        bodyLabel.alignment = TextAlignmentOptions.TopLeft;
        bodyLabel.fontStyle = FontStyles.Bold;
        bodyLabel.textWrappingMode = TextWrappingModes.Normal;
        bodyLabel.enableAutoSizing = true;
        bodyLabel.fontSizeMin = 17f;
        bodyLabel.fontSizeMax = 22f;
        bodyLabel.overflowMode = TextOverflowModes.Truncate;

        indexLabel = OfficeMiniGameUi.CreateLabel("GuideIndex", paper.transform, "1/6", 20f, new Color(0.32f, 0.22f, 0.12f, 1f));
        RectTransform indexRect = indexLabel.GetComponent<RectTransform>();
        indexRect.anchorMin = new Vector2(1f, 0f);
        indexRect.anchorMax = new Vector2(1f, 0f);
        indexRect.sizeDelta = new Vector2(90f, 38f);
        indexRect.anchoredPosition = new Vector2(-52f, 50f);
    }

    private void CreateCloseButton(Transform parent)
    {
        Button closeButton = OfficeMiniGameUi.CreateButton("Close Notes Guide", parent, "X", new Vector2(54f, 54f), new Color(0.08f, 0.08f, 0.09f, 0.95f), Close);
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.5f, 0.5f);
        closeRect.anchorMax = new Vector2(0.5f, 0.5f);
        closeRect.anchoredPosition = new Vector2(610f, 410f);
    }

    private readonly struct GuideEntry
    {
        public readonly string Title;
        public readonly string Body;

        public GuideEntry(string title, string body)
        {
            Title = title;
            Body = body;
        }
    }
}
