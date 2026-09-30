using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// [Usability] Editor-only helper (the Editor folder is not included in the build).
// "Tools > Usability > Apply Scene Setup" wires every usability change into terribleOffice.unity:
// new labels, counters, dirt marks, coffee level, sounds, menu layout and button listeners.
// It is safe to run again (for example after merging a teammate's scene): it finds the objects
// it created before by name and updates them instead of making copies.
// Positions are computed from the real objects (renderer bounds, desk raycast), not typed in by hand.
public static class UsabilitySceneSetup
{
    const string ScenePath = "Assets/Scenes/terribleOffice.unity";

    // Where the player's eyes are at the start (XR Origin position + standing eye height)
    static readonly Vector3 HeadPosition = new Vector3(-0.68f, 1.6f, -0.07f);

    const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    const string OutlineFontMaterialPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Outline.mat";

    // ------------------------------------------------------------------
    // Apply
    // ------------------------------------------------------------------

    [MenuItem("Tools/Usability/Apply Scene Setup")]
    public static void Apply()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);

        // Shared objects
        var gameManager = GameObject.Find("GameManager");
        var runController = gameManager.GetComponent<GameRunController>();
        var summary = gameManager.GetComponent<RunSummaryOnQuit>();
        var tracker = gameManager.GetComponent<ProgressTracker>();

        var sounds = SetupFeedbackSounds(gameManager.transform);

        SetupFileColours();
        SetupDrawer(tracker.drawerA, sounds);
        SetupDrawer(tracker.drawerB, sounds);
        SetupTrash(tracker.trashTask, sounds, runController);
        SetupCoffee(tracker.coffeeTask, sounds);
        SetupCleaning(tracker.cleaningTask, sounds);
        SetupMenu(runController, summary, tracker);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[UsabilitySetup] Scene saved.");
    }

    // Apply, then take pictures of the result (used from the command line)
    public static void ApplyAndSnapshot()
    {
        Apply();
        TakeSnapshots("after");
    }

    // ---------- Sounds ----------

    static FeedbackSounds SetupFeedbackSounds(Transform gameManager)
    {
        var go = FindOrCreateChild(gameManager, "FeedbackAudio").gameObject;

        var source = go.GetComponent<AudioSource>();
        if (!source) source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f; // 2D
        source.volume = 0.8f;

        var sounds = go.GetComponent<FeedbackSounds>();
        if (!sounds) sounds = go.AddComponent<FeedbackSounds>();
        sounds.source = source;
        sounds.successClip = CopyAudio("Button Pop", "Assets/_Lab4Assets/Audio/Success_Pop.wav");
        sounds.errorClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Lab4Assets/X - Others/VRTemplateAssets/Audio/Button_14_hover.wav");
        sounds.errorPitch = 0.5f;
        EditorUtility.SetDirty(sounds);

        Debug.Log($"[UsabilitySetup] FeedbackAudio: success={sounds.successClip} error={sounds.errorClip}");
        return sounds;
    }

    // Copies a clip from the XRI samples into _Lab4Assets, so it survives a samples update
    static AudioClip CopyAudio(string searchName, string targetPath)
    {
        var existing = AssetDatabase.LoadAssetAtPath<AudioClip>(targetPath);
        if (existing) return existing;

        foreach (var guid in AssetDatabase.FindAssets(searchName + " t:AudioClip"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(searchName + ".wav")) continue;
            AssetDatabase.CopyAsset(path, targetPath);
            AssetDatabase.ImportAsset(targetPath);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(targetPath);
        }

        Debug.LogWarning("[UsabilitySetup] Could not find audio clip " + searchName);
        return null;
    }

    // ---------- Drawers ----------

    // The two file colours were almost the same green. Now one is dark and one is light.
    static void SetupFileColours()
    {
        SetMaterialColour("Assets/_Lab4Assets/Drawers/FileMaterialGreen.mat", new Color(0.05f, 0.33f, 0.08f));
        SetMaterialColour("Assets/_Lab4Assets/Drawers/FileMaterialLightGreen.mat", new Color(0.6f, 1f, 0.6f));
    }

    static void SetupDrawer(DrawerTask drawer, FeedbackSounds sounds)
    {
        if (!drawer) return;

        bool light = drawer.expectedType == FileType.LightGreen;
        var body = drawer.transform.Find("Drawer_itself");   // the part that slides out
        var front = body.Find("Front");
        var knob = body.Find("Knob");

        // The front panel faces the player. "outward" points from the drawer towards the player.
        var frontBounds = front.GetComponent<Renderer>().bounds;
        var bodyBounds = BoundsOf(body);
        Vector3 outward = Flatten(frontBounds.center - bodyBounds.center).normalized;
        float faceOffset = Vector3.Dot(frontBounds.extents, Abs(outward)); // half the panel thickness
        Vector3 faceCenter = frontBounds.center + outward * faceOffset;
        float knobBottom = knob.GetComponent<Renderer>().bounds.min.y;
        float frontBottom = frontBounds.min.y;

        // Everything hangs under one object that moves with the drawer and uses metres
        var root = FindOrCreateChild(body, "UsabilityLabels");
        root.localScale = Vector3.one / body.lossyScale.x;
        root.position = faceCenter + outward * 0.007f;
        root.rotation = Quaternion.LookRotation(-outward, Vector3.up); // text reads from the player's side

        // 1) Colour swatch with the drawer name, in the same material as its files
        var fileMaterial = AssetDatabase.LoadAssetAtPath<Material>(light
            ? "Assets/_Lab4Assets/Drawers/FileMaterialLightGreen.mat"
            : "Assets/_Lab4Assets/Drawers/FileMaterialGreen.mat");
        // root is upright and in metres, so a local Y offset is a height difference in the world.
        // Plates sit 4 mm behind their text (local +Z points into the drawer).
        float labelY = knobBottom - 0.03f - 0.045f;     // 3 cm under the knob, plate is 9 cm tall
        float labelLocalY = labelY - root.position.y;
        var swatch = MakePlate(root, "ColourSwatch", fileMaterial, 0.36f, 0.09f);
        swatch.transform.localPosition = new Vector3(0, labelLocalY, 0.004f);
        var label = MakeText(root, "DrawerLabel", light ? "LIGHT GREEN" : "DARK GREEN", 0.33f, 0.07f, false);
        label.rectTransform.anchoredPosition3D = new Vector3(0, labelLocalY, 0);
        label.color = light ? Color.black : Color.white;

        // 2) Counter "0/4" on a dark plate under the swatch
        float countY = labelY - 0.045f - 0.02f - 0.05f;  // 2 cm gap, plate is 10 cm tall
        float countLocalY = countY - root.position.y;
        var countPlate = MakePlate(root, "CountBackground", LabelBackgroundMaterial(), 0.36f, 0.10f);
        countPlate.transform.localPosition = new Vector3(0, countLocalY, 0.004f);
        var count = MakeText(root, "DrawerCount", $"0/{drawer.requiredCount}", 0.33f, 0.085f, true);
        count.rectTransform.anchoredPosition3D = new Vector3(0, countLocalY, 0);

        drawer.countText = count;
        drawer.sounds = sounds;
        EditorUtility.SetDirty(drawer);

        Debug.Log($"[UsabilitySetup] {drawer.name}: face={V(faceCenter)} outward={V(outward)} knobBottom={knobBottom:F3} " +
                  $"label y={labelY:F3} count y={countY:F3} (count bottom {countY - 0.05f:F3} > front bottom {frontBottom:F3})");
    }

    // ---------- Trash ----------

    static void SetupTrash(TrashBinScorer bin, FeedbackSounds sounds, GameRunController runController)
    {
        if (!bin) return;

        var binBounds = bin.GetComponent<Renderer>().bounds;
        // Not a child of the bin (the bin is scaled and has a Rigidbody); a sibling instead
        var root = FindOrCreateChild(bin.transform.parent, "TrashStatus");
        root.position = new Vector3(binBounds.center.x, binBounds.max.y + 0.5f, binBounds.center.z);
        FacePlayer(root);

        var plate = MakePlate(root, "Background", LabelBackgroundMaterial(), 0.56f, 0.2f);
        plate.transform.localPosition = new Vector3(0, 0, 0.004f);
        var text = MakeText(root, "Text", $"Trash 0/{TrashBinScorer.RequiredScore}", 0.52f, 0.17f, true);
        text.rectTransform.anchoredPosition3D = Vector3.zero;

        bin.statusText = text;
        bin.sounds = sounds;
        bin.runController = runController;
        EditorUtility.SetDirty(bin);

        Debug.Log($"[UsabilitySetup] TrashStatus at {V(root.position)} (bin top {binBounds.max.y:F3})");
    }

    // ---------- Coffee ----------

    static void SetupCoffee(CoffeeTask coffee, FeedbackSounds sounds)
    {
        if (!coffee) return;

        // Pouring thresholds: start at 45 deg (was 25), stop at 60 (was 45), full after 2.5 s in the cup (was 0.3)
        coffee.angleOnDeg = 45f;
        coffee.angleOffDeg = 60f;
        coffee.requiredSeconds = 2.5f;

        var cup = coffee.cup.transform;              // the grabbable cup, so the coffee moves with it
        var cupBounds = cup.GetComponent<Renderer>().bounds;
        // cupMouth sits in the middle of the cup body (the renderer bounds also include the handle)
        Vector3 bodyCenter = coffee.cupMouth ? coffee.cupMouth.bounds.center : cupBounds.center;
        float cupDiameter = Mathf.Min(cupBounds.size.x, cupBounds.size.z);

        float bottom = cupBounds.min.y + 0.006f;     // 6 mm cup floor
        float top = cupBounds.max.y - 0.012f;        // full cup = 12 mm under the rim
        float liquidDiameter = cupDiameter * 0.8f;   // stay inside the cup walls

        // CoffeeLevel = pivot at the cup bottom; CoffeeTask scales its Y from 0 to full
        var level = FindOrCreateChild(cup, "CoffeeLevel");
        level.rotation = cup.rotation;
        level.position = new Vector3(bodyCenter.x, bottom, bodyCenter.z);
        Vector3 worldScale = new Vector3(liquidDiameter, (top - bottom) / 2f, liquidDiameter); // a cylinder is 2 units tall
        level.localScale = Divide(worldScale, cup.lossyScale);

        var liquid = FindOrCreatePrimitive(level, "CoffeeLiquid", PrimitiveType.Cylinder,
            GetOrCreateMaterial("Assets/_Lab4Assets/Coffee/CoffeeLiquid.mat", "Universal Render Pipeline/Lit", new Color(0.23f, 0.14f, 0.08f), 0.6f));
        liquid.transform.localPosition = new Vector3(0, 1, 0);   // bottom of the cylinder on the pivot
        liquid.transform.localRotation = Quaternion.identity;
        liquid.transform.localScale = Vector3.one;
        level.gameObject.SetActive(false);                      // empty cup at the start

        // Text above and a bit to the right of the cup, so the pot does not cover it while pouring
        var root = FindOrCreateChild(coffee.transform, "CoffeeStatus");
        root.localScale = Vector3.one;
        Vector3 aboveCup = new Vector3(bodyCenter.x, cupBounds.max.y + 0.1f, bodyCenter.z);
        root.position = aboveCup + RightOfPlayer(aboveCup) * 0.12f;
        FacePlayer(root);
        var text = MakeText(root, "Text", "", 0.24f, 0.06f, true);
        text.fontSharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(OutlineFontMaterialPath);
        text.rectTransform.anchoredPosition3D = Vector3.zero;

        coffee.coffeeLevel = level;
        coffee.statusText = text;
        coffee.sounds = sounds;
        EditorUtility.SetDirty(coffee);

        Debug.Log($"[UsabilitySetup] Coffee: level at {V(level.position)} full height {top - bottom:F3} diameter {liquidDiameter:F3}; text at {V(root.position)}");
    }

    // ---------- Cleaning ----------

    static void SetupCleaning(CleaningTask cleaning, FeedbackSounds sounds)
    {
        if (!cleaning) return;

        var dirtMaterial = GetOrCreateMaterial("Assets/_Lab4Assets/Cleaning/DirtMark.mat", "Universal Render Pipeline/Lit",
            new Color(0.55f, 0.35f, 0.17f), 0f);

        foreach (var zone in cleaning.targets)
        {
            if (!zone) continue;
            float deskY = DeskHeightUnder(zone.bounds.center);

            // A flat brown disc lying on the desk, 1.5 mm above the surface
            var dirt = FindOrCreatePrimitive(zone.transform, "Dirt", PrimitiveType.Cylinder, dirtMaterial);
            dirt.transform.rotation = Quaternion.identity;
            dirt.transform.position = new Vector3(zone.bounds.center.x, deskY + 0.0015f, zone.bounds.center.z);
            dirt.transform.localScale = Divide(new Vector3(0.16f, 0.001f, 0.16f), zone.transform.lossyScale);
            dirt.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            dirt.SetActive(true);

            Debug.Log($"[UsabilitySetup] Dirt under {zone.name} at {V(dirt.transform.position)} (desk y {deskY:F4})");
        }

        cleaning.sounds = sounds;
        EditorUtility.SetDirty(cleaning);

        // Hint above the sponge's home spot (not a child of the sponge, so it stays when the sponge is taken)
        var controller = Object.FindAnyObjectByType<CleaningTaskController>();
        Transform home = controller && controller.spongeHome ? controller.spongeHome : cleaning.transform;
        var spongeBounds = BoundsOf(cleaning.transform);
        var root = FindOrCreateChild(controller ? controller.transform : cleaning.transform.parent, "SpongeHint");
        root.localScale = Vector3.one;
        root.position = new Vector3((home.position.x + spongeBounds.center.x) / 2f, spongeBounds.max.y + 0.07f, home.position.z);
        FacePlayer(root);
        var hint = MakeText(root, "Text", "Grab the sponge and\nwipe the brown spots", 0.22f, 0.05f, false);
        hint.fontSharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(OutlineFontMaterialPath);
        hint.rectTransform.anchoredPosition3D = Vector3.zero;

        Debug.Log($"[UsabilitySetup] SpongeHint at {V(root.position)} (sponge top {spongeBounds.max.y:F3})");
    }

    // Highest static surface under a point (the desk top), ignoring triggers and moving objects
    static float DeskHeightUnder(Vector3 point)
    {
        Physics.SyncTransforms();
        float best = float.MinValue;
        foreach (var hit in Physics.RaycastAll(point + Vector3.up * 0.3f, Vector3.down, 1f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.rigidbody) continue;
            if (hit.point.y > best) best = hit.point.y;
        }
        if (best == float.MinValue)
        {
            Debug.LogWarning("[UsabilitySetup] No desk found under " + point);
            return point.y;
        }
        return best;
    }

    // ---------- Menu ----------

    static void SetupMenu(GameRunController runController, RunSummaryOnQuit summary, ProgressTracker tracker)
    {
        var uiRoot = GameObject.Find("UI Root");
        var canvas = uiRoot.GetComponent<Canvas>();
        var buttons = uiRoot.transform.Find("Buttons");
        var mainText = uiRoot.transform.Find("MainText").GetComponent<TMP_Text>();

        // Opaque panel. The window glass was also drawn on top of the panel (UI writes no depth),
        // so it looked see-through; a higher sorting order draws the menu after the glass.
        canvas.sortingOrder = 10;
        EditorUtility.SetDirty(canvas);
        var background = uiRoot.transform.Find("X - Background").GetComponent<Image>();
        background.color = new Color(0, 0, 0, 1);
        EditorUtility.SetDirty(background);

        // Instructions: fixed box above the Reset row; the text shrinks to fit instead of running over the buttons
        var fitter = mainText.GetComponent<ContentSizeFitter>();
        if (fitter) fitter.enabled = false;
        var mainRect = mainText.rectTransform;
        mainRect.anchoredPosition = new Vector2(0, -20);
        mainRect.sizeDelta = new Vector2(302, 220);
        mainText.enableAutoSizing = true;
        mainText.fontSizeMin = 8;
        mainText.fontSizeMax = 13;
        mainText.overflowMode = TextOverflowModes.Truncate;
        mainText.text =
            "Welcome to Lab 4: A Terrible Day in the Office\n\n" +
            "To finish your working day, complete these tasks:\n\n" +
            "- File all files in their correct drawers\n" +
            "- Throw the trash in the bin\n" +
            "- Pour yourself a full cup of coffee\n" +
            "- Wipe your desk clean with the sponge\n\n" +
            "Press Start to begin. When all tasks are done, press Quit.";
        EditorUtility.SetDirty(mainText);

        // Reset confirmation text: green, under the Reset row. Under Buttons, so Quit hides it too.
        var status = FindOrCreateUIText(buttons, "ResetStatus");
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

        // Reset buttons: one labelled row between the text and Start/Quit, no overlap
        string[] names = { "DrawerResetButton", "TrashResetButton", "CoffeeResetButton", "CleaningResetButton" };
        string[] labels = { "Reset\nDrawers", "Reset\nTrash", "Reset\nCoffee", "Reset\nCleaning" };
        string[] feedbackNames = { "Drawers", "Trash", "Coffee", "Cleaning" };
        float[] xs = { -117, -39, 39, 117 };
        var resetButtons = new Button[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            var rect = (RectTransform)buttons.Find(names[i]);
            rect.anchoredPosition = new Vector2(xs[i], -50);
            rect.sizeDelta = new Vector2(74, 36);

            var label = rect.GetComponentInChildren<TMP_Text>(true);
            label.text = labels[i];
            label.fontSize = 11;
            label.enableAutoSizing = false;
            label.alignment = TextAlignmentOptions.Center;
            EditorUtility.SetDirty(label);

            resetButtons[i] = rect.GetComponent<Button>();
            var colors = resetButtons[i].colors;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); // clearly greyed out before Start
            resetButtons[i].colors = colors;
            EditorUtility.SetDirty(resetButtons[i]);
        }

        // Extra OnClick listeners, added after the original (locked) reset calls
        AddListenerOnce(resetButtons[1].onClick, tracker.trashTask, "ResetScore", tracker.trashTask.ResetScore);
        AddListenerOnce(resetButtons[3].onClick, tracker.cleaningTask, "ResetProgress", tracker.cleaningTask.ResetProgress);
        for (int i = 0; i < names.Length; i++)
            AddStringListenerOnce(resetButtons[i].onClick, resetFeedback, "Show", resetFeedback.Show, feedbackNames[i]);

        // Reset lock + live checklist
        var menuStatus = buttons.GetComponent<MenuStatus>();
        if (!menuStatus) menuStatus = buttons.gameObject.AddComponent<MenuStatus>();
        menuStatus.runController = runController;
        menuStatus.instructionsText = mainText;
        menuStatus.resetButtons = resetButtons;
        menuStatus.drawerA = tracker.drawerA;
        menuStatus.drawerB = tracker.drawerB;
        menuStatus.trashTask = tracker.trashTask;
        menuStatus.coffeeTask = tracker.coffeeTask;
        menuStatus.cleaningTask = tracker.cleaningTask;
        EditorUtility.SetDirty(menuStatus);

        // Quit asks for a second press
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

        Debug.Log("[UsabilitySetup] Menu: layout, reset listeners, MenuStatus and Quit confirmation done");
    }

    static void AddListenerOnce(UnityEvent evt, Object target, string method, UnityAction action)
    {
        for (int i = 0; i < evt.GetPersistentEventCount(); i++)
            if (evt.GetPersistentTarget(i) == target && evt.GetPersistentMethodName(i) == method) return;
        UnityEventTools.AddPersistentListener(evt, action);
    }

    static void AddStringListenerOnce(UnityEvent evt, Object target, string method, UnityAction<string> action, string argument)
    {
        for (int i = 0; i < evt.GetPersistentEventCount(); i++)
            if (evt.GetPersistentTarget(i) == target && evt.GetPersistentMethodName(i) == method) return;
        UnityEventTools.AddStringPersistentListener(evt, action, argument);
    }

    // ------------------------------------------------------------------
    // Small helpers
    // ------------------------------------------------------------------

    static Transform FindOrCreateChild(Transform parent, string name)
    {
        var child = parent.Find(name);
        if (child) return child;
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static GameObject FindOrCreatePrimitive(Transform parent, string name, PrimitiveType type, Material material)
    {
        var existing = parent.Find(name);
        GameObject go;
        if (existing)
        {
            go = existing.gameObject;
        }
        else
        {
            go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
        }

        // No collider: labels and decals must never block grabbing, sockets, the bin trigger or the coffee raycast
        var collider = go.GetComponent<Collider>();
        if (collider) Object.DestroyImmediate(collider);
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go;
    }

    // Thin box used as a sign background (world size in metres, the parent must have scale 1 in the world)
    static GameObject MakePlate(Transform parent, string name, Material material, float width, float height)
    {
        var plate = FindOrCreatePrimitive(parent, name, PrimitiveType.Cube, material);
        plate.transform.localRotation = Quaternion.identity;
        plate.transform.localScale = new Vector3(width, height, 0.004f);
        plate.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return plate;
    }

    // World-space TextMeshPro that fills a box of width x height metres
    static TextMeshPro MakeText(Transform parent, string name, string text, float width, float height, bool wrap)
    {
        var existing = parent.Find(name);
        GameObject go = existing ? existing.gameObject : new GameObject(name);
        if (!existing) go.transform.SetParent(parent, false);

        var tmp = go.GetComponent<TextMeshPro>();
        if (!tmp) tmp = go.AddComponent<TextMeshPro>();
        tmp.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        tmp.text = text;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 0.05f;
        tmp.fontSizeMax = 20f;
        tmp.rectTransform.sizeDelta = new Vector2(width, height);
        tmp.transform.localRotation = Quaternion.identity;
        tmp.transform.localScale = Vector3.one;
        tmp.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return tmp;
    }

    // Menu (uGUI) text under a canvas object
    static TextMeshProUGUI FindOrCreateUIText(Transform parent, string name)
    {
        var existing = parent.Find(name);
        GameObject go = existing ? existing.gameObject : new GameObject(name, typeof(RectTransform));
        if (!existing) go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (!tmp) tmp = go.AddComponent<TextMeshProUGUI>();
        go.transform.localScale = Vector3.one;
        go.transform.localRotation = Quaternion.identity;
        return tmp;
    }

    // Turns a label so it faces the player's eyes at the start position
    static void FacePlayer(Transform label)
    {
        label.rotation = Quaternion.LookRotation(label.position - HeadPosition, Vector3.up);
    }

    // Direction that is "to the right" for the player looking at a point
    static Vector3 RightOfPlayer(Vector3 point)
    {
        Vector3 look = Flatten(point - HeadPosition).normalized;
        return Vector3.Cross(Vector3.up, look);
    }

    static Material LabelBackgroundMaterial()
    {
        return GetOrCreateMaterial("Assets/_Lab4Assets/LabelBackground.mat", "Universal Render Pipeline/Unlit", new Color(0.08f, 0.08f, 0.08f), 0f);
    }

    static Material GetOrCreateMaterial(string path, string shaderName, Color colour, float smoothness)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!material)
        {
            material = new Material(Shader.Find(shaderName));
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", colour);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(material);
        return material;
    }

    static void SetMaterialColour(string path, Color colour)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!material) return;
        material.SetColor("_BaseColor", colour);
        material.SetColor("_Color", colour);
        EditorUtility.SetDirty(material);
    }

    static Bounds BoundsOf(Transform t)
    {
        var renderers = t.GetComponentsInChildren<Renderer>(true);
        var bounds = renderers[0].bounds;
        foreach (var r in renderers)
        {
            // skip the labels this script added, so running it twice gives the same result
            if (r.transform.parent && r.transform.parent.name == "UsabilityLabels") continue;
            bounds.Encapsulate(r.bounds);
        }
        return bounds;
    }

    static Vector3 Flatten(Vector3 v) => new Vector3(v.x, 0, v.z);
    static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    static Vector3 Divide(Vector3 a, Vector3 b) => new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);
    static string V(Vector3 v) => $"({v.x:F3},{v.y:F3},{v.z:F3})";
    static string FullPath(Transform t) => t.parent ? FullPath(t.parent) + "/" + t.name : t.name;

    // ------------------------------------------------------------------
    // Checking tools
    // ------------------------------------------------------------------

    // Prints positions and sizes of the objects the usability changes touch
    [MenuItem("Tools/Usability/Inspect Scene")]
    public static void Inspect()
    {
        EditorSceneManager.OpenScene(ScenePath);
        var sb = new StringBuilder("[Inspect]\n");

        string[] names =
        {
            "XR Origin (XR Rig)", "Drawer_Left", "Drawer_Right", "TrashTask", "CoffeeTask", "CleaningTask", "UI Root", "GameManager"
        };

        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (!go.scene.IsValid()) continue;
            if (System.Array.IndexOf(names, go.name) < 0) continue;
            Dump(go.transform, sb, 0, go.name == "UI Root" ? 4 : 3);
        }

        Debug.Log(sb.ToString());
    }

    static void Dump(Transform t, StringBuilder sb, int depth, int maxDepth)
    {
        string pad = new string(' ', depth * 2);
        sb.Append($"{pad}- {FullPath(t)} active={t.gameObject.activeSelf} pos={V(t.position)} rot={V(t.eulerAngles)} lossy={V(t.lossyScale)}\n");

        var rend = t.GetComponent<Renderer>();
        if (rend) sb.Append($"{pad}    renderer bounds c={V(rend.bounds.center)} size={V(rend.bounds.size)} mat={(rend.sharedMaterial ? rend.sharedMaterial.name : "none")}\n");
        foreach (var col in t.GetComponents<Collider>())
            sb.Append($"{pad}    collider {col.GetType().Name} trigger={col.isTrigger} c={V(col.bounds.center)} size={V(col.bounds.size)}\n");
        var rt = t as RectTransform;
        if (rt) sb.Append($"{pad}    rect anchored={rt.anchoredPosition} size={rt.sizeDelta}\n");
        foreach (var tmp in t.GetComponents<TMP_Text>())
            sb.Append($"{pad}    TMP size={tmp.fontSize} auto={tmp.enableAutoSizing} text='{tmp.text.Replace("\n", "|")}'\n");
        foreach (var btn in t.GetComponents<Button>())
            for (int i = 0; i < btn.onClick.GetPersistentEventCount(); i++)
                sb.Append($"{pad}      onClick[{i}] {btn.onClick.GetPersistentTarget(i)} . {btn.onClick.GetPersistentMethodName(i)}\n");

        if (depth >= maxDepth) return;
        foreach (Transform child in t)
            Dump(child, sb, depth + 1, maxDepth);
    }

    // Renders PNG pictures from the player's point of view, to check positions without a headset.
    // Pictures go to the folder in the SNAP_DIR environment variable, or Temp/Snapshots.
    [MenuItem("Tools/Usability/Snapshots")]
    public static void Snapshots()
    {
        EditorSceneManager.OpenScene(ScenePath);
        TakeSnapshots("scene");
    }

    static void TakeSnapshots(string prefix)
    {
        string dir = System.Environment.GetEnvironmentVariable("SNAP_DIR");
        if (string.IsNullOrEmpty(dir)) dir = "Temp/Snapshots";
        Directory.CreateDirectory(dir);

        // Show the "during the run" state too (not saved): coffee half full
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name == "CoffeeLevel") t.gameObject.SetActive(true);
            if (t.name == "CoffeeStatus") t.GetComponentInChildren<TMP_Text>().text = "Coffee 40%";
        }

        foreach (var tmp in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
            tmp.ForceMeshUpdate();
        Canvas.ForceUpdateCanvases();

        var go = new GameObject("SnapCam");
        go.hideFlags = HideFlags.HideAndDontSave;
        var cam = go.AddComponent<Camera>();
        cam.fieldOfView = 70f;
        cam.nearClipPlane = 0.02f;
        var rt = new RenderTexture(1280, 800, 24);
        cam.targetTexture = rt;

        Vector3 head = HeadPosition;
        Shot(cam, rt, dir, prefix + "_01_forward", head, new Vector3(0.6f, 0.9f, -0.07f));
        Shot(cam, rt, dir, prefix + "_02_drawers", head, new Vector3(-0.3f, 0.35f, -0.05f));
        Shot(cam, rt, dir, prefix + "_03_bin", head, new Vector3(1.54f, 0.6f, -0.867f));
        Shot(cam, rt, dir, prefix + "_04_cup", head, new Vector3(-0.13f, 0.85f, -0.895f));
        Shot(cam, rt, dir, prefix + "_05_menu", head, new Vector3(-0.131f, 1.5f, 1.493f));
        Shot(cam, rt, dir, prefix + "_06_desk_top", new Vector3(-0.05f, 2.5f, -0.07f), new Vector3(-0.05f, 0.7f, -0.0701f));
        Shot(cam, rt, dir, prefix + "_07_sponge", head, new Vector3(-0.28f, 0.8f, -0.49f));
        Shot(cam, rt, dir, prefix + "_08_drawer_left_front", new Vector3(-1.0f, 0.5f, 0.6f), new Vector3(-0.3f, 0.3f, 0.6f));
        Shot(cam, rt, dir, prefix + "_09_drawer_right_front", new Vector3(-1.0f, 0.5f, -0.69f), new Vector3(-0.3f, 0.3f, -0.69f));
        Shot(cam, rt, dir, prefix + "_10_menu_front", new Vector3(-0.131f - 0.487f * 1.3f, 1.55f, 1.493f - 0.873f * 1.3f), new Vector3(-0.131f, 1.6f, 1.493f));
        Shot(cam, rt, dir, prefix + "_11_cup_close", new Vector3(-0.45f, 1.15f, -0.8f), new Vector3(-0.13f, 0.82f, -0.895f));
        Shot(cam, rt, dir, prefix + "_12_crouch_drawers", new Vector3(-0.85f, 1.0f, -0.05f), new Vector3(-0.3f, 0.25f, -0.05f));

        cam.targetTexture = null;
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(rt);
        Debug.Log("[Snapshots] saved to " + dir);
    }

    static void Shot(Camera cam, RenderTexture rt, string dir, string name, Vector3 from, Vector3 lookAt)
    {
        cam.transform.position = from;
        cam.transform.LookAt(lookAt);
        cam.Render();

        RenderTexture.active = rt;
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }
}
