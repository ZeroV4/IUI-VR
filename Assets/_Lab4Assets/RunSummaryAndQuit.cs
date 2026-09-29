// RunSummaryOnQuit.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TMPro;
using UnityEngine;

public class RunSummaryOnQuit : MonoBehaviour
{
    // NESTED DTO (keeps Unity happy)
    [Serializable]
    private class RunRecordDTO
    {
        public string startTimeUtc;
        public string endTimeUtc;
        public float durationSeconds;
        public List<string> completedTasks;
    }

    [Header("Hook these up")]
    public TMP_Text instructionsText;          // your existing instructions TMP
    public List<GameObject> uiToHide = new();  // buttons/containers to hide on quit
    public GameRunController runController;    // assign your existing controller

    [Header("Optional: tasks to record (only if you want completedTasks in JSON)")]
    public DrawerTask drawerA;
    public DrawerTask drawerB;
    public CleaningTask cleaningTask;
    public TrashBinScorer trashTask;
    public CoffeeTask coffeeTask;

    [Header("File")]
    public string fileName = "run.json";       // must match GameRunController

    [Header("Behavior")]
    public float delaySeconds = 10f;           // countdown before quit

    string _path;

    void Awake()
    {
        _path = Path.Combine(Application.persistentDataPath, fileName);
    }

    /// Call this from Quit button
    public void ShowSummaryThenQuit()
    {
        // 1) Finish the run FIRST so we get fresh JSON (if a run was started)
        TryFinishRunNow();

        // 2) Hide buttons/controls while showing summary
        foreach (var go in uiToHide)
            if (go) go.SetActive(false);

        // 3) Initial render
        if (instructionsText)
            instructionsText.text = BuildSummaryText(Mathf.CeilToInt(delaySeconds));

        // 4) Start countdown + quit
        StartCoroutine(QuitAfterDelay(delaySeconds));
    }

    void TryFinishRunNow()
    {
        if (runController == null) return;

        // Only finish if a run actually started
        if (!runController.Started)
        {
            Debug.LogWarning("ShowSummaryThenQuit called, but run was never started. Using existing JSON if present.");
            return;
        }

        // If your GameRunController has the new API with completed tasks:
        List<string> completed = GetCompletedTasks();
        try
        {
            runController.FinishRunAndWriteJson(completed);
        }
        catch (MissingMethodException)
        {
            // Old signature fallback (no task list)
            runController.FinishRunAndWriteJson();
        }
        catch (Exception e)
        {
            Debug.LogError($"FinishRun failed: {e}");
        }

        // Helpful debugging: show where we wrote and when
        var path = Path.Combine(Application.persistentDataPath, fileName);
        try
        {
            if (File.Exists(path))
            {
                var t = File.GetLastWriteTime(path);
                Debug.Log($"run.json written at: {path}\nLastWrite (local): {t:yyyy-MM-dd HH:mm:ss}");
            }
            else
            {
                Debug.LogWarning($"Expected run.json at: {path} but it does not exist yet.");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Could not stat run.json: {e.Message}");
        }
    }

    IEnumerator QuitAfterDelay(float seconds)
    {
        float end = Time.unscaledTime + Mathf.Max(0f, seconds);

        while (Time.unscaledTime < end)
        {
            int remaining = Mathf.CeilToInt(end - Time.unscaledTime);
            if (instructionsText)
                instructionsText.text = BuildSummaryText(remaining);
            yield return null;
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit(); // Quest-safe
#endif
    }

    string BuildSummaryText(int secondsLeft)
    {
        var countdownLine = $"\n\nExiting in {Mathf.Max(0, secondsLeft)}s…";

        // Show path + time for debugging
        string header = $"<b>Run Summary</b>\n<color=#888>{_path}</color>\n";

        if (!File.Exists(_path))
            return header + $"No run.json found.{countdownLine}";

        try
        {
            // (Optional) force a short delay to ensure write has flushed on slower storage
            // yield return null;  // not allowed here (non-coroutine), but we finished first anyway.

            string json = File.ReadAllText(_path);
            var data = JsonUtility.FromJson<RunRecordDTO>(json);

            if (data == null)
                return header + $"Invalid JSON.\nRaw:\n{json}{countdownLine}";

            string startLocal = FormatIsoLocal(data.startTimeUtc);
            string endLocal   = FormatIsoLocal(data.endTimeUtc);
            string dur        = FormatDuration(data.durationSeconds);

            string tasks = "None";
            if (data.completedTasks != null && data.completedTasks.Count > 0)
                tasks = string.Join("\n", data.completedTasks.ConvertAll(t => $"• ✅ {t}"));

            var lastWrite = File.GetLastWriteTime(_path).ToString("yyyy-MM-dd HH:mm:ss");

            return header +
                   $"Last write: {lastWrite}\n" +
                   $"Start: {startLocal}\n" +
                   $"End:   {endLocal}\n" +
                   $"Duration: {dur}\n\n" +
                   $"<b>Completed Tasks</b>\n{tasks}" +
                   countdownLine;
        }
        catch (Exception e)
        {
            return header + $"Error reading run.json:\n{e.Message}{countdownLine}";
        }
    }

    // Build the completed task list without needing ProgressTracker
    List<string> GetCompletedTasks()
    {
        var list = new List<string>();
        if (drawerA && drawerA.IsComplete) list.Add("Drawer A");
        if (drawerB && drawerB.IsComplete) list.Add("Drawer B");
        if (cleaningTask && cleaningTask.IsComplete) list.Add("Cleaning");
        if (trashTask && trashTask.IsComplete) list.Add("Trash");
        if (coffeeTask && coffeeTask.IsComplete) list.Add("Coffee");
        return list;
    }

    static string FormatIsoLocal(string iso)
    {
        if (string.IsNullOrEmpty(iso)) return "-";
        if (DateTime.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
            return dt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
        return iso;
    }

    static string FormatDuration(float seconds)
    {
        if (seconds <= 0.01f) return "-";
        var ts = TimeSpan.FromSeconds(seconds);
        if (ts.TotalHours >= 1)   return $"{(int)ts.TotalHours}h {ts.Minutes}m {ts.Seconds}s";
        if (ts.TotalMinutes >= 1) return $"{ts.Minutes}m {ts.Seconds}s";
        return $"{Mathf.RoundToInt(seconds)}s";
    }
}
