using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// [Usability] Editor-only smoke test. Runs the scene in Play mode without a headset, fakes the
// task events (trigger enters, pour time, button clicks) and saves pictures + a log of what happened.
// Run it with "Tools > Usability > Play Mode Smoke Test" or from the command line (-executeMethod UsabilityPlayTest.Run).
[InitializeOnLoad]
public static class UsabilityPlayTest
{
    const string RunningKey = "UsabilityPlayTest.Running";
    static float startTime;
    static int step;
    static int errorCount;
    static readonly List<string> report = new List<string>();

    static UsabilityPlayTest()
    {
        // Entering Play mode reloads scripts, so the test continues from here
        if (!SessionState.GetBool(RunningKey, false)) return;
        EditorApplication.update += Tick;
        Application.logMessageReceived += OnLog;
    }

    [MenuItem("Tools/Usability/Play Mode Smoke Test")]
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/terribleOffice.unity");
        SessionState.SetBool(RunningKey, true);
        // needed when "Enter Play Mode Options" skip the script reload
        EditorApplication.update += Tick;
        Application.logMessageReceived += OnLog;
        EditorApplication.EnterPlaymode();
    }

    static void OnLog(string message, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception)
        {
            // OpenXR has no runtime on Linux; that error is expected
            if (message.Contains("OpenXR") || message.Contains("XR")) return;
            errorCount++;
            report.Add("ERROR: " + message);
        }
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying) return;
        if (step == 0) { startTime = Time.realtimeSinceStartup; step = 1; }
        float t = Time.realtimeSinceStartup - startTime;

        var gm = GameObject.Find("GameManager");
        var run = gm.GetComponent<GameRunController>();
        var tracker = gm.GetComponent<ProgressTracker>();
        var summary = gm.GetComponent<RunSummaryOnQuit>();
        var menu = Object.FindAnyObjectByType<MenuStatus>();

        if (step == 1 && t > 2f)
        {
            Check("Reset buttons locked before Start", !menu.resetButtons[0].interactable);
            Check("Trash shows 0/2 before Start", tracker.trashTask.statusText.text == "Trash 0/2");
            Check("Drawer shows 0/4", tracker.drawerA.countText.text == "0/4");
            var drawerLabel = tracker.drawerA.countText.transform.parent.Find("DrawerLabel");
            Check("Drawer counter sits under its label", tracker.drawerA.countText.transform.position.y < drawerLabel.position.y - 0.08f);
            Snap("play_1_menu_before_start", MenuView());
            run.StartRun();
            step = 2;
        }
        else if (step == 2 && t > 3.5f)
        {
            Check("Reset buttons unlocked after Start", menu.resetButtons[0].interactable);
            Check("Checklist replaced the instructions", menu.instructionsText.text.Contains("[ ]"));

            // Non-trash item in the bin
            var apple = GameObject.Find("Food_Apple_Red");
            if (apple) tracker.trashTask.SendMessage("OnTriggerEnter", apple.GetComponentInChildren<Collider>());
            Check("Apple in bin gives a message", tracker.trashTask.statusText.text == "Only the can and bottle count");
            Snap("play_2_menu_checklist", MenuView());
            Snap("play_2_trash_message", BinView());
            step = 3;
        }
        else if (step == 3 && t > 6f)
        {
            Check("Trash message went back to the count", tracker.trashTask.statusText.text == "Trash 0/2");

            // Wrong file in a drawer
            tracker.drawerA.SendMessage("ShowWrongInsert");
            Check("Drawer shows wrong-colour message", tracker.drawerA.countText.text.StartsWith("Wrong colour"));
            Snap("play_3_drawer_wrong", new Pose(new Vector3(-0.85f, 1.0f, -0.05f), new Vector3(-0.3f, 0.25f, -0.05f)));

            // Wipe 2 of the 5 spots
            var cleaning = tracker.cleaningTask;
            cleaning.SendMessage("OnTriggerEnter", cleaning.targets[0]);
            cleaning.SendMessage("OnTriggerEnter", cleaning.targets[1]);
            Check("2 spots wiped", cleaning.TouchedCount == 2);
            Check("Wiped spot's dirt is hidden", !cleaning.targets[0].transform.GetChild(0).gameObject.activeSelf);

            // Half-poured coffee
            SetPrivate(tracker.coffeeTask, "pouringSeconds", tracker.coffeeTask.requiredSeconds * 0.4f);
            step = 4;
        }
        else if (step == 4 && t > 7f)
        {
            Check("Coffee text shows progress", tracker.coffeeTask.statusText.text == "Coffee 40%");
            Check("Coffee level visible", tracker.coffeeTask.coffeeLevel.gameObject.activeSelf);
            Snap("play_4_cup", new Pose(new Vector3(-0.45f, 1.15f, -0.8f), new Vector3(-0.13f, 0.82f, -0.895f)));
            Snap("play_4_desk", new Pose(new Vector3(-0.05f, 2.5f, -0.07f), new Vector3(-0.05f, 0.7f, -0.0701f)));
            Snap("play_4_menu_progress", MenuView());

            // Finish the coffee
            typeof(CoffeeTask).GetProperty("IsComplete").GetSetMethod(true).Invoke(tracker.coffeeTask, new object[] { true });
            step = 5;
        }
        else if (step == 5 && t > 8f)
        {
            Check("Coffee text says ready", tracker.coffeeTask.statusText.text == "Coffee ready!");
            Snap("play_5_cup_done", new Pose(new Vector3(-0.45f, 1.15f, -0.8f), new Vector3(-0.13f, 0.82f, -0.895f)));

            // Press the Reset buttons (same listeners as a real click)
            foreach (var b in menu.resetButtons) b.onClick.Invoke();
            step = 6;
        }
        else if (step == 6 && t > 9f)
        {
            Check("Cleaning reset: 0 spots", tracker.cleaningTask.TouchedCount == 0);
            Check("Cleaning reset: dirt back", tracker.cleaningTask.targets[0].transform.GetChild(0).gameObject.activeSelf);
            Check("Coffee reset: empty", tracker.coffeeTask.Progress01 == 0f && !tracker.coffeeTask.coffeeLevel.gameObject.activeSelf);
            Check("Trash reset: 0/2", tracker.trashTask.statusText.text == "Trash 0/2");
            Check("Reset confirmation shown", GameObject.Find("ResetStatus").GetComponent<TMP_Text>().text.Contains("reset"));
            Snap("play_6_after_reset", MenuView());

            summary.ConfirmThenQuit();
            Check("Quit asks for confirmation", summary.quitButtonLabel.text.StartsWith("Press again"));
            Snap("play_6_quit_confirm", MenuView());
            step = 7;
        }
        else if (step == 7 && t > 13f)
        {
            Check("Quit label back after 3 s", summary.quitButtonLabel.text == "Quit");
            summary.ConfirmThenQuit();
            summary.ConfirmThenQuit();
            step = 8;
        }
        else if (step == 8 && t > 14f)
        {
            Check("Summary shown after 2 presses", summary.instructionsText.text.Contains("Run Summary"));
            Snap("play_8_summary", MenuView());
            Finish();
        }
        else if (t > 40f)
        {
            report.Add("TIMEOUT at step " + step);
            Finish();
        }
    }

    static void Finish()
    {
        report.Add($"Console errors: {errorCount}");
        Debug.Log("[PlayTest]\n" + string.Join("\n", report));
        SessionState.SetBool(RunningKey, false);
        EditorApplication.update -= Tick;
        if (Application.isBatchMode) EditorApplication.Exit(0);
        else EditorApplication.ExitPlaymode();
    }

    static void Check(string what, bool ok)
    {
        report.Add((ok ? "PASS " : "FAIL ") + what);
    }

    static void SetPrivate(object target, string field, object value)
    {
        target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    }

    struct Pose
    {
        public Vector3 from, lookAt;
        public Pose(Vector3 from, Vector3 lookAt) { this.from = from; this.lookAt = lookAt; }
    }

    static Pose MenuView() => new Pose(new Vector3(-0.131f - 0.487f * 1.3f, 1.55f, 1.493f - 0.873f * 1.3f), new Vector3(-0.131f, 1.6f, 1.493f));
    static Pose BinView() => new Pose(new Vector3(-0.68f, 1.6f, -0.07f), new Vector3(1.54f, 0.6f, -0.867f));

    static void Snap(string name, Pose pose)
    {
        string dir = System.Environment.GetEnvironmentVariable("SNAP_DIR");
        if (string.IsNullOrEmpty(dir)) dir = "Temp/Snapshots";
        Directory.CreateDirectory(dir);

        Canvas.ForceUpdateCanvases();
        var go = new GameObject("SnapCam");
        var cam = go.AddComponent<Camera>();
        cam.fieldOfView = 70f;
        cam.nearClipPlane = 0.02f;
        var rt = new RenderTexture(1280, 800, 24);
        cam.targetTexture = rt;
        cam.transform.position = pose.from;
        cam.transform.LookAt(pose.lookAt);
        cam.Render();

        RenderTexture.active = rt;
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());

        cam.targetTexture = null;
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
    }
}
