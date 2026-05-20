using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public sealed class Game1ClickableAreaInstaller : MonoBehaviour
{
    private const string RootName = "_ClickableDeskObjects";
    private const string BackgroundObjectName = "ChatGPT Image 20 May 2026 04_44_35_0";
    private const float SourceWidth = 1536f;
    private const float SourceHeight = 1024f;

    private static readonly AreaDefinition[] Areas =
    {
        Area("Saat", "Saat", 982f, 19f, 1146f, 172f, 20),
        Area("TamamlananlarDosyasi", "Tamamlananlar Dosyasi", 1040f, 474f, 1268f, 725f, 20),
        Area("MusteriDosyalari", "Musteri Dosyalari", 414f, 455f, 615f, 704f, 20),
        Area("Gelenler", "Gelenler", 126f, 464f, 377f, 741f, 20),
        Area("Takvim", "Takvim", 671f, 231f, 785f, 360f, 20),
        Area("Etiketler", "Etiketler", 506f, 876f, 594f, 931f, 35),
        Area("UsbEklentileri", "USB Eklentileri", 604f, 873f, 690f, 932f, 35),
        Area("UsbEklentileri", "USB Eklentileri Sag Cekmece", 978f, 768f, 1321f, 955f, 28),
        Area("Kaseler", "Kaseler", 529f, 765f, 805f, 874f, 28),
        Area("Kaseler", "Kaseler Etiketi", 696f, 874f, 799f, 932f, 35),
        Area("ZimbaTeliVeKlips", "Zimba Teli ve Klips", 209f, 765f, 415f, 884f, 28),
        Area("ZimbaTeliVeKlips", "Zimba Teli ve Klips Etiketi", 810f, 874f, 936f, 932f, 35),
        Area("Yazici", "Yazici", 1292f, 271f, 1485f, 604f, 20),
        Area("Telefon", "Telefon", 1106f, 178f, 1294f, 390f, 20),
        Area("GunlukHedefler", "Gunluk Hedefler", 475f, 58f, 640f, 196f, 20),
        Area("Toplanti", "Toplanti", 750f, 133f, 823f, 185f, 30),
        Area("Cop", "Cop", 1399f, 769f, 1530f, 1015f, 20),
        Area("Cekmeceler", "Sol Cekmece", 54f, 940f, 426f, 1021f, 5),
        Area("Cekmeceler", "Orta Cekmece", 453f, 940f, 970f, 1021f, 5),
        Area("Cekmeceler", "Sag Cekmece", 940f, 940f, 1382f, 1021f, 5),
        Area("Cekmeceler", "Sag Dolap Cekmeceleri", 1344f, 656f, 1501f, 850f, 5),
        Area("Notlar", "Notlar", 845f, 546f, 957f, 702f, 20),
        Area("Bitkiler", "Sol Ust Bitki", 40f, 138f, 147f, 292f, 20),
        Area("Bitkiler", "Sol Alt Bitki", 0f, 286f, 83f, 454f, 20),
        Area("Bitkiler", "Sag Kucuk Bitki", 1235f, 378f, 1307f, 500f, 20),
        Area("Bitkiler", "Sag Buyuk Bitki", 1307f, 52f, 1523f, 328f, 20),
        Area("TarihKasesi", "Tarih Kasesi", 857f, 443f, 950f, 532f, 30),
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void RegisterSceneLoadedHook()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallForInitialScene()
    {
        InstallForScene(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InstallForScene(scene);
    }

    private static void InstallForScene(Scene scene)
    {
        if (!IsLevelScene(scene.name) || GameObject.Find(RootName) != null)
        {
            return;
        }

        SpriteRenderer background = FindBackgroundRenderer();
        if (background == null)
        {
            Debug.LogWarning("Clickable desk objects could not be installed because the background sprite was not found.");
            return;
        }

        EnsurePhysicsRaycaster();

        GameObject root = new GameObject(RootName);
        Game1ClickableAreaInstaller installer = root.AddComponent<Game1ClickableAreaInstaller>();
        installer.CreateAreas(background);
    }

    private static SpriteRenderer FindBackgroundRenderer()
    {
        GameObject background = GameObject.Find(BackgroundObjectName);
        if (background != null && background.TryGetComponent(out SpriteRenderer renderer))
        {
            return renderer;
        }

        return Object.FindAnyObjectByType<SpriteRenderer>();
    }

    private static bool IsLevelScene(string sceneName)
    {
        LevelDefinition level = LevelDatabase.Load().GetLevelBySceneName(sceneName);
        return level != null;
    }

    private static void EnsurePhysicsRaycaster()
    {
        Camera camera = Camera.main;
        if (camera != null && camera.GetComponent<Physics2DRaycaster>() == null)
        {
            camera.gameObject.AddComponent<Physics2DRaycaster>();
        }
    }

    private void CreateAreas(SpriteRenderer background)
    {
        Bounds bounds = background.bounds;

        foreach (AreaDefinition area in Areas)
        {
            GameObject clickArea = new GameObject(area.DisplayName);
            clickArea.transform.SetParent(transform, false);
            clickArea.transform.position = ImagePointToWorld(bounds, area.Center, area.Priority);

            BoxCollider2D collider = clickArea.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = ImageSizeToWorld(bounds, area.Size);

            ClickableDeskObject clickable = clickArea.AddComponent<ClickableDeskObject>();
            clickable.Initialize(area.Id, area.DisplayName);
        }
    }

    private static Vector3 ImagePointToWorld(Bounds bounds, Vector2 point, int priority)
    {
        float normalizedX = point.x / SourceWidth;
        float normalizedY = 1f - (point.y / SourceHeight);

        return new Vector3(
            bounds.min.x + normalizedX * bounds.size.x,
            bounds.min.y + normalizedY * bounds.size.y,
            -0.05f - priority * 0.01f);
    }

    private static Vector2 ImageSizeToWorld(Bounds bounds, Vector2 size)
    {
        return new Vector2(
            size.x / SourceWidth * bounds.size.x,
            size.y / SourceHeight * bounds.size.y);
    }

    private static AreaDefinition Area(string id, string displayName, float left, float top, float right, float bottom, int priority)
    {
        return new AreaDefinition(id, displayName, left, top, right, bottom, priority);
    }

    private readonly struct AreaDefinition
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly Vector2 Center;
        public readonly Vector2 Size;
        public readonly int Priority;

        public AreaDefinition(string id, string displayName, float left, float top, float right, float bottom, int priority)
        {
            Id = id;
            DisplayName = displayName;
            Center = new Vector2((left + right) * 0.5f, (top + bottom) * 0.5f);
            Size = new Vector2(right - left, bottom - top);
            Priority = priority;
        }
    }
}
