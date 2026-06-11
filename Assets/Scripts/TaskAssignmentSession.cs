using System.Collections.Generic;
using System;
using UnityEngine;

public enum OfficeTaskKind
{
    Stamp,
    Filing,
    Calendar,
    Contract,
    Phone,
    Printer,
    Trash,
    IncomingDocuments,
    CustomerFiles,
    Mail,
}

public sealed class TaskAssignment
{
    public OfficeTaskKind Kind { get; }
    public string DisplayName { get; }
    public int TimeLimitSeconds { get; }
    public int AccuracyTargetPercent { get; }

    public TaskAssignment(OfficeTaskKind kind, string displayName, int timeLimitSeconds, int accuracyTargetPercent)
    {
        Kind = kind;
        DisplayName = displayName;
        TimeLimitSeconds = Mathf.Max(1, timeLimitSeconds);
        AccuracyTargetPercent = Mathf.Clamp(accuracyTargetPercent, 0, 100);
    }
}

public sealed class TaskSelection
{
    public OfficeTaskKind Kind { get; }
    public string DisplayName { get; }

    public TaskSelection(OfficeTaskKind kind, string displayName)
    {
        Kind = kind;
        DisplayName = displayName;
    }
}

public static class TaskAssignmentSession
{
    private static readonly Dictionary<OfficeTaskKind, TaskAssignment> AssignmentsByKind = new Dictionary<OfficeTaskKind, TaskAssignment>();
    private static readonly Dictionary<OfficeTaskKind, TaskProgress> ProgressByKind = new Dictionary<OfficeTaskKind, TaskProgress>();
    private static readonly Dictionary<OfficeTaskKind, TaskSelection> SelectedTasksByKind = new Dictionary<OfficeTaskKind, TaskSelection>();
    private static readonly List<TaskAssignment> Assignments = new List<TaskAssignment>();
    private static readonly List<TaskSelection> SelectedTasks = new List<TaskSelection>();

    public static bool HasAssignments => Assignments.Count > 0;
    public static bool HasSelectedTasks => SelectedTasks.Count > 0;
    public static IReadOnlyList<TaskAssignment> CurrentAssignments => Assignments;
    public static IReadOnlyList<TaskSelection> CurrentSelectedTasks => SelectedTasks;
    public static event Action AssignmentsChanged;

    public static void SetSelectedTasks(IReadOnlyList<TaskSelection> selectedTasks)
    {
        SelectedTasks.Clear();
        SelectedTasksByKind.Clear();
        ProgressByKind.Clear();

        if (selectedTasks != null)
        {
            for (int i = 0; i < selectedTasks.Count; i++)
            {
                TaskSelection selection = selectedTasks[i];
                if (selection == null)
                {
                    continue;
                }

                SelectedTasks.Add(selection);
                SelectedTasksByKind[selection.Kind] = selection;
            }
        }

        AssignmentsChanged?.Invoke();
    }

    public static void SetAssignments(IReadOnlyList<TaskAssignment> assignments)
    {
        Assignments.Clear();
        AssignmentsByKind.Clear();
        ProgressByKind.Clear();

        if (assignments == null)
        {
            return;
        }

        for (int i = 0; i < assignments.Count; i++)
        {
            TaskAssignment assignment = assignments[i];
            if (assignment == null)
            {
                continue;
            }

            Assignments.Add(assignment);
            AssignmentsByKind[assignment.Kind] = assignment;
            if (!SelectedTasksByKind.ContainsKey(assignment.Kind))
            {
                TaskSelection selection = new TaskSelection(assignment.Kind, assignment.DisplayName);
                SelectedTasks.Add(selection);
                SelectedTasksByKind[selection.Kind] = selection;
            }
        }

        AssignmentsChanged?.Invoke();
    }

    public static void ClearAssignments()
    {
        Assignments.Clear();
        AssignmentsByKind.Clear();
        ProgressByKind.Clear();
        SelectedTasks.Clear();
        SelectedTasksByKind.Clear();
        AssignmentsChanged?.Invoke();
    }

