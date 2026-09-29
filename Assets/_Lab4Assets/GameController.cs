using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
// DO NOT CHANGE
// Handles the start and end of the run (uses RunSummaryAndQuit for displaying the final result)
public class GameRunController : MonoBehaviour
{
    [Header("Objects that should be locked until Start is pressed")]
    public List<GameObject> interactiveRoots = new();

    [Header("Insert FileStack object here")]
    public FileStackSpawner fss;

    [Header("Run State (read-only at runtime)")]
    public bool Started { get; private set; }
    public DateTime StartTimeUtc { get; private set; }
    public DateTime EndTimeUtc { get; private set; }

    // Prevent double-finish
    private bool _finished;

    void Awake()
    {
        // Lock interaction on load
        SetInteractionLocked(true);
    }

    public void StartRun()
    {
        if (Started) return;

        Started = true;
        _finished = false;
        StartTimeUtc = DateTime.UtcNow;

        SetInteractionLocked(false);
        try { fss?.Spawn(); } catch (Exception e) { Debug.LogWarning($"FileStackSpawner.Spawn failed: {e.Message}"); }

        Debug.Log($"Run started at {StartTimeUtc:O}");
    }

    // Finish the run, compute duration, and write JSON. Accepts an optional list of completed task names.
    public void FinishRunAndWriteJson(List<string> completedTasks = null)
    {
        if (!Started)
        {
            Debug.LogWarning("FinishRun called before StartRun.");
            return;
        }
        if (_finished) return;
        _finished = true;

        EndTimeUtc = DateTime.UtcNow;
        var duration = (float)(EndTimeUtc - StartTimeUtc).TotalSeconds;

        var record = new RunRecord
        {
            startTimeUtc = StartTimeUtc.ToString("O"),
            endTimeUtc   = EndTimeUtc.ToString("O"),
            durationSeconds = duration,
            completedTasks  = completedTasks ?? new List<string>(),
            errorRates = new Dictionary<string, float>() // keep for future use
        };

        try
        {
            var json = JsonUtility.ToJson(record, true);
            var path = Path.Combine(Application.persistentDataPath, "run.json");
            File.WriteAllText(path, json);
            Debug.Log($"Run finished. Wrote {path}\n{json}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to write run.json: {e}");
        }
    }

    void SetInteractionLocked(bool locked)
    {
        foreach (var root in interactiveRoots)
        {
            if (!root) continue;

            // Only affect XR-grabbables
            foreach (var grab in root.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(true))
            {
                var go = grab.gameObject;

                // Colliders on the grabbable
                foreach (var col in go.GetComponentsInChildren<Collider>(true))
                    col.enabled = !locked;

                // Its Rigidbody
                var rb = go.GetComponent<Rigidbody>();
                if (rb) rb.isKinematic = locked;

                // The interactable script itself
                grab.enabled = !locked;
            }
        }
    }

    [Serializable]
    public class RunRecord
    {
        public string startTimeUtc;
        public string endTimeUtc;
        public float durationSeconds;
        public List<string> completedTasks;
        public Dictionary<string, float> errorRates;
    }
}
