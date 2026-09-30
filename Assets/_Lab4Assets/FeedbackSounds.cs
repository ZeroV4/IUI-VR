using UnityEngine;

// [Usability] New script. Shared "success" and "error" sounds that every task can use.
// The clips are set once here, and tasks call Success() or Error().
public class FeedbackSounds : MonoBehaviour
{
    [Header("Hook these up")]
    public AudioSource source;          // AudioSource on the same object (Play On Awake off)
    public AudioClip successClip;
    public AudioClip errorClip;

    [Header("Behavior")]
    [Tooltip("A lower pitch makes a reused UI sound sound like 'wrong'.")]
    public float errorPitch = 0.5f;

    public void Success()
    {
        Debug.Log("[FeedbackSounds] success");
        Play(successClip, 1f);
    }

    public void Error()
    {
        Debug.Log("[FeedbackSounds] error");
        Play(errorClip, errorPitch);
    }

    void Play(AudioClip clip, float pitch)
    {
        if (!source || !clip) return;
        source.pitch = pitch;
        source.PlayOneShot(clip);
    }
}
