using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// usability: one-off helper that does the same inspector wiring you would do by hand
// run it once, after that you can delete this file or the whole Editor folder, the scene keeps everything
public static class UsabilitySceneSetup
{
    const string ScenePath = "Assets/Scenes/terribleOffice.unity";
    const string ClickClip = "Assets/_Lab4Assets/Audio/Button_22_click.wav";
    const string PopClip = "Assets/_Lab4Assets/Audio/Success_Pop.wav";
    const string ErrorClip = "Assets/_Lab4Assets/X - Others/VRTemplateAssets/Audio/Button_14_hover.wav";

    [MenuItem("Tools/Usability/Apply Scene Setup")]
    public static void Apply()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);

        var gameManager = GameObject.Find("GameManager");
        var tracker = gameManager.GetComponent<ProgressTracker>();
        var runController = gameManager.GetComponent<GameRunController>();
        var summary = gameManager.GetComponent<RunSummaryOnQuit>();

        SetupCleaning(tracker.cleaningTask);
        SetupDrawer(tracker.drawerA);
        SetupDrawer(tracker.drawerB);
        SetupCoffee(tracker.coffeeTask);
        SetupTrash(tracker.trashTask);
        SetupMenu(tracker, runController, summary);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[UsabilitySetup] Scene saved.");
    }

    static void SetupCleaning(CleaningTask cleaning)
    {
        AddSound(cleaning.gameObject, ClickClip, 1f);

        // same spot as the old sponge hint, above the desk next to the sponge
        var parent = Object.FindAnyObjectByType<CleaningTaskController>().transform;
        var text = WorldText(parent, "CleaningProgress", new Vector2(0.22f, 0.05f), "Desk 0/5 clean", Color.white);
        text.transform.parent.localPosition = new Vector3(-1.2572396f, 1.0184f, -1.293f);
        text.transform.parent.localRotation = new Quaternion(0.1648399f, 0.83498806f, -0.38038853f, 0.36183888f);
        text.transform.parent.localScale = Vector3.one;

        cleaning.progressText = text;
        EditorUtility.SetDirty(cleaning);
    }

    static void SetupDrawer(DrawerTask drawer)
    {
        // drawer front in the same colour as the files that go in it
        string material = drawer.expectedType == FileType.LightGreen
            ? "Assets/_Lab4Assets/Drawers/FileMaterialLightGreen.mat"
            : "Assets/_Lab4Assets/Drawers/FileMaterialGreen.mat";
        var front = drawer.transform.Find("Drawer_itself/Front").GetComponent<Renderer>();
        front.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(material);
        EditorUtility.SetDirty(front);

        // colour name on the front, same spot as the first labels we had
        bool light = drawer.expectedType == FileType.LightGreen;
        var label = WorldText(drawer.transform.Find("Drawer_itself"), "DrawerLabel", new Vector2(0.33f, 0.07f),
            light ? "LIGHT GREEN" : "DARK GREEN", light ? Color.black : Color.white);
        label.transform.parent.localPosition = new Vector3(-0.0515553f, 0f, -0.0525f);
        label.transform.parent.localRotation = new Quaternion(0f, 0.7071068f, 0f, 0.7071068f);
        label.transform.parent.localScale = Vector3.one * 0.2222222f;

        AddSound(drawer.gameObject, ErrorClip, 0.5f);
    }

    static void SetupCoffee(CoffeeTask coffee)
    {
        coffee.angleOnDeg = 45;
        coffee.angleOffDeg = 60;
        EditorUtility.SetDirty(coffee);

        AddSound(coffee.gameObject, PopClip, 1f);
    }

    static void SetupTrash(TrashBinScorer trash)
    {
        AddSound(trash.gameObject, PopClip, 1f);

        // same spot as the first trash sign, above the bin
        var text = WorldText(trash.transform.parent, "TrashProgress", new Vector2(0.52f, 0.17f), "Trash 0/2", Color.white);
        text.transform.parent.localPosition = new Vector3(1.9686993f, -0.22961408f, -0.41918948f);
        text.transform.parent.localRotation = new Quaternion(0.07588888f, 0.8107463f, -0.10787606f, 0.5703456f);
        text.transform.parent.localScale = Vector3.one;

        trash.progressText = text;
        EditorUtility.SetDirty(trash);
    }

    static void SetupMenu(ProgressTracker tracker, GameRunController runController, RunSummaryOnQuit summary)
    {
        var uiRoot = GameObject.Find("UI Root");
        var buttons = uiRoot.transform.Find("Buttons");
        var mainText = uiRoot.transform.Find("MainText").GetComponent<TMP_Text>();

        // the window glass was drawn over the menu, this draws the menu last
        var canvas = uiRoot.GetComponent<Canvas>();
        canvas.sortingOrder = 10;
        EditorUtility.SetDirty(canvas);

        // the top of the menu is solid but the bottom background is 90 percent, so you could see through half of it
        // copy of the bottom background at full colour, the original background stays as it is
        var background = uiRoot.transform.Find("X - Background");
        var solid = uiRoot.transform.Find("Solid Background");
        if (!solid)
        {
            solid = Object.Instantiate(background, uiRoot.transform);
            solid.name = "Solid Background";
            solid.SetSiblingIndex(background.GetSiblingIndex() + 1);
        }
        var solidImage = solid.GetComponent<Image>();
        solidImage.color = Color.black;
        solidImage.raycastTarget = false;
        EditorUtility.SetDirty(solidImage);

        // fixed box so the text shrinks instead of running over the buttons
        var fitter = mainText.GetComponent<ContentSizeFitter>();
        if (fitter) fitter.enabled = false;
        mainText.rectTransform.anchoredPosition = new Vector2(0, -20);
        mainText.rectTransform.sizeDelta = new Vector2(302, 220);
        mainText.enableAutoSizing = true;
        mainText.fontSizeMin = 8;
        mainText.fontSizeMax = 13;
        mainText.overflowMode = TextOverflowModes.Truncate;
        mainText.text =
            "Welcome to Lab 4: A Terrible Day in the Office\n\n" +
            "To finish your working day, complete these tasks:\n\n" +
            "- File the files into the drawer with the same colour\n" +
            "- Throw the can and the bottle (2 items) in the bin\n" +
            "- Pour yourself a full cup of coffee\n" +
            "- Wipe the 5 spots along the desk with the sponge\n\n" +
            "Press Start to begin. When all tasks are done, press Quit.";
        EditorUtility.SetDirty(mainText);

        // green "<task> reset" text under the reset row
        var statusObject = buttons.Find("ResetStatus");
        if (!statusObject)
        {
            statusObject = new GameObject("ResetStatus", typeof(RectTransform)).transform;
            statusObject.SetParent(buttons, false);
        }
        var status = statusObject.GetComponent<TextMeshProUGUI>();
        if (!status) status = statusObject.gameObject.AddComponent<TextMeshProUGUI>();
        status.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        status.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        status.rectTransform.anchoredPosition = new Vector2(0, -95);
        status.rectTransform.sizeDelta = new Vector2(300, 24);
        status.fontSize = 14;
        status.alignment = TextAlignmentOptions.Center;
        status.color = new Color(0.49f, 0.99f, 0.49f);
        status.raycastTarget = false;
        status.text = "";
        EditorUtility.SetDirty(status);

        var resetFeedback = uiRoot.GetComponent<ResetFeedback>();
        if (!resetFeedback) resetFeedback = uiRoot.AddComponent<ResetFeedback>();
        resetFeedback.statusText = status;
        EditorUtility.SetDirty(resetFeedback);

        // reset buttons in one labelled row, greyed out until Start
        string[] names = { "DrawerResetButton", "TrashResetButton", "CoffeeResetButton", "CleaningResetButton" };
        string[] labels = { "Reset\nDrawers", "Reset\nTrash", "Reset\nCoffee", "Reset\nCleaning" };
        string[] taskNames = { "Drawers", "Trash", "Coffee", "Cleaning" };
        float[] xs = { -117, -39, 39, 117 };
        var start = buttons.Find("StartButton").GetComponent<Button>();
        for (int i = 0; i < names.Length; i++)
        {
            var button = buttons.Find(names[i]).GetComponent<Button>();
            var rect = (RectTransform)button.transform;
            rect.anchoredPosition = new Vector2(xs[i], -50);
            rect.sizeDelta = new Vector2(74, 36);

            var label = button.GetComponentInChildren<TMP_Text>(true);
            label.text = labels[i];
            label.fontSize = 11;
            label.enableAutoSizing = false;
            label.alignment = TextAlignmentOptions.Center;
            EditorUtility.SetDirty(label);

            var colors = button.colors;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            button.colors = colors;
            button.interactable = false;
            EditorUtility.SetDirty(button);

            // start button turns the reset button on, like ticking "Button > interactable" in its OnClick
            if (!HasListener(start.onClick, button, "set_interactable"))
            {
                var turnOn = (UnityAction<bool>)System.Delegate.CreateDelegate(typeof(UnityAction<bool>), button, "set_interactable");
                UnityEventTools.AddBoolPersistentListener(start.onClick, turnOn, true);
            }

            if (i == 1 && !HasListener(button.onClick, tracker.trashTask, "ResetScore"))
                UnityEventTools.AddPersistentListener(button.onClick, tracker.trashTask.ResetScore);
            if (i == 3 && !HasListener(button.onClick, tracker.cleaningTask, "ResetProgress"))
                UnityEventTools.AddPersistentListener(button.onClick, tracker.cleaningTask.ResetProgress);
            if (!HasListener(button.onClick, resetFeedback, "Show"))
                UnityEventTools.AddStringPersistentListener(button.onClick, resetFeedback.Show, taskNames[i]);
        }
        EditorUtility.SetDirty(start);

        // checklist after Start
        var menuStatus = buttons.GetComponent<MenuStatus>();
        if (!menuStatus) menuStatus = buttons.gameObject.AddComponent<MenuStatus>();
        menuStatus.runController = runController;
        menuStatus.instructionsText = mainText;
        menuStatus.drawerA = tracker.drawerA;
        menuStatus.drawerB = tracker.drawerB;
        menuStatus.trashTask = tracker.trashTask;
        menuStatus.coffeeTask = tracker.coffeeTask;
        menuStatus.cleaningTask = tracker.cleaningTask;
        EditorUtility.SetDirty(menuStatus);

        // quit asks for a second press
        var quit = buttons.Find("QuitButton").GetComponent<Button>();
        for (int i = quit.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            string method = quit.onClick.GetPersistentMethodName(i);
            if (method == "ShowSummaryThenQuit" || method == "ConfirmThenQuit")
                UnityEventTools.RemovePersistentListener(quit.onClick, i);
        }
        UnityEventTools.AddPersistentListener(quit.onClick, summary.ConfirmThenQuit);
        EditorUtility.SetDirty(quit);

        var quitLabel = quit.GetComponentInChildren<TMP_Text>(true);
        quitLabel.enableAutoSizing = true;
        quitLabel.fontSizeMin = 8;
        quitLabel.fontSizeMax = 16;
        EditorUtility.SetDirty(quitLabel);
        summary.quitButtonLabel = quitLabel;
        EditorUtility.SetDirty(summary);
    }

    // empty object with a 3D text child, you place the empty object
    static TextMeshPro WorldText(Transform parent, string name, Vector2 size, string value, Color color)
    {
        var root = parent.Find(name);
        if (!root)
        {
            root = new GameObject(name).transform;
            root.SetParent(parent, false);
        }
        var textObject = root.Find("Text");
        if (!textObject)
        {
            textObject = new GameObject("Text").transform;
            textObject.SetParent(root, false);
        }
        var text = textObject.GetComponent<TextMeshPro>();
        if (!text) text = textObject.gameObject.AddComponent<TextMeshPro>();
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        text.fontSharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Outline.mat");
        text.rectTransform.sizeDelta = size;
        text.rectTransform.anchoredPosition3D = Vector3.zero;
        text.enableAutoSizing = true;
        text.fontSizeMin = 0.05f;
        text.fontSizeMax = 20f;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        text.text = value;
        text.color = color;
        EditorUtility.SetDirty(text);
        return text;
    }

    // same as Add Component > Audio Source and dragging the clip in
    static void AddSound(GameObject go, string clipPath, float pitch)
    {
        var source = go.GetComponent<AudioSource>();
        if (!source) source = go.AddComponent<AudioSource>();
        source.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
        source.playOnAwake = false;
        source.pitch = pitch;
        EditorUtility.SetDirty(source);
    }

    // so running it twice does not add the same OnClick entry twice
    static bool HasListener(UnityEventBase evt, Object target, string method)
    {
        for (int i = 0; i < evt.GetPersistentEventCount(); i++)
            if (evt.GetPersistentTarget(i) == target && evt.GetPersistentMethodName(i) == method) return true;
        return false;
    }
}
