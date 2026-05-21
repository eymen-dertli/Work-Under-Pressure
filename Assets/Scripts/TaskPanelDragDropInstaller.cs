using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class TaskPanelDragDropInstaller : MonoBehaviour
{
    private const string InstalledMarkerName = "_TaskPanelDragDropInstalled";
    private const string TimeValueType = "time";
    private const string TrustValueType = "trust";

    private static readonly int[] TimeValues = { 90, 80, 70, 60, 50, 40 };
    private static readonly int[] TrustValues = { 90, 80, 70, 60, 50, 100 };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void RegisterSceneLoadedHook()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallForInitialScene()
    {
        Install();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Install();
    }

    private static void Install()
    {
        if (GameObject.Find(InstalledMarkerName) != null)
        {
            return;
        }

        Transform times = FindTransform("Times");
        Transform trusts = FindTransform("Trusts");
        Transform timeFills = FindTransform("TimesFills");
        Transform trustFills = FindTransform("TrustsFills") ?? FindTransform("TrustFills");

        if (times == null || trusts == null || timeFills == null || trustFills == null)
        {
            return;
        }

        EnsureEventSystem();
        EnsureCanvasRaycaster(times);

        GameObject marker = new GameObject(InstalledMarkerName);
        marker.AddComponent<TaskPanelDragDropInstaller>();

        SetupSourceValues(times, TimeValues, TimeValueType, "s");
        SetupSourceValues(trusts, TrustValues, TrustValueType, "%");
        SetupDropSlots(timeFills, TimeValueType);
        SetupDropSlots(trustFills, TrustValueType);
    }

    private static void SetupSourceValues(Transform parent, int[] values, string valueType, string suffixOrPrefix)
    {
        EnsureChildCount(parent, values.Length, false);

        for (int i = 0; i < values.Length; i++)
        {
            Transform child = parent.GetChild(i);
            child.gameObject.SetActive(true);

            TaskPanelDragItem dragItem = child.GetComponent<TaskPanelDragItem>();
            if (dragItem == null)
            {
                dragItem = child.gameObject.AddComponent<TaskPanelDragItem>();
            }

            string displayText = valueType == TrustValueType ? $"{suffixOrPrefix}{values[i]}" : $"{values[i]}{suffixOrPrefix}";
            dragItem.Configure(valueType, values[i], displayText);
        }
    }

    private static void SetupDropSlots(Transform parent, string valueType)
    {
        EnsureChildCount(parent, 6, true);

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            child.gameObject.SetActive(true);

            TaskPanelDropSlot dropSlot = child.GetComponent<TaskPanelDropSlot>();
            if (dropSlot == null)
            {
                dropSlot = child.gameObject.AddComponent<TaskPanelDropSlot>();
            }

            dropSlot.Configure(valueType);
        }
    }

    private static void EnsureChildCount(Transform parent, int count, bool isFillSlot)
    {
        while (parent.childCount < count)
        {
            GameObject child = new GameObject(isFillSlot ? $"Fill {parent.childCount + 1}" : $"Value {parent.childCount + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            child.transform.SetParent(parent, false);

            RectTransform rect = child.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(72f, 64f);
            rect.anchoredPosition = new Vector2(parent.childCount * 80f, 0f);

            Image image = child.GetComponent<Image>();
            image.color = isFillSlot ? new Color(1f, 1f, 1f, 0.5f) : Color.white;
        }
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(eventSystem);
        }
    }

    private static void EnsureCanvasRaycaster(Transform transformInCanvas)
    {
        Canvas canvas = transformInCanvas.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private static Transform FindTransform(string objectName)
    {
        GameObject found = GameObject.Find(objectName);
        return found != null ? found.transform : null;
    }
}
