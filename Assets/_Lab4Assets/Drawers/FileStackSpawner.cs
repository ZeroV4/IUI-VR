using System.Collections.Generic;
using UnityEngine;

// DO NOT CHANGE THIS FILE NOR ITS SETTINGS
public class FileStackSpawner : MonoBehaviour
{
    [Header("DO NOT CHANGE THIS FILE NOR ITS SETTINGS")]
    [Header("Prefabs & Counts")]
    [SerializeField] private GameObject type1Prefab;
    [SerializeField] private GameObject type2Prefab;
    [SerializeField, Min(0)] private int type1Count = 4;
    [SerializeField, Min(0)] private int type2Count = 4;

    [Header("Placement")]
    [SerializeField] private Transform stackAnchor;      // location where the files will be spawned
    [SerializeField] private float heightStep = 0.02f;   // distance between files
    [SerializeField] private Vector2 yawJitterDeg = new(-2f, 2f);
    [SerializeField] private Vector2 tiltJitterDeg = new(-0.5f, 0.5f);

    [Header("Runtime")]
    [SerializeField] private bool spawnOnStart = false;  // keep false, Spawn() is called from Start button
    [SerializeField] private bool clearExistingBeforeSpawn = true;

    // DO NOT CHANGE THIS METHOD
    private void Reset() => stackAnchor = transform;

    // DO NOT CHANGE THIS METHOD
    private void Start()
    {
        if (spawnOnStart) Spawn();
    }

    [ContextMenu("Spawn Stack")]
    // Method that spawns the files in a stack
    // DO NOT CHANGE THIS METHOD
    public void Spawn()
    {   
        // Ensure that we have a location to spawn the files at
        if (!stackAnchor) stackAnchor = transform;

        // Ensure the prefabs (files) are assigned
        if ((type1Count > 0 && !type1Prefab) || (type2Count > 0 && !type2Prefab))
        {
            Debug.LogError("Assign prefabs for any type with count > 0.");
            return;
        }

        // Clear current files, in case we are re-spawning (reset)
        if (clearExistingBeforeSpawn) ClearSpawned();

        // Build and shuffle the list
        var files = new List<GameObject>(type1Count + type2Count);
        for (int i = 0; i < type1Count; i++) files.Add(type1Prefab);
        for (int i = 0; i < type2Count; i++) files.Add(type2Prefab);
        for (int i = files.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (files[i], files[j]) = (files[j], files[i]);
        }

        // Create the stack and add some rotation to make it more stack-y
        for (int i = 0; i < files.Count; i++)
        {
            Vector3 pos = stackAnchor.position + Vector3.up * (i * heightStep);
            float yaw   = Random.Range(yawJitterDeg.x, yawJitterDeg.y) + 90f;
            float pitch = Random.Range(tiltJitterDeg.x, tiltJitterDeg.y) + 90f;
            float roll  = Random.Range(tiltJitterDeg.x, tiltJitterDeg.y);
            Quaternion rot = stackAnchor.rotation * Quaternion.Euler(pitch, yaw, roll);

            var go = Instantiate(files[i], pos, rot, stackAnchor);
            go.SetActive(true);

            var rb = go.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                rb.Sleep();
            }
        }
    }

    [ContextMenu("Clear Spawned Children")]
    // Makes sure that all the existing files are deleted (not for the ones already in the drawer)
    // DO NOT CHANGE THIS METHOD
    public void ClearSpawned()
    {   
        var anchor = stackAnchor ? stackAnchor : transform;

        // Collect first to avoid modifying while iterating
        var toDestroy = new List<GameObject>();
        foreach (Transform child in anchor) toDestroy.Add(child.gameObject);
        foreach (var go in toDestroy) Destroy(go);
    }

    // resets the state, do not change this method
    public void ResetState()
    {
        ClearSpawned();
        Spawn();
    }

}
