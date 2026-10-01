using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables; // XRGrabInteractable
using TMPro;

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

    // usability: text next to the cup that shows how full it is
    public TMP_Text fillText;

    // State
    float pouringSeconds;
    bool pouring;

    void Update()
    {
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
        if (pouring && RayHitsCupMouth())
        {
            pouringSeconds += Time.deltaTime;
            if (pouringSeconds >= requiredSeconds)
            {
                IsComplete = true;
                SetParticles(false);
                Debug.Log("Coffee task COMPLETE");
                // usability: sound when the cup is full, you could not tell when to stop pouring
                GetComponent<AudioSource>().Play();
            }
            ShowFill();
        }
        // usability: we dont empty the cup anymore when the stream misses it for a moment, you keep what you poured
    }

    // usability: shows 0 to 100 percent while pouring, the coffee reset button calls this too so it goes back to 0
    public void ShowFill()
    {
        if (!fillText) return;

        if (IsComplete)
        {
            fillText.text = "Coffee ready!";
            fillText.color = Color.green;
        }
        else
        {
            int percent = Mathf.Min(100, Mathf.RoundToInt(pouringSeconds / requiredSeconds * 100f));
            fillText.text = $"Coffee {percent}%";
            fillText.color = Color.white;
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
        // usability: the coffee falls straight down so we check straight down too
        // the spout direction could touch the cup from the side, so tilting or jerking the pot filled it without coffee going in
        Vector3 dir = Vector3.down;
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
