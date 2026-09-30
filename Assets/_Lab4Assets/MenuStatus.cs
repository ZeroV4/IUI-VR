using TMPro;
using UnityEngine;
using UnityEngine.UI;

// [Usability] New script. Lives on UI Root/Buttons.
// 1) The Reset buttons are greyed out until Start. Before, pressing Drawers Reset before Start
//    spawned files that were never locked.
// 2) After Start, the long instructions are replaced by a live task checklist with progress.
// Quit hides Buttons, which stops this Update, so RunSummaryOnQuit can write its summary into the same text.
public class MenuStatus : MonoBehaviour
{
    [Header("Hook these up")]
    public GameRunController runController;
    public TMP_Text instructionsText;      // UI Root/MainText
    public Button[] resetButtons;          // the 4 Reset buttons

    [Header("Tasks")]
    public DrawerTask drawerA;
    public DrawerTask drawerB;
    public TrashBinScorer trashTask;
    public CoffeeTask coffeeTask;
    public CleaningTask cleaningTask;

    bool unlocked;
    string lastText;
    float nextRefreshTime;

    void Start()
    {
        SetResetButtons(false);
    }

    void Update()
    {
        if (!runController || !runController.Started) return;

        if (!unlocked)
        {
            unlocked = true;
            SetResetButtons(true);
        }

        if (!instructionsText) return;

        // rebuilding the text 5 times per second is enough, and cheaper on the Quest than every frame
        if (Time.time < nextRefreshTime) return;
        nextRefreshTime = Time.time + 0.2f;

        string text = BuildChecklist();
        if (text == lastText) return; // only touch the text when something changed
        lastText = text;
        instructionsText.text = text;
    }

    void SetResetButtons(bool interactable)
    {
        foreach (var button in resetButtons)
        {
            if (button) button.interactable = interactable;
        }
        Debug.Log(interactable ? "[MenuStatus] Reset buttons enabled" : "[MenuStatus] Reset buttons disabled until Start");
    }

    string BuildChecklist()
    {
        string text = "<b>Tasks</b>\n\n";
        int done = 0;
        int total = 0;

        if (drawerA)
        {
            total++;
            if (drawerA.IsComplete) done++;
            text += Line(drawerA.IsComplete, DrawerLabel(drawerA), $"{drawerA.MatchedCount}/{drawerA.requiredCount}");
        }
        if (drawerB)
        {
            total++;
            if (drawerB.IsComplete) done++;
            text += Line(drawerB.IsComplete, DrawerLabel(drawerB), $"{drawerB.MatchedCount}/{drawerB.requiredCount}");
        }
        if (trashTask)
        {
            total++;
            if (trashTask.IsComplete) done++;
            text += Line(trashTask.IsComplete, "Throw the can and the bottle in the bin", $"{trashTask.score}/{TrashBinScorer.RequiredScore}");
        }
        if (coffeeTask)
        {
            total++;
            if (coffeeTask.IsComplete) done++;
            text += Line(coffeeTask.IsComplete, "Pour a full cup of coffee", $"{Mathf.FloorToInt(coffeeTask.Progress01 * 100f)}%");
        }
        if (cleaningTask)
        {
            total++;
            if (cleaningTask.IsComplete) done++;
            text += Line(cleaningTask.IsComplete, "Wipe the brown spots with the sponge", $"{cleaningTask.TouchedCount}/{cleaningTask.TargetCount}");
        }

        if (done == total)
            text += "\n<color=#00FF00><b>All done! Press Quit.</b></color>";
        else
            text += $"\n{done}/{total} done. Press Quit when all tasks are done.";
        return text;
    }

    // One checklist line: green with [x] when done, otherwise [ ] with the progress in grey
    static string Line(bool complete, string label, string progress)
    {
        if (complete)
            return $"<color=#00FF00>[x] {label}</color>\n";
        return $"[ ] {label}  <color=#AAAAAA>{progress}</color>\n";
    }

    static string DrawerLabel(DrawerTask drawer)
    {
        if (drawer.expectedType == FileType.LightGreen)
            return "Light green files into the LIGHT GREEN drawer";
        return "Dark green files into the DARK GREEN drawer";
    }
}
