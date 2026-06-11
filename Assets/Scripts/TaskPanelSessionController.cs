using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class TaskPanelSessionController : MonoBehaviour
{
    private const string ControllerName = "_TaskPanelSessionController";
    private const int TaskCount = 5;
    private const string TimeValueType = "time";
    private const string TrustValueType = "trust";

    private static readonly int[] TimeValues = { 90, 80, 70, 60, 50, 40 };
    private static readonly int[] TrustValues = { 90, 80, 70, 60, 50, 100 };

    private static readonly TaskDefinition[] TaskPool =
    {
        new TaskDefinition(OfficeTaskKind.IncomingDocuments, "Evrakları Ayırma"),
        new TaskDefinition(OfficeTaskKind.Mail, "Mailleri Cevaplama"),
        new TaskDefinition(OfficeTaskKind.Stamp, "Belgeleri Kaşeleme"),
        new TaskDefinition(OfficeTaskKind.CustomerFiles, "Müşteri Dosyalarını Sıralama"),
        new TaskDefinition(OfficeTaskKind.Phone, "Telefon Cevaplayıp Not Alma"),
        new TaskDefinition(OfficeTaskKind.Filing, "Dosya Tamamlama"),
        new TaskDefinition(OfficeTaskKind.Calendar, "Takvim Düzenleme"),
        new TaskDefinition(OfficeTaskKind.Contract, "Sözleşme İmzalama"),
    };

    private readonly List<TaskDefinition> selectedTasks = new List<TaskDefinition>();
    private Transform taskPanel;
    private Transform tasksRoot;
    private Transform timesRoot;
    private Transform trustsRoot;
    private Transform timeFillsRoot;
    private Transform trustFillsRoot;
    private TextMeshProUGUI statusLabel;

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
        if (!IsLevelScene(scene.name) || FindAnyObjectByType<TaskPanelSessionController>() != null)
        {
            return;
        }

        GameObject controllerObject = new GameObject(ControllerName);
        TaskPanelSessionController controller = controllerObject.AddComponent<TaskPanelSessionController>();
        controller.Initialize();
    }

    private static bool IsLevelScene(string sceneName)
    {
        LevelDefinition level = LevelDatabase.Load().GetLevelBySceneName(sceneName);
        return level != null;
    }

    private void Initialize()
    {
        TaskAssignmentSession.ClearAssignments();

        taskPanel = FindTransformIncludingInactive("TaskPanel");
        tasksRoot = FindTransformIncludingInactive("Tasks");
        timesRoot = FindTransformIncludingInactive("Times");
        trustsRoot = FindTransformIncludingInactive("Trusts");
        timeFillsRoot = FindTransformIncludingInactive("TimesFills");
        trustFillsRoot = FindTransformIncludingInactive("TrustFills") ?? FindTransformIncludingInactive("TrustsFills");

        if (taskPanel == null || tasksRoot == null || timesRoot == null || trustsRoot == null || timeFillsRoot == null || trustFillsRoot == null)
        {
            return;
        }

        SetupSourceValues(timesRoot, TimeValues, TimeValueType, "s");
        SetupSourceValues(trustsRoot, TrustValues, TrustValueType, "%");
        SetupDropSlots(timeFillsRoot, TimeValueType);
        SetupDropSlots(trustFillsRoot, TrustValueType);
        EnsureEventSystem();
        EnsureCanvasRaycaster();
        RandomizeTasks();
        RegisterSelectedTasks();
        ClearDropSlots(timeFillsRoot);
        ClearDropSlots(trustFillsRoot);
        SetDefaultDropSlotValues();
        RegisterDefaultAssignments();
        CreateTaskLabels();
        CreateStatusLabel();
        BindOkButton();

        taskPanel.gameObject.SetActive(true);
        SetStatus("Süre ve doğruluk hedeflerini görevlerle eşleştir.");
    }

    private void RandomizeTasks()
    {
        selectedTasks.Clear();

        List<TaskDefinition> pool = new List<TaskDefinition>(TaskPool);
        for (int i = 0; i < TaskCount && pool.Count > 0; i++)
        {
            int index = Random.Range(0, pool.Count);
            selectedTasks.Add(pool[index]);
            pool.RemoveAt(index);
        }
    }

    private void RegisterSelectedTasks()
    {
        List<TaskSelection> selections = new List<TaskSelection>();
        for (int i = 0; i < selectedTasks.Count; i++)
        {
            TaskDefinition task = selectedTasks[i];
            selections.Add(new TaskSelection(task.Kind, task.DisplayName));
        }

        TaskAssignmentSession.SetSelectedTasks(selections);
    }

    private void RegisterDefaultAssignments()
    {
        List<TaskAssignment> assignments = new List<TaskAssignment>();
        for (int i = 0; i < selectedTasks.Count; i++)
        {
            TaskDefinition task = selectedTasks[i];
            assignments.Add(new TaskAssignment(task.Kind, task.DisplayName, GetDefaultTimeValue(i), GetDefaultTrustValue(i)));
        }

        TaskAssignmentSession.SetAssignments(assignments);
    }

    private void CreateTaskLabels()
    {
        ClearGeneratedTaskLabels();

        List<RectTransform> taskBackgrounds = GetSortedChildren(tasksRoot);

        for (int i = 0; i < taskBackgrounds.Count; i++)
        {
            taskBackgrounds[i].gameObject.SetActive(i < selectedTasks.Count);
        }

        for (int i = 0; i < selectedTasks.Count && i < taskBackgrounds.Count; i++)
        {
            GameObject labelObject = new GameObject($"Generated Task Label {i + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(tasksRoot, false);
            labelObject.transform.SetAsLastSibling();

            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = taskBackgrounds[i].pivot;
            labelRect.sizeDelta = taskBackgrounds[i].sizeDelta;
            labelRect.anchoredPosition = taskBackgrounds[i].anchoredPosition;
            labelRect.localRotation = Quaternion.identity;

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = selectedTasks[i].DisplayName;
            label.margin = new Vector4(14f, 0f, 14f, 0f);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.fontStyle = FontStyles.Bold;
            label.fontSize = 19f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 12f;
            label.fontSizeMax = 19f;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.color = new Color(0.1f, 0.08f, 0.06f, 1f);
            label.raycastTarget = false;
        }
    }

    private void ClearGeneratedTaskLabels()
    {
        TextMeshProUGUI[] labels = tasksRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = labels.Length - 1; i >= 0; i--)
        {
            if (labels[i].name.StartsWith("Generated Task Label"))
            {
                Destroy(labels[i].gameObject);
            }
        }
    }

    private void CreateStatusLabel()
    {
        Transform background = FindTransformIncludingInactive("TaskBackground") ?? taskPanel;
        GameObject labelObject = new GameObject("Generated Task Status", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(background, false);

        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(700f, 40f);
        rect.anchoredPosition = new Vector2(0f, 28f);

        statusLabel = labelObject.GetComponent<TextMeshProUGUI>();
        statusLabel.alignment = TextAlignmentOptions.Center;
        statusLabel.fontSize = 20f;
        statusLabel.fontStyle = FontStyles.Bold;
        statusLabel.color = new Color(0.25f, 0.08f, 0.04f, 1f);
        statusLabel.raycastTarget = false;
    }

    private void BindOkButton()
    {
        Transform okTransform = FindTransformIncludingInactive("OKButton");
        if (okTransform == null)
        {
            return;
        }

        Button button = okTransform.GetComponent<Button>();
        if (button == null)
        {
            button = okTransform.gameObject.AddComponent<Button>();
        }

        button.onClick.RemoveListener(ConfirmAssignments);
        button.onClick.AddListener(ConfirmAssignments);
    }

    private void ConfirmAssignments()
    {
        List<RectTransform> timeSlots = GetSortedChildren(timeFillsRoot);
        List<RectTransform> trustSlots = GetSortedChildren(trustFillsRoot);
        List<TaskAssignment> assignments = new List<TaskAssignment>();

        for (int i = 0; i < selectedTasks.Count; i++)
        {
            TaskPanelDropSlot timeSlot = i < timeSlots.Count ? timeSlots[i].GetComponent<TaskPanelDropSlot>() : null;
            TaskPanelDropSlot trustSlot = i < trustSlots.Count ? trustSlots[i].GetComponent<TaskPanelDropSlot>() : null;
            if (timeSlot == null || trustSlot == null)
            {
                SetStatus("Görev süre ve doğruluk alanları bulunamadı.");
                return;
            }

            TaskDefinition task = selectedTasks[i];
            int timeValue = timeSlot.HasValue ? timeSlot.NumericValue : GetDefaultTimeValue(i);
            int trustValue = trustSlot.HasValue ? trustSlot.NumericValue : GetDefaultTrustValue(i);
            assignments.Add(new TaskAssignment(task.Kind, task.DisplayName, timeValue, trustValue));
        }

        TaskAssignmentSession.SetAssignments(assignments);
        taskPanel.gameObject.SetActive(false);
    }

    private void SetDefaultDropSlotValues()
    {
        List<RectTransform> timeSlots = GetSortedChildren(timeFillsRoot);
        List<RectTransform> trustSlots = GetSortedChildren(trustFillsRoot);

        for (int i = 0; i < selectedTasks.Count; i++)
        {
            if (i < timeSlots.Count && timeSlots[i].TryGetComponent(out TaskPanelDropSlot timeSlot))
            {
                int value = GetDefaultTimeValue(i);
                timeSlot.SetValue(value, FormatTimeValue(value));
            }

            if (i < trustSlots.Count && trustSlots[i].TryGetComponent(out TaskPanelDropSlot trustSlot))
            {
                int value = GetDefaultTrustValue(i);
                trustSlot.SetValue(value, FormatTrustValue(value));
            }
        }
    }

    private static void ClearDropSlots(Transform parent)
    {
        List<RectTransform> slots = GetSortedChildren(parent);
        for (int i = 0; i < slots.Count; i++)
        {
            TaskPanelDropSlot slot = slots[i].GetComponent<TaskPanelDropSlot>();
            if (slot != null)
            {
                slot.ClearValue();
            }
        }
    }

    private static void SetupSourceValues(Transform parent, int[] values, string valueType, string suffixOrPrefix)
    {
        List<RectTransform> children = GetChildrenInSiblingOrder(parent);
        for (int i = 0; i < values.Length && i < children.Count; i++)
        {
            TaskPanelDragItem dragItem = children[i].GetComponent<TaskPanelDragItem>();
            if (dragItem == null)
            {
                dragItem = children[i].gameObject.AddComponent<TaskPanelDragItem>();
            }

            string displayText = valueType == TrustValueType ? FormatTrustValue(values[i]) : FormatTimeValue(values[i]);
            dragItem.Configure(valueType, values[i], displayText);
        }
    }

    private static void SetupDropSlots(Transform parent, string valueType)
    {
        List<RectTransform> children = GetSortedChildren(parent);
        for (int i = 0; i < children.Count; i++)
        {
            TaskPanelDropSlot dropSlot = children[i].GetComponent<TaskPanelDropSlot>();
            if (dropSlot == null)
            {
                dropSlot = children[i].gameObject.AddComponent<TaskPanelDropSlot>();
            }

            dropSlot.Configure(valueType);
        }
    }

    private void SetStatus(string text)
    {
        if (statusLabel != null)
        {
            statusLabel.text = text;
        }
    }

    private static int GetDefaultTimeValue(int index)
    {
        return TimeValues[Mathf.Clamp(index, 0, TimeValues.Length - 1)];
    }

    private static int GetDefaultTrustValue(int index)
    {
        return TrustValues[Mathf.Clamp(index, 0, TrustValues.Length - 1)];
    }

    private static string FormatTimeValue(int value)
    {
        return $"{value}s";
    }

    private static string FormatTrustValue(int value)
    {
        return $"%{value}";
    }

    private static List<RectTransform> GetSortedChildren(Transform parent)
    {
        List<RectTransform> children = new List<RectTransform>();
        if (parent == null)
        {
            return children;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            if (parent.GetChild(i) is RectTransform rect)
            {
                children.Add(rect);
            }
        }

        children.Sort((left, right) => right.anchoredPosition.y.CompareTo(left.anchoredPosition.y));
        return children;
    }

    private static List<RectTransform> GetChildrenInSiblingOrder(Transform parent)
    {
        List<RectTransform> children = new List<RectTransform>();
        if (parent == null)
        {
            return children;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            if (parent.GetChild(i) is RectTransform rect)
            {
                children.Add(rect);
            }
        }

        return children;
    }

    private static Transform FindTransformIncludingInactive(string objectName)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate != null && candidate.name == objectName && candidate.gameObject.scene.IsValid())
            {
                return candidate;
            }
        }

        return null;
    }

    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private void EnsureCanvasRaycaster()
    {
        Canvas canvas = taskPanel.GetComponentInParent<Canvas>(true);
        if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private readonly struct TaskDefinition
    {
        public readonly OfficeTaskKind Kind;
        public readonly string DisplayName;

        public TaskDefinition(OfficeTaskKind kind, string displayName)
        {
            Kind = kind;
            DisplayName = displayName;
        }
    }
}