    public static bool IsTaskEnabled(OfficeTaskKind kind)
    {
        if (HasSelectedTasks)
        {
            return SelectedTasksByKind.ContainsKey(kind);
        }

        return false;
    }

    public static bool TryGetAssignment(OfficeTaskKind kind, out TaskAssignment assignment)
    {
        return AssignmentsByKind.TryGetValue(kind, out assignment);
    }

    public static void RegisterTaskTimer(OfficeTaskKind kind, TaskTimer timer)
    {
        if (timer == null || !TryGetAssignment(kind, out TaskAssignment assignment))
        {
            return;
        }

        ProgressByKind[kind] = new TaskProgress(timer, true, false, false, assignment.TimeLimitSeconds);
        AssignmentsChanged?.Invoke();
    }

    public static void MarkTaskCompleted(OfficeTaskKind kind)
    {
        if (!TryGetAssignment(kind, out TaskAssignment assignment))
        {
            return;
        }

        TaskProgress progress = ProgressByKind.TryGetValue(kind, out TaskProgress current)
            ? current
            : new TaskProgress(null, false, false, false, assignment.TimeLimitSeconds);

        ProgressByKind[kind] = new TaskProgress(progress.Timer, progress.HasStarted, true, false, progress.InitialSeconds);
        AssignmentsChanged?.Invoke();
    }

    public static void MarkTaskFailed(OfficeTaskKind kind)
    {
        if (!TryGetAssignment(kind, out TaskAssignment assignment))
        {
            return;
        }

        TaskProgress progress = ProgressByKind.TryGetValue(kind, out TaskProgress current)
            ? current
            : new TaskProgress(null, false, false, false, assignment.TimeLimitSeconds);

        ProgressByKind[kind] = new TaskProgress(progress.Timer, progress.HasStarted, false, true, progress.InitialSeconds);
        AssignmentsChanged?.Invoke();
    }

    public static bool TryGetProgress(OfficeTaskKind kind, out TaskProgress progress)
    {
        return ProgressByKind.TryGetValue(kind, out progress);
    }

    public static string FormatSeconds(float seconds)
    {
        int wholeSeconds = Mathf.CeilToInt(Mathf.Max(0f, seconds));
        int minutes = wholeSeconds / 60;
        int remainder = wholeSeconds % 60;
        return $"{minutes:00}:{remainder:00}";
    }

    public static int CalculateAccuracyPercent(int correctCount, int wrongCount)
    {
        int attempts = Mathf.Max(0, correctCount) + Mathf.Max(0, wrongCount);
        return attempts > 0 ? Mathf.RoundToInt(correctCount / (float)attempts * 100f) : 0;
    }

    public static string BuildAccuracyLine(OfficeTaskKind kind, int accuracyPercent)
    {
        if (!TryGetAssignment(kind, out TaskAssignment assignment))
        {
            return $"Doğruluk: %{accuracyPercent}";
        }

        string result = accuracyPercent >= assignment.AccuracyTargetPercent ? "Başarılı" : "Başarısız";
        return $"Doğruluk: %{accuracyPercent}\nHedef: %{assignment.AccuracyTargetPercent}\n{result}";
    }

    public readonly struct TaskProgress
    {
        public readonly TaskTimer Timer;
        public readonly bool HasStarted;
        public readonly bool IsCompleted;
        public readonly bool IsFailed;
        public readonly float InitialSeconds;

        public TaskProgress(TaskTimer timer, bool hasStarted, bool isCompleted, bool isFailed, float initialSeconds)
        {
            Timer = timer;
            HasStarted = hasStarted;
            IsCompleted = isCompleted;
            IsFailed = isFailed;
            InitialSeconds = initialSeconds;
        }

        public bool IsRunning => Timer != null && Timer.IsRunning && !IsCompleted && !IsFailed;
        public float RemainingSeconds => Timer != null ? Timer.RemainingSeconds : InitialSeconds;
    }
}
