using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class UiClickSoundPlayer : MonoBehaviour
{
    private const string PlayerObjectName = "[UI Click Sound Player]";
    private const string ClickClipResourcePath = "Audio/Mouse_Click";
    private const float RescanInterval = 0.35f;

    private static UiClickSoundPlayer instance;
    private AudioSource audioSource;
    private AudioClip clickClip;
    private bool missingClipWarned;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForInitialScene()
    {
        EnsureInstance();
    }

    public static void PlayClick()
    {
        EnsureInstance();

        if (instance != null)
        {
            instance.Play();
        }
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureInstance();
        instance.ScanButtons();
    }

    private static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        GameObject player = new GameObject(PlayerObjectName);
        instance = player.AddComponent<UiClickSoundPlayer>();
        DontDestroyOnLoad(player);
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

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        clickClip = Resources.Load<AudioClip>(ClickClipResourcePath);
        StartCoroutine(ScanButtonsLoop());
    }

    private IEnumerator ScanButtonsLoop()
    {
        while (true)
        {
            ScanButtons();
            yield return new WaitForSecondsRealtime(RescanInterval);
        }
    }

    private void ScanButtons()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button button in buttons)
        {
            if (button == null || button.GetComponent<ButtonClickSoundHook>() != null)
            {
                continue;
            }

            ButtonClickSoundHook hook = button.gameObject.AddComponent<ButtonClickSoundHook>();
            hook.Bind(button);
        }
    }

    private void Play()
    {
        if (clickClip == null)
        {
            if (!missingClipWarned)
            {
                Debug.LogWarning($"Click sound clip could not be loaded from Resources/{ClickClipResourcePath}.");
                missingClipWarned = true;
            }

            return;
        }

        AudioManager.PlaySfx(audioSource, clickClip);
    }
}

public sealed class ButtonClickSoundHook : MonoBehaviour
{
    private Button button;

    public void Bind(Button targetButton)
    {
        if (targetButton == null)
        {
            return;
        }

        button = targetButton;
        button.onClick.RemoveListener(UiClickSoundPlayer.PlayClick);
        button.onClick.AddListener(UiClickSoundPlayer.PlayClick);
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(UiClickSoundPlayer.PlayClick);
        }
    }
}
