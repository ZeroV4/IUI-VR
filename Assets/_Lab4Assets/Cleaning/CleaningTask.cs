using UnityEngine;

/**
This class handles the logic of the cleaning task

You can make changes in this file
*/
public class CleaningTask : MonoBehaviour
{   
    [Header("You can change this file, just not these pre-set parameters")]
    [Header("Drag colliders here")]
    public Collider[] targets;       // Drag grid colliders manually

    [Header("Filter (assign the sponge's Rigidbody)")]
    public Rigidbody spongeRigidbody; 

    [Header("State")]
    public bool cleaningTask;        // True when all zones touched
    public bool IsComplete { get { return cleaningTask; } }

    // [Usability] Progress numbers (the menu checklist reads them)
    public int TouchedCount { get { return touchedCount; } }
    public int TargetCount { get { return targets != null ? targets.Length : 0; } }

    // [Usability] Success sound when every spot is clean
    [Header("Feedback (added for usability)")]
    public FeedbackSounds sounds;

    // Internal fields
    private bool[] touched;
    private int touchedCount;

    // DO NOT CHANGE THIS METHOD
    void Start()
    {

        // initialize array keeping track of progress
        int n = (targets != null) ? targets.Length : 0;
        touched = new bool[n];
        touchedCount = 0;

        // no zones means already complete
        cleaningTask = (n == 0); 
    }

    // This method is called when a trigger collider is touched
    void OnTriggerEnter(Collider other)
    {
        if (cleaningTask || targets == null) return;

        // Only count when the assigned sponge Rigidbody touches the zone
        if (spongeRigidbody != null && other.attachedRigidbody != spongeRigidbody)
            return;

        // Loop over the targets, to see if this collision is a new one
        for (int i = 0; i < targets.Length; i++)
        {
            if (!touched[i] && other == targets[i])
            {
                touched[i] = true;
                touchedCount++;
                Debug.Log("Touched a cleaning spot");

                // [Usability] The dirt mark on this spot disappears, so the player sees what is left
                SetDirtVisible(targets[i], false);

                if (touchedCount == targets.Length)
                {
                    cleaningTask = true;
                    Debug.Log("Cleaning task COMPLETE: all zones touched.");
                    if (sounds) sounds.Success(); // [Usability]
                }
                break;
            }
        }
    }

    // [Usability] Called by the Cleaning Reset button, next to CleaningTaskController.ResetTask.
    // Before this, resetting only moved the sponge back; the wiped spots stayed wiped.
    public void ResetProgress()
    {
        if (touched != null)
        {
            for (int i = 0; i < touched.Length; i++)
                touched[i] = false;
        }
        touchedCount = 0;

        // same rule as Start(): no zones means already complete
        cleaningTask = (targets == null || targets.Length == 0);

        // [Usability] Show all dirt marks again
        if (targets != null)
        {
            foreach (var target in targets)
                SetDirtVisible(target, true);
        }

        Debug.Log("[CleaningReset] Zone progress cleared.");
    }

    // [Usability] The dirt marks are the child objects of each target zone (added in the Editor).
    // Before, the zones were invisible, so the player could not see where to wipe.
    static void SetDirtVisible(Collider target, bool visible)
    {
        if (!target) return;
        foreach (Transform child in target.transform)
            child.gameObject.SetActive(visible);
    }

}
