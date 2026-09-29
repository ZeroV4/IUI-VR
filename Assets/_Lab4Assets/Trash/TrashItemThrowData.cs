using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// DO NOT CHANGE FILE
[RequireComponent(typeof(Rigidbody), typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class TrashItemThrowData : MonoBehaviour
{
    public Vector3 releasePos;
    public Vector3 releaseVel;
    public float releaseTime;

    Rigidbody rb;
    UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;

    // DO NOT CHANGE
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
    }

    // DO NOT CHANGE
    void OnEnable()  => grab.selectExited.AddListener(OnRelease);
    void OnDisable() => grab.selectExited.RemoveListener(OnRelease);

    // DO NOT CHANGE
    void OnRelease(SelectExitEventArgs args)
    {
        releasePos = transform.position;
        releaseVel = rb.linearVelocity;
        releaseTime = Time.time;
        Debug.Log($"Trash: released speed {releaseVel.magnitude:F2}");
    }
}
