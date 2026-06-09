using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public sealed class TaskTimer : MonoBehaviour
{
    private static readonly List<TaskTimer> ActiveTimers = new List<TaskTimer>();

    [SerializeField] private float durationSeconds = 60f;
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private bool autoHideWhenStopped = true;
    [SerializeField] private GameObject timerRoot;
    [SerializeField] private TextMeshProUGUI timerLabel;
    [SerializeField] private UnityEvent onTimerExpired = new UnityEvent();

    private float remainingSeconds;
    private bool isRunning;
    private bool isPaused;

    public event Action<TaskTimer> TimerExpired;

    public float DurationSeconds => durationSeconds;
    public float RemainingSeconds => remainingSeconds;
    public bool IsRunning => isRunning;
    public bool IsPaused => isPaused;

    private void OnDisable()
    {
        ActiveTimers.Remove(this);
    }

    private void Update()
    {
        if (!isRunning || isPaused)
        {
            return;
        }

        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        remainingSeconds = Mathf.Max(0f, remainingSeconds - deltaTime);
        RefreshUi();

        if (remainingSeconds <= 0f)
        {
            Expire();
        }
    }

    public void SetDuration(float seconds)
    {
        durationSeconds = Mathf.Max(1f, seconds);
        remainingSeconds = durationSeconds;
        RefreshUi();
    }

    public void StartTimer()
    {
        StartTimer(durationSeconds);
    }

    public void StartTimer(float seconds)
    {
        EnsureUi();
        durationSeconds = Mathf.Max(1f, seconds);
        remainingSeconds = durationSeconds;
        isRunning = true;
        isPaused = false;

        if (!ActiveTimers.Contains(this))
        {
            ActiveTimers.Add(this);
        }

        SetTimerVisible(true);
        RefreshUi();
    }

    public void StopTimer()
    {
        isRunning = false;
        isPaused = false;
        ActiveTimers.Remove(this);

        if (autoHideWhenStopped)
        {
            SetTimerVisible(false);
        }
    }

    public void PauseTimer()
    {
        if (isRunning)
        {
            isPaused = true;
        }
    }

    public void ResumeTimer()
    {
        if (isRunning)
        {
            isPaused = false;
        }
    }

    public static void PauseAllActive()
    {
        foreach (TaskTimer timer in ActiveTimers.ToArray())
        {
            if (timer != null)
            {
                timer.PauseTimer();
            }
        }
    }

    public static void ResumeAllActive()
    {
        foreach (TaskTimer timer in ActiveTimers.ToArray())
        {
            if (timer != null)
            {
                timer.ResumeTimer();
            }
        }
    }

    private void Expire()
    {
        StopTimer();
        onTimerExpired.Invoke();
        TimerExpired?.Invoke(this);
    }

    private void RefreshUi()
    {
        if (timerLabel == null)
        {
            return;
        }

        int seconds = Mathf.CeilToInt(remainingSeconds);
        int minutes = seconds / 60;
        int remainder = seconds % 60;
        timerLabel.text = $"Görev Saati {minutes:00}:{remainder:00}";
    }

    private void SetTimerVisible(bool visible)
    {
        if (timerRoot != null)
        {
            timerRoot.SetActive(visible);
        }
    }

    private void EnsureUi()
    {
        if (timerRoot != null && timerLabel != null)
        {
            return;
        }

        Canvas canvas = OfficeMiniGameUi.CreateOverlayCanvas("Task Timer Canvas", transform, 1020);
        timerRoot = OfficeMiniGameUi.CreateImage("Task Timer Root", canvas.transform, new Color(0.08f, 0.08f, 0.09f, 0.82f));
        RectTransform timerRect = timerRoot.GetComponent<RectTransform>();
        timerRect.anchorMin = new Vector2(0.5f, 1f);
        timerRect.anchorMax = new Vector2(0.5f, 1f);
        timerRect.sizeDelta = new Vector2(310f, 48f);
        timerRect.anchoredPosition = new Vector2(0f, -42f);

        timerLabel = OfficeMiniGameUi.CreateLabel("TaskTimerLabel", timerRoot.transform, "Görev Saati 00:00", 22f, Color.white);
        OfficeMiniGameUi.Stretch(timerLabel.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
    }
}
