using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables; // XRGrabInteractable

public class CoffeeTask : MonoBehaviour
{
    [Header("XR Objects")]
    public XRGrabInteractable mokaPot;
    public XRGrabInteractable cup;

    [Header("Spawn Points, assign in editor")]
    public Transform potSpawn;
    public Transform cupSpawn;

    [Header("Pour Setup")]
    public Transform spoutTip;            // child at the nozzle (blue arrow = flow dir)
    public ParticleSystem pourParticles;  // particle system under spoutTip
    public Collider cupMouth;             // trigger collider at cup opening
    public float rayDistance = 0.35f;     // stream length

    [Header("Angles")]
    public float angleOnDeg = 25;        // start pouring when <= this to DOWN
    public float angleOffDeg = 45;       // keep pouring when <= this

    [Header("Completion")]
    public float requiredSeconds = 0.3f;    // time hitting cup to complete
    public bool IsComplete { get; private set; }

    // [Usability] 0 = empty cup, 1 = full cup (the menu checklist reads it)
    public float Progress01
    {
        get
        {
            if (IsComplete) return 1f;
            if (requiredSeconds <= 0f) return 0f;
            return Mathf.Clamp01(pouringSeconds / requiredSeconds);
        }
    }

    // [Usability] Feedback shown to the player in the world (logs are not visible in the headset)
    [Header("Feedback (added for usability)")]
    public Transform coffeeLevel;     // empty object at the inside bottom of the cup; its Y scale grows while pouring
    public TMP_Text statusText;       // "Coffee 40%" / "Coffee ready!" above the cup
    public FeedbackSounds sounds;     // shared success / error sounds

    // State
    float pouringSeconds;
    bool pouring;

    // [Usability] What the feedback currently shows, so we only update it when something changed
    Vector3 levelFullScale = Vector3.one;
    int shownPercent = -1;

    void Awake()
    {
        // [Usability] The scale set in the Editor is the "full cup" scale
        if (coffeeLevel) levelFullScale = coffeeLevel.localScale;
        if (statusText) statusText.text = "";
    }

    void Update()
    {
        // [Usability] Runs first, also after ResetTask (locked), so the visuals always follow the state
        UpdateFeedback();

        if (IsComplete || !mokaPot || !spoutTip || !pourParticles) return;

        bool held = mokaPot.isSelected;

        // Decide if we should pour (held + angle with hysteresis)
        bool targetPour = false;
        if (held)
        {
            float angleToDown = Vector3.Angle(StreamDir(), Vector3.down);
            targetPour = !pouring ? angleToDown <= angleOnDeg   // start
                                  : angleToDown <= angleOffDeg; // keep
        }

        if (targetPour != pouring)
        {
            pouring = targetPour;
            SetParticles(pouring); 
        }

        // Count time only while pouring AND ray hits the cup mouth
        // [Usability] Progress is no longer wiped when the stream misses the cup for a moment.
        // Only ResetTask (the Coffee Reset button) sets pouringSeconds back to 0.
        if (pouring && RayHitsCupMouth())
        {
            pouringSeconds += Time.deltaTime;
            if (pouringSeconds >= requiredSeconds)
            {
                IsComplete = true;
                SetParticles(false);
                Debug.Log("Coffee task COMPLETE");
            }
        }
    }

    // [Usability] Before, the player could not see how much longer to pour, or whether the task finished.
    // Now the coffee rises in the cup and a text above the cup shows the progress.
    void UpdateFeedback()
    {
        // 100 only when the task is really complete (99.6% must not show as 100%)
        int percent = IsComplete ? 100 : Mathf.Min(99, Mathf.FloorToInt(Progress01 * 100f));
        if (percent == shownPercent) return;

        bool justCompleted = percent == 100 && shownPercent != -1;
        shownPercent = percent;

        // coffee level in the cup
        if (coffeeLevel)
        {
            coffeeLevel.gameObject.SetActive(percent > 0);
            coffeeLevel.localScale = new Vector3(levelFullScale.x, levelFullScale.y * Progress01, levelFullScale.z);
        }

        // text above the cup
        if (statusText)
        {
            if (IsComplete)
            {
                statusText.text = "Coffee ready!";
                statusText.color = Color.green;
            }
            else if (percent > 0)
            {
                statusText.text = $"Coffee {percent}%";
                statusText.color = Color.white;
            }
            else
            {
                statusText.text = "";
            }
        }

        if (justCompleted)
        {
            Debug.Log("Coffee: feedback ready");
            if (sounds) sounds.Success();
        }
    }

    // Coffee Task reset
    // DO NOT CHANGE
    public void ResetTask()
    {
        // 1) Drop if held
        ForceRelease(mokaPot);
        ForceRelease(cup);

        // 2) Stop & clear particles
        if (pourParticles)
            pourParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // 3) Reset state
        pouring = false;
        pouringSeconds = 0f;
        IsComplete = false;

        // 4) Respawn to spawn points
        if (mokaPot && potSpawn)
            mokaPot.transform.SetPositionAndRotation(potSpawn.position, potSpawn.rotation);
        if (cup && cupSpawn)
            cup.transform.SetPositionAndRotation(cupSpawn.position, cupSpawn.rotation);

        // 5) Zero physics
        ZeroBody(mokaPot ? mokaPot.GetComponent<Rigidbody>() : null);
        ZeroBody(cup     ? cup.GetComponent<Rigidbody>()     : null);

        Debug.Log("Coffee task RESET");
    }

    // ===== Helpers =====

    // DO NOT CHANGE
    Vector3 StreamDir() => spoutTip.forward; // blue axis

    // check whether the ray from the sprouttip hits the triggercollider
    bool RayHitsCupMouth()
    {
        if (!cupMouth) return false;
        Vector3 dir = StreamDir();
        Vector3 origin = spoutTip.position + dir * 0.01f; // avoid hitting our own pot
        return Physics.Raycast(origin, dir, out var hit, rayDistance, ~0, QueryTriggerInteraction.Collide)
               && hit.collider == cupMouth;
    }

    // turn particles on/off
    void SetParticles(bool play)
    {
        if (!pourParticles) return;
        if (play && !pourParticles.isPlaying) pourParticles.Play(true);
        if (!play && pourParticles.isPlaying)  pourParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    // force the release of the coffee cup
    void ForceRelease(XRGrabInteractable grab)
    {
        if (!grab || !grab.isSelected) return;
        var im = grab.interactionManager;
        // Cleanly end all selections (drop)
        for (int i = grab.interactorsSelecting.Count - 1; i >= 0; i--)
        {
            var interactor = grab.interactorsSelecting[i];
            im?.SelectExit(interactor, grab);
        }
    }

    // stop movement
    // DO NOT CHANGE
    void ZeroBody(Rigidbody rb)
    {
        if (!rb) return;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.Sleep();
    }
}
