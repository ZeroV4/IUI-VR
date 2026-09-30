using System.Collections.Generic;
using UnityEngine;
using TMPro;

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

    // usability: text above the bin that shows how many items went in
    public TMP_Text progressText;

    private HashSet<TrashItemThrowData> counted = new();

    // DO NOT CHANGE
    void Reset() { scoreZone = GetComponent<Collider>(); }

    // called when something enters the scorecollider
    void OnTriggerEnter(Collider other)
    {   
        Debug.Log("Something entered the bin");
        // check what the other rigidbody is
        var rb = other.attachedRigidbody;
        if (!rb) return;

        var data = rb.GetComponent<TrashItemThrowData>();
        if (!data || counted.Contains(data)) return;
        
        float since = Time.time - data.releaseTime;
        float speed = data.releaseVel.magnitude;
        var center = rimCenter ? rimCenter.position : transform.position;
        float dist = Vector3.Distance(data.releasePos, center);
        bool downward = !requireDownwardEntry || Vector3.Dot(rb.linearVelocity.normalized, Vector3.down) > 0.2f;

        if (since <= maxSecondsSinceRelease && speed >= minReleaseSpeed && dist >= minReleaseDistance && downward)
        {
            score++;
            counted.Add(data);
            Debug.Log($"Trash: SCORE #{score} (speed {speed:F1}, dist {dist:F2}, t {since:F1}s)");
            // usability: sound when a throw counts
            GetComponent<AudioSource>().Play();
        }
        else
        {
            Debug.Log($"Trash: rejected (speed {speed:F1}, dist {dist:F2}, t {since:F1}s, down {downward})");
        }

        UpdateCompletion();
        ShowProgress();
    }

    // usability: trash reset respawned the items but kept the old score, this puts it back to 0
    public void ResetScore()
    {
        score = 0;
        counted.Clear();
        UpdateCompletion();
        ShowProgress();
    }

    // usability: shows 1/2 while throwing and goes green when both are in
    void ShowProgress()
    {
        if (!progressText) return;

        if (IsComplete)
        {
            progressText.text = "Trash done!";
            progressText.color = Color.green;
        }
        else
        {
            progressText.text = $"Trash {score}/2";
            progressText.color = Color.white;
        }
    }

    // check if the task is completed
    // DO NOT CHANGE
    private void UpdateCompletion()
    {
        // True only if at least 2 valid scores AND nothing removed (still 2+ inside)
        IsComplete = score >= 2 && counted.Count > 1;
    }
}
