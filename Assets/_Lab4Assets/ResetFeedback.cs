using TMPro;
using UnityEngine;

// [Usability] New script. Shows a short "<task> reset ✓" message on the menu,
// so the player can see that a Reset button actually did something.
// Each Reset button calls Show("<task name>") from its OnClick list.
public class ResetFeedback : MonoBehaviour
{
    [Header("Hook these up")]
    public TMP_Text statusText;          // the ResetStatus text under Buttons

    [Header("Behavior")]
    public float visibleSeconds = 2f;    // how long the message stays

    void Awake()
    {
        if (statusText) statusText.text = "";
    }

    public void Show(string taskName)
    {
        Debug.Log($"[ResetFeedback] {taskName} reset");
        if (!statusText) return;

        statusText.text = $"{taskName} reset ✓";

        // restart the timer if another Reset is pressed while the message is showing
        CancelInvoke(nameof(Hide));
        Invoke(nameof(Hide), visibleSeconds);
    }

    void Hide()
    {
        statusText.text = "";
    }
}
