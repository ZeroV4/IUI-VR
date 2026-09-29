# AGENTS.md

> A map of this project for AI agents. Update it when the structure changes significantly.

## Project Overview
"A Terrible Day in the Office" is a Unity VR app for Meta Quest with five deliberately poor-usability tasks. The assignment is to improve its usability. See `.ai-factory/DESCRIPTION.md`.

## Tech Stack
- **Language:** C#
- **Engine:** Unity 6000.0.51f1, URP
- **XR:** XR Interaction Toolkit 3.1.1, OpenXR, Oculus XR
- **Database / ORM:** none

## Project Structure
```
Assets/
├── Scenes/terribleOffice.unity     # the only build scene; all wiring (refs, OnClick) lives here
├── _Lab4Assets/                    # ALL game code; edit here
│   ├── GameController.cs           # GameRunController: Start lock/unlock, run.json (LOCKED)
│   ├── ProgressTracker.cs          # polls IsComplete, logs, finishes run
│   ├── RunSummaryAndQuit.cs        # RunSummaryOnQuit: Quit button → summary + countdown
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
- New components, fields and listeners have to be wired up in the Unity Editor. Say which wiring steps are needed with every code change.
- Don't hand-edit `.unity` or `.prefab` YAML unless asked; the team wires things in the Editor.
- Split shell commands into separate steps instead of chaining them.
  - Wrong: `cd Assets && grep ...`
  - Right: run `grep` with an absolute path.
