using UnityEngine;

/**
This class handles the reset of the CleaningTask by
- resetting the position of the sponge

DO NOT CHANGE THIS FILE
*/
public class CleaningTaskController : MonoBehaviour
{
    [Header("DO NOT CHANGE ANY PARAMETERS HERE")]
    [Header("Reference to cleaning task")]
    public CleaningTask task;

    [Header("Fields for Sponge reset")]
    public Transform sponge;      // the sponge object
    public Transform spongeHome;  // empty transform where the sponge should reset to

    [ContextMenu("Reset Cleaning Task")]
    public void ResetTask()
    {

        // 2) Snap sponge back home (and clear physics)
        if (sponge && spongeHome)
        {   
            // Get the RigidBody
            var rb = sponge.GetComponent<Rigidbody>();

            // Set the speed to zero and sleep (no collision detection or simulation will be performed anymore)
            if (rb)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.Sleep();
            }

            // set to original location
            sponge.SetPositionAndRotation(spongeHome.position, spongeHome.rotation);

            // "wake up" again
            if (rb) rb.WakeUp();
        }

        Debug.Log("[CleaningReset] Progress cleared and sponge reset.");
    }
}
