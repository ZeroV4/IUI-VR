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

                if (touchedCount == targets.Length)
                {
                    cleaningTask = true;
                    Debug.Log("Cleaning task COMPLETE: all zones touched.");
                }
                break;
            }
        }
    }

}
