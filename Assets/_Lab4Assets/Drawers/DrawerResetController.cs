using UnityEngine;

// DO NOT CHANGE THIS FILE
public class DrawerTaskResetController : MonoBehaviour
{
    [Header("Assign these in Inspector")]
    public DrawerTask drawerTask1;  // drag a drawer here from the inspector
    public DrawerTask drawerTask2;  // drag another drawer       
    public FileStackSpawner stackSpawner; // drag an empty parent with a FileStackSpawner script attached to it

    [ContextMenu("Reset Task")]
    public void ResetTask()
    {   
        // reset filestack, then the drawers itself
        if (stackSpawner) stackSpawner.ResetState();
        if (drawerTask1)   drawerTask1.ResetState();
        if (drawerTask2)   drawerTask2.ResetState();
        Debug.Log("[DrawerReset] Task reset.");
    }
}
