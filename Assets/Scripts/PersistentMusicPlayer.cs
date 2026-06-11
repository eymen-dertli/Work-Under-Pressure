using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class PersistentMusicPlayer : MonoBehaviour
{
    private const string MainSceneName = "MainScene";
    private const string PlayerObjectName = "[Persistent Music Player]";
    private static readonly string[] MusicResourcePaths =
    {
        "Audio/Ledger Pulse",
        "Audio/Deadline Conveyor",
    };

    [SerializeField, Range(0f, 1f)] private float volume = 0.65f;

    private static PersistentMusicPlayer instance;
    private AudioSource audioSource;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void TryStartForInitialScene()
    {
        TryStartForScene(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryStartForScene(scene);
    }

    private static void TryStartForScene(Scene scene)
    {
        if (scene.name != MainSceneName || instance != null)
        {
            return;
        }

        GameObject player = new GameObject(PlayerObjectName);
        instance = player.AddComponent<PersistentMusicPlayer>();
        DontDestroyOnLoad(player);
        instance.PlayMusic();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void PlayMusic()
    {
        AudioClip clip = LoadMusicClip();
        if (clip == null)
        {
            Debug.LogWarning($"Music clip could not be loaded from Resources/{string.Join(" or Resources/", MusicResourcePaths)}.");
            return;
        }

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = volume;
        audioSource.spatialBlend = 0f;
        AudioManager.RegisterMusicSource(audioSource, volume);
        audioSource.Play();
    }

    private static AudioClip LoadMusicClip()
    {
        foreach (string resourcePath in MusicResourcePaths)
        {
            AudioClip clip = Resources.Load<AudioClip>(resourcePath);
            if (clip != null)
            {
                return clip;
            }
        }

        return null;
    }

    private void OnDestroy()
    {
        if (audioSource != null)
        {
            AudioManager.UnregisterMusicSource(audioSource);
        }
    }
}
