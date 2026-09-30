# AGENTS.md

> A map of this project for AI agents. Update it when the structure changes significantly.

## Project Overview
"A Terrible Day in the Office" is a Unity VR app for Meta Quest with five deliberately poor-usability tasks. The assignment is to improve its usability. See `.ai-factory/DESCRIPTION.md`.

## Tech Stack
- **Language:** C#
- **Engine:** Unity 6000.6.0f1, URP 17.6
- **XR:** XR Interaction Toolkit 3.6.0, OpenXR 1.18, Oculus XR 4.5.5
- **Database / ORM:** none

## Project Structure
```
Assets/
├── Scenes/terribleOffice.unity     # the only build scene; all wiring (refs, OnClick) lives here
├── _Lab4Assets/                    # ALL game code; edit here
│   ├── GameController.cs           # GameRunController: Start lock/unlock, run.json (LOCKED)
│   ├── ProgressTracker.cs          # polls IsComplete, logs, finishes run
│   ├── RunSummaryAndQuit.cs        # RunSummaryOnQuit: Quit (press twice) → summary + countdown
│   ├── FeedbackSounds.cs           # [Usability] shared success/error sounds
│   ├── ResetFeedback.cs            # [Usability] "<task> reset ✓" text on the menu
│   ├── MenuStatus.cs               # [Usability] Reset lock until Start + live task checklist
│   ├── Editor/                     # [Usability] editor-only tools (not in the build)
│   │   ├── UsabilitySceneSetup.cs  # Tools > Usability > Apply Scene Setup: wires all usability objects into the scene
│   │   └── UsabilityPlayTest.cs    # Tools > Usability > Play Mode Smoke Test: fake task events + checks + pictures
│   ├── Cleaning/                   # CleaningTask (sponge touches grid), CleaningTaskController (LOCKED)
│   ├── Coffee/                     # CoffeeTask (tilt-to-pour raycast into cup)
│   ├── Drawers/                    # DrawerTask (sockets), FileStackSpawner/FileItem/DrawerResetController (LOCKED)
│   ├── Trash/                      # TrashBinScorer (throw rules), TrashRespawner/TrashItemThrowData (LOCKED)
│   ├── Audio/                      # Button_22_click.wav
│   ├── X - Room/                   # office art prefabs
│   └── X - Others/VRTemplateAssets # Unity VR template scripts/prefabs (vendor)
├── Samples/, XR/, XRI/, TextMesh Pro/  # package samples & settings (vendor)
Packages/manifest.json              # package deps
ProjectSettings/                    # Unity settings (EditorBuildSettings = scene list)
Manual.pdf                          # assignment brief
Lab 3 - VR - Usability Tables.docx.pdf  # usability inspection (in progress)
```

## Key Entry Points
| File | Purpose |
|------|---------|
| `Assets/Scenes/terribleOffice.unity` | Scene: menu canvas (Start/Quit/4×Reset buttons), tasks, XR Origin |
| `Assets/_Lab4Assets/GameController.cs` | Run start/finish, interaction lock |
| `Assets/_Lab4Assets/ProgressTracker.cs` | Task completion aggregation |
| `Assets/_Lab4Assets/*/…Task.cs` | Per-task logic (`IsComplete`) |
| `Packages/manifest.json` | Unity package dependencies |

## Documentation
| Document | Path | Description |
|----------|------|-------------|
| README | README.md | Title only |
| Assignment brief | Manual.pdf | Deliverables: 1 merged build, ≤5-min video, usability tables |
| Usability tables | Lab 3 - VR - Usability Tables.docx.pdf | Per-task principle violations (partially filled) |

## AI Context Files
| File | Purpose |
|------|---------|
| AGENTS.md | This project map |
| .ai-factory/DESCRIPTION.md | Project description and tech stack |
| .ai-factory/rules/base.md | Conventions and the **DO NOT CHANGE** file list |
| .ai-factory/config.yaml | AI Factory settings |
| ~/.claude/CLAUDE.md | User preferences (fish shell) |

## Agent Rules
- Never edit code marked `// DO NOT CHANGE`. Extend it through new scripts or editable files instead (see `.ai-factory/rules/base.md`).
- Scene wiring is done by `Assets/_Lab4Assets/Editor/UsabilitySceneSetup.cs`, run in Unity batchmode (Unity must be closed):
  `~/Unity/Hub/Editor/6000.6.0f1/Editor/Unity -batchmode -quit -projectPath <repo> -executeMethod UsabilitySceneSetup.Apply -logFile <log>`.
  Add new wiring there (idempotent: find-or-create by name, positions from renderer bounds), not by hand. Re-run it after merging a teammate's scene.
- Check positions with `UsabilitySceneSetup.ApplyAndSnapshot` / `Snapshots` (PNG renders, `SNAP_DIR` env var) and behaviour with `UsabilityPlayTest.Run` (no `-quit`; it exits itself).
- Don't hand-edit `.unity` or `.prefab` YAML.
- Split shell commands into separate steps instead of chaining them.
  - Wrong: `cd Assets && grep ...`
  - Right: run `grep` with an absolute path.
