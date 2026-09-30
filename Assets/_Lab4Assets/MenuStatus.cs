using TMPro;
using UnityEngine;

// usability: new script on UI Root/Buttons. after Start the long instructions turn into a task checklist
// so you can see what is done and what is left
// quit hides Buttons, that stops this Update so the summary can use the same text
public class MenuStatus : MonoBehaviour
{
    [Header("Hook these up")]
    public GameRunController runController;
    public TMP_Text instructionsText;      // UI Root/MainText

    [Header("Tasks")]
    public DrawerTask drawerA;
    public DrawerTask drawerB;
    public TrashBinScorer trashTask;
    public CoffeeTask coffeeTask;
    public CleaningTask cleaningTask;

    float nextRefreshTime;

    void Update()
    {
        if (!runController || !runController.Started || !instructionsText) return;

        // usability: 5 times a second is enough, no need to rebuild the text every frame on the quest
        if (Time.time < nextRefreshTime) return;
        nextRefreshTime = Time.time + 0.2f;

        instructionsText.text = BuildChecklist();
    }

    string BuildChecklist()
    {
        string text = "<b>Tasks</b>\n\n";
        bool allDone = true;

        if (drawerA)
        {
            text += Line(drawerA.IsComplete, DrawerLabel(drawerA));
            if (!drawerA.IsComplete) allDone = false;
        }
        if (drawerB)
        {
            text += Line(drawerB.IsComplete, DrawerLabel(drawerB));
            if (!drawerB.IsComplete) allDone = false;
        }
        if (trashTask)
        {
            text += Line(trashTask.IsComplete, "Throw the can and the bottle in the bin");
            if (!trashTask.IsComplete) allDone = false;
        }
        if (coffeeTask)
        {
            text += Line(coffeeTask.IsComplete, "Pour a full cup of coffee");
            if (!coffeeTask.IsComplete) allDone = false;
        }
        if (cleaningTask)
        {
            text += Line(cleaningTask.IsComplete, "Wipe the 5 spots along the desk");
            if (!cleaningTask.IsComplete) allDone = false;
        }

        if (allDone)
            text += "\n<color=#00FF00><b>All done! Press Quit.</b></color>";
        else
            text += "\nPress Quit when all tasks are done.";
        return text;
    }

    // usability: done tasks get a green [x], the rest stay [ ]
    static string Line(bool complete, string label)
    {
        if (complete)
            return $"<color=#00FF00>[x] {label}</color>\n";
        return $"[ ] {label}\n";
    }

    static string DrawerLabel(DrawerTask drawer)
    {
        if (drawer.expectedType == FileType.LightGreen)
            return "Light green files into the light green drawer";
        return "Dark green files into the dark green drawer";
    }
}
