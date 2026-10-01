using UnityEngine;
using TMPro;

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

    // usability: text above the desk that shows how many spots are clean
    public TMP_Text progressText;

    // usability: brown dirt spot on each zone, same order as targets, it disappears when you clean it
    public GameObject[] dirtSpots;

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
                // usability: sound when a spot gets cleaned so you know it counted
                GetComponent<AudioSource>().Play();
                if (i < dirtSpots.Length) dirtSpots[i].SetActive(false);
                Debug.Log("Touched a cleaning spot");

                if (touchedCount == targets.Length)
                {
                    cleaningTask = true;
                    Debug.Log("Cleaning task COMPLETE: all zones touched.");
                }
                ShowProgress();
                break;
            }
        }
    }

    // usability: reset only moved the sponge back, this clears the cleaned spots too
    public void ResetProgress()
    {
        for (int i = 0; i < touched.Length; i++)
        {
            touched[i] = false;
            // usability: bring the dirt back so you can clean again
            if (i < dirtSpots.Length) dirtSpots[i].SetActive(true);
        }
        touchedCount = 0;
        cleaningTask = (touched.Length == 0);
        ShowProgress();
    }

    // usability: shows 2/5 while cleaning and goes green when the whole desk is done
    void ShowProgress()
    {
        if (!progressText) return;

        if (cleaningTask)
        {
            progressText.text = "Desk clean!";
            progressText.color = Color.green;
        }
        else
        {
            progressText.text = $"Desk {touchedCount}/{touched.Length} clean";
            progressText.color = Color.white;
        }
    }

}
