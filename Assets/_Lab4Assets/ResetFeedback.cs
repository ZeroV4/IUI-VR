using TMPro;
using UnityEngine;

// usability: new script. shows "task reset" on the menu for a moment
// so you can see the reset button actually did something
// each reset button calls Show("task name") from its OnClick list
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
        if (!statusText) return;

        // usability: plain text, the font has no tick mark and showed a box
        statusText.text = $"{taskName} reset";

        // start the timer again if you press another reset while the message is up
        CancelInvoke(nameof(Hide));
        Invoke(nameof(Hide), visibleSeconds);
    }

    void Hide()
    {
        statusText.text = "";
    }
}
