using UnityEngine;

// DO NOT CHANGE THIS FILE
public class TrashRespawner : MonoBehaviour
{
    [Header("Assign these in Inspector")]
    public GameObject canPrefab;
    public Transform canSpawn;

    public GameObject bottlePrefab;
    public Transform bottleSpawn;

    // DO NOT CHANGE
    void Start()
    {
        RespawnAll(); // spawn once at start
    }

    // DO NOT CHANGE
    public void RespawnAll()
    {
        // Remove existing trash
        foreach (var existing in GameObject.FindGameObjectsWithTag("Trash"))
            Destroy(existing);

        // Spawn can
        if (canPrefab && canSpawn)
            Instantiate(canPrefab, canSpawn.position, canSpawn.rotation);

        // Spawn bottle
        if (bottlePrefab && bottleSpawn)
            Instantiate(bottlePrefab, bottleSpawn.position, bottleSpawn.rotation);
    }
}