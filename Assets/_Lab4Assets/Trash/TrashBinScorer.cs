using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TrashBinScorer : MonoBehaviour
{
    [Header("Setup")]
    public Transform rimCenter;     // point at the center/top of the bin opening
    [Tooltip("This object should have a Trigger collider (e.g., tall cylinder above bin).")]
    public Collider scoreZone;      // auto-filled by Reset if on same object

    [Header("Rules")]
    public float minReleaseSpeed = 1.2f;      // m/s; prevents “drop-ins”
    public float minReleaseDistance = 1.0f;   // meters from rim at release
    public float maxSecondsSinceRelease = 5f; // throw must be recent
    public bool requireDownwardEntry = true;  // must be moving downward when entering

    [Header("State")]
    public int score;
    public bool IsComplete { get; private set; }  // <-- new property

    private HashSet<TrashItemThrowData> counted = new();

    // [Usability] Feedback shown to the player in the world (logs are not visible in the headset)
    [Header("Feedback (added for usability)")]
    public TMP_Text statusText;              // "Trash 1/2" text above the bin
    public FeedbackSounds sounds;            // shared success / error sounds
    public GameRunController runController;  // used to ignore throws before Start
    public float messageSeconds = 2f;        // how long a red message stays

    public const int RequiredScore = 2;      // same number as in UpdateCompletion (locked)
    private bool showingMessage;

    // DO NOT CHANGE
    void Reset() { scoreZone = GetComponent<Collider>(); }

    // [Usability] Show "Trash 0/2" as soon as the scene starts
    void Start()
    {
        ShowCount();
    }

    // called when something enters the scorecollider
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Something entered the bin");
        // check what the other rigidbody is
        var rb = other.attachedRigidbody;
        if (!rb) return;

        var data = rb.GetComponent<TrashItemThrowData>();

        // [Usability] Before, anything that was not trash was ignored silently
        if (!data)
        {
            ShowMessage("Only the can and bottle count");
            return;
        }
        if (counted.Contains(data)) return;

        // [Usability] Trash is not locked before Start, so throws before Start used to count
        if (runController && !runController.Started)
        {
            ShowMessage("Press Start first");
            return;
        }

        float since = Time.time - data.releaseTime;
        float speed = data.releaseVel.magnitude;
        var center = rimCenter ? rimCenter.position : transform.position;
        float dist = Vector3.Distance(data.releasePos, center);
        bool downward = !requireDownwardEntry || Vector3.Dot(rb.linearVelocity.normalized, Vector3.down) > 0.2f;

        // [Usability] Same rules as before, but now we know WHICH rule failed,
        // so we can tell the player what to do differently
        string reason = GetRejectReason(since, speed, dist, downward);

        if (reason == null)
        {
            score++;
            counted.Add(data);
            Debug.Log($"Trash: SCORE #{score} (speed {speed:F1}, dist {dist:F2}, t {since:F1}s)");
            if (sounds) sounds.Success();
        }
        else
        {
            Debug.Log($"Trash: rejected (speed {speed:F1}, dist {dist:F2}, t {since:F1}s, down {downward}) -> \"{reason}\"");
            ShowMessage(reason);
        }

        UpdateCompletion();
        ShowCount();
    }

    // [Usability] Returns the first rule that failed, in words the player understands.
    // Returns null when the throw counts.
    string GetRejectReason(float since, float speed, float dist, bool downward)
    {
        if (since > maxSecondsSinceRelease) return "Throw it straight in";
        if (speed < minReleaseSpeed) return "Throw harder";
        if (dist < minReleaseDistance) return "Step back and throw";
        if (!downward) return "Throw it in from above";
        return null;
    }

    // [Usability] Called by the Trash Reset button, next to TrashRespawner.RespawnAll.
    // Before this, resetting respawned the trash but kept the old score.
    public void ResetScore()
    {
        score = 0;
        counted.Clear();
        UpdateCompletion(); // score is 0, so IsComplete becomes false
        Debug.Log("Trash: score reset");

        // clear any red message and show "Trash 0/2" again
        CancelInvoke(nameof(EndMessage));
        showingMessage = false;
        ShowCount();
    }

    // [Usability] Progress text: white while in progress, green when done
    void ShowCount()
    {
        if (!statusText || showingMessage) return;
        statusText.text = $"Trash {score}/{RequiredScore}";
        statusText.color = IsComplete ? Color.green : Color.white;
    }

    // [Usability] Red message + error sound for a few seconds, then back to the count
    void ShowMessage(string message)
    {
        Debug.Log($"Trash: feedback \"{message}\"");
        if (sounds) sounds.Error();
        if (!statusText) return;

        showingMessage = true;
        statusText.text = message;
        statusText.color = Color.red;

        CancelInvoke(nameof(EndMessage));
        Invoke(nameof(EndMessage), messageSeconds);
    }

    void EndMessage()
    {
        showingMessage = false;
        ShowCount();
    }

    // check if the task is completed
    // DO NOT CHANGE
    private void UpdateCompletion()
    {
        // True only if at least 2 valid scores AND nothing removed (still 2+ inside)
        IsComplete = score >= 2 && counted.Count > 1;
    }
}
