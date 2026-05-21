using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class IncomingDocumentsSortingController : MonoBehaviour
{
    public static event Action<int> MistakePercentReported;

    private const string ControllerName = "_IncomingDocumentsSortingController";

    private static readonly IncomingDocument[] Documents =
    {
        new IncomingDocument(
            "Tedarikci odeme bildirimi",
            "Muhasebe ekibi, tedarikci faturasinin bugun mesai bitmeden onaylanmasini bekliyor.\nGecikirse sevkiyat sabah cikmayabilir.\nDosyada eksik imza yok, sadece yonetici onayi gerekiyor.\nBu evrak kapanmadan satin alma islemi ilerlemeyecek.",
            DocumentPriority.Urgent),
        new IncomingDocument(
            "Arsiv kopya talebi",
            "Gecen ay kapatilan dosyanin bir kopyasi arsive eklenmek uzere istendi.\nTalep sahibi hafta icinde donus yapilmasinin yeterli olacagini belirtti.\nIslem tamamlanmasa da bugunku operasyonu etkilemiyor.\nBelge sadece kayit duzeni icin isteniyor.",
            DocumentPriority.CanWait),
        new IncomingDocument(
            "Musteri bilgi guncelleme",
            "Musteri adres bilgisinde degisiklik oldugunu bildirdi.\nSiparis bugun cikmayacak, ancak kayitlarin dogru kalmasi gerekiyor.\nDosya standart kontrol akisi ile guncellenebilir.\nBir sonraki islemden once sisteme islenmesi yeterli.",
            DocumentPriority.Normal),
        new IncomingDocument(
            "Kurye teslim tutanagi",
            "Kurye, teslim tutanaginda alici imzasinin eksik oldugunu iletti.\nPaket aliciya ulasmis gorunuyor fakat teyit kaydi kapanmadi.\nGunun kapanis raporu bu bilgiye bagli.\nTutanak tamamlanmadan teslimat kesinlestirilemiyor.",
            DocumentPriority.Urgent),
        new IncomingDocument(
            "Toplanti notlari duzenleme",
            "Dunku toplantinin notlari temiz bir dosyaya aktarilacak.\nEkip notlara yarin ogleden sonra bakacak.\nBugun yalnizca taslak halinin saklanmasi yeterli.\nIcerik karari etkileyen bir onay beklemiyor.",
            DocumentPriority.CanWait),
        new IncomingDocument(
            "Personel izin formu",
            "Personel gelecek hafta icin izin formu birakti.\nFormda tarih ve imza bilgileri tam.\nPlanlama listesine islenmesi gerekiyor.\nAyni gun yanit zorunlu degil, fakat rutin akista bekletilmemeli.",
            DocumentPriority.Normal),
        new IncomingDocument(
            "Sozlesme ek protokol",
            "Musteri, ek protokolun bugunku gorusme baslamadan dosyaya eklenmesini istedi.\nGorusme saati yaklasiyor ve eski metin kullanilirsa yanlis madde okunabilir.\nBelge hazir, yalnizca dogru klasore alinacak.\nIlgili ekip bu dosyayi bekliyor.",
            DocumentPriority.Urgent),
        new IncomingDocument(
            "Katalog baski onayi",
            "Pazarlama ekibi yeni katalog taslagini kontrol icin gonderdi.\nBaski tarihi henuz kesinlesmedi.\nHafta sonuna kadar geri donus yapilmasi yeterli gorunuyor.\nBugunku musteri dosyalarina etkisi yok.",
            DocumentPriority.CanWait),
        new IncomingDocument(
            "Depo stok duzeltmesi",
            "Depo kaydinda iki urun miktari farkli gorunuyor.\nFark su an sevkiyati durdurmuyor, ancak stok raporunda duzeltilmeli.\nKontrol sonrasi sisteme not dusulmesi bekleniyor.\nGunun normal is takibine alinabilir.",
            DocumentPriority.Normal),
        new IncomingDocument(
            "Iade itiraz formu",
            "Musteri iade itirazinda son yanit saatinin bugun oldugunu belirtti.\nForm incelenmezse otomatik kabul sureci baslayabilir.\nEk belgeler tam ve karar icin hazir.\nOncelik verilmesi gereken bir cevap bekliyor.",
            DocumentPriority.Urgent),
        new IncomingDocument(
            "Egitim katilim listesi",
            "Gelecek ay yapilacak egitim icin katilim listesi geldi.\nListe henuz kesinlesmeyecek, yalnizca ilk taslak olarak saklanacak.\nHafta icinde isim ekleme cikarma yapilabilir.\nSimdilik beklemede kalmasi sorun yaratmaz.",
            DocumentPriority.CanWait),
        new IncomingDocument(
            "Bakim servis raporu",
            "Ofis yazicisi icin servis raporu geldi.\nCihaz calisiyor, rapor sadece bakim kaydina eklenecek.\nTeknik ekip formun dosyalanmasini istiyor.\nGunluk akista normal siraya alinabilir.",
            DocumentPriority.Normal),
    };

    private GameObject panelRoot;
    private TextMeshProUGUI titleLabel;
    private TextMeshProUGUI bodyLabel;
    private TextMeshProUGUI progressLabel;
    private TextMeshProUGUI statusLabel;
    private TextMeshProUGUI mistakeLabel;
    private int currentDocumentIndex;
    private int mistakeCount;
    private int completedCount;

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
        if (clickedObject == null || !IsIncomingObject(clickedObject))
        {
            return;
        }

        EnsureController().Open();
    }

    private static IncomingDocumentsSortingController EnsureController()
    {
        IncomingDocumentsSortingController controller = FindAnyObjectByType<IncomingDocumentsSortingController>();
        if (controller != null)
        {
            return controller;
        }

        GameObject controllerObject = new GameObject(ControllerName);
        return controllerObject.AddComponent<IncomingDocumentsSortingController>();
    }

    private static bool IsIncomingObject(ClickableDeskObject clickedObject)
    {
        string text = $"{clickedObject.ObjectId} {clickedObject.DisplayName} {clickedObject.gameObject.name}";
        text = NormalizeTurkish(text);
        return text.Contains("gelen");
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
        BuildUi();
    }

    private void Open()
    {
        currentDocumentIndex = 0;
        mistakeCount = 0;
        completedCount = 0;
        panelRoot.SetActive(true);
        ShowCurrentDocument("Evraki oku ve dogru rafa ayir.");
    }

    private void SelectPriority(DocumentPriority selectedPriority)
    {
        IncomingDocument document = Documents[currentDocumentIndex];

        if (selectedPriority != document.Priority)
        {
            mistakeCount++;
            UpdateMistakeLabel();
            statusLabel.text = "Bu sinif uygun degil. Metindeki ipuclarini tekrar oku.";
            return;
        }

        completedCount++;

        if (currentDocumentIndex >= Documents.Length - 1)
        {
            int mistakePercent = Mathf.RoundToInt(mistakeCount / (float)(mistakeCount + Documents.Length) * 100f);
            MistakePercentReported?.Invoke(mistakePercent);
            titleLabel.text = "Evraklar tamamlandi";
            bodyLabel.text = "Tum gelen evraklar incelendi ve siniflandirildi.";
            progressLabel.text = $"{Documents.Length}/{Documents.Length}";
            statusLabel.text = mistakeCount == 0 ? "Kusursuz siniflandirma." : $"Gorev bitti. Hata orani: %{mistakePercent}";
            mistakeLabel.text = $"Hata: %{mistakePercent}";
            return;
        }

        currentDocumentIndex++;
        ShowCurrentDocument("Dogru. Siradaki evraki incele.");
    }

    private void ShowCurrentDocument(string statusText)
    {
        IncomingDocument document = Documents[currentDocumentIndex];
        titleLabel.text = document.Title;
        bodyLabel.text = document.Body;
        progressLabel.text = $"{currentDocumentIndex + 1}/{Documents.Length}";
        statusLabel.text = statusText;
        UpdateMistakeLabel();
    }

    private void UpdateMistakeLabel()
    {
        int attempts = completedCount + mistakeCount;
        int mistakePercent = attempts > 0 ? Mathf.RoundToInt(mistakeCount / (float)attempts * 100f) : 0;
        mistakeLabel.text = $"Hata: %{mistakePercent}";
    }

    private void BuildUi()
    {
        if (panelRoot != null)
        {
            return;
        }

        EnsureEventSystem();

        GameObject canvasObject = new GameObject("Incoming Documents Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1003;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        panelRoot = CreateUiImage("Incoming Documents Panel", canvasObject.transform, new Color(0f, 0f, 0f, 0.6f));
        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        GameObject desk = CreateUiImage("Incoming Documents Desk", panelRoot.transform, new Color(0.88f, 0.84f, 0.76f, 1f));
        RectTransform deskRect = desk.GetComponent<RectTransform>();
        deskRect.sizeDelta = new Vector2(1180f, 780f);
        deskRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI header = CreateLabel("Header", desk.transform, "GELEN EVRAKLAR", 34f, new Color(0.12f, 0.1f, 0.08f, 1f));
        RectTransform headerRect = header.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.offsetMin = new Vector2(44f, -82f);
        headerRect.offsetMax = new Vector2(-44f, -28f);

        CreateDocumentCard(desk.transform);
        CreatePriorityTrays(desk.transform);
        CreateFooter(desk.transform);
        CreateCloseButton(panelRoot.transform);

        panelRoot.SetActive(false);
    }

    private void CreateDocumentCard(Transform parent)
    {
        GameObject card = CreateUiImage("Document Card", parent, new Color(0.98f, 0.96f, 0.9f, 1f));
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0f, 0f);
        cardRect.anchorMax = new Vector2(0f, 1f);
        cardRect.offsetMin = new Vector2(58f, 150f);
        cardRect.offsetMax = new Vector2(760f, -112f);

        titleLabel = CreateLabel("DocumentTitle", card.transform, string.Empty, 30f, new Color(0.1f, 0.1f, 0.09f, 1f));
        RectTransform titleRect = titleLabel.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(42f, -92f);
        titleRect.offsetMax = new Vector2(-42f, -34f);
        titleLabel.alignment = TextAlignmentOptions.Left;

        bodyLabel = CreateLabel("DocumentBody", card.transform, string.Empty, 23f, new Color(0.12f, 0.11f, 0.1f, 1f));
        RectTransform bodyRect = bodyLabel.GetComponent<RectTransform>();
        bodyRect.anchorMin = Vector2.zero;
        bodyRect.anchorMax = Vector2.one;
        bodyRect.offsetMin = new Vector2(46f, 64f);
        bodyRect.offsetMax = new Vector2(-46f, -124f);
        bodyLabel.alignment = TextAlignmentOptions.TopLeft;
        bodyLabel.fontStyle = FontStyles.Normal;

        progressLabel = CreateLabel("Progress", card.transform, "1/12", 20f, new Color(0.26f, 0.24f, 0.2f, 1f));
        RectTransform progressRect = progressLabel.GetComponent<RectTransform>();
        progressRect.anchorMin = new Vector2(1f, 0f);
        progressRect.anchorMax = new Vector2(1f, 0f);
        progressRect.sizeDelta = new Vector2(120f, 42f);
        progressRect.anchoredPosition = new Vector2(-90f, 40f);
    }

    private void CreatePriorityTrays(Transform parent)
    {
        CreatePriorityButton(parent, "AcilTray", "ACIL", new Vector2(-198f, 160f), new Color(0.72f, 0.24f, 0.2f, 1f), DocumentPriority.Urgent);
        CreatePriorityButton(parent, "NormalTray", "NORMAL", new Vector2(-198f, 40f), new Color(0.24f, 0.45f, 0.68f, 1f), DocumentPriority.Normal);
        CreatePriorityButton(parent, "WaitTray", "BEKLEYEBILIR", new Vector2(-198f, -80f), new Color(0.45f, 0.56f, 0.3f, 1f), DocumentPriority.CanWait);
    }

    private void CreatePriorityButton(Transform parent, string name, string text, Vector2 anchoredPosition, Color color, DocumentPriority priority)
    {
        GameObject buttonObject = CreateUiImage(name, parent, color);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 0.5f);
        buttonRect.anchorMax = new Vector2(1f, 0.5f);
        buttonRect.sizeDelta = new Vector2(300f, 86f);
        buttonRect.anchoredPosition = anchoredPosition;

        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(() => SelectPriority(priority));

        TextMeshProUGUI label = CreateLabel("Label", buttonObject.transform, text, 24f, Color.white);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
    }

    private void CreateFooter(Transform parent)
    {
        statusLabel = CreateLabel("Status", parent, string.Empty, 22f, new Color(0.15f, 0.11f, 0.08f, 1f));
        RectTransform statusRect = statusLabel.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0f, 0f);
        statusRect.anchorMax = new Vector2(1f, 0f);
        statusRect.offsetMin = new Vector2(58f, 62f);
        statusRect.offsetMax = new Vector2(-320f, 118f);
        statusLabel.alignment = TextAlignmentOptions.Left;

        mistakeLabel = CreateLabel("MistakePercent", parent, "Hata: %0", 24f, new Color(0.62f, 0.08f, 0.06f, 1f));
        RectTransform mistakeRect = mistakeLabel.GetComponent<RectTransform>();
        mistakeRect.anchorMin = new Vector2(1f, 0f);
        mistakeRect.anchorMax = new Vector2(1f, 0f);
        mistakeRect.sizeDelta = new Vector2(230f, 58f);
        mistakeRect.anchoredPosition = new Vector2(-180f, 92f);
    }

    private void CreateCloseButton(Transform parent)
    {
        GameObject buttonObject = CreateUiImage("Close Incoming Documents", parent, new Color(0.08f, 0.08f, 0.09f, 0.95f));
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(54f, 54f);
        buttonRect.anchoredPosition = new Vector2(620f, 420f);

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

    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }

    private readonly struct IncomingDocument
    {
        public readonly string Title;
        public readonly string Body;
        public readonly DocumentPriority Priority;

        public IncomingDocument(string title, string body, DocumentPriority priority)
        {
            Title = title;
            Body = body;
            Priority = priority;
        }
    }

    private enum DocumentPriority
    {
        Urgent,
        Normal,
        CanWait,
    }
}
