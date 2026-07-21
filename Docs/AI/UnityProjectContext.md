# Unity Project Context

<!-- unity-onboarding:generated:start -->

## Project Summary

- Project root: `C:/Github/action_hunters/action_hunters`
- Last analyzed: 2026-07-21
- Last analyzed commit: `71f8aa5`
- State: early prototype; first-party gameplay architecture is not established yet.

## Confirmed Environment

- Unity version: Unity 6.3 LTS, `6000.3.8f1`
- Render pipeline: Universal Render Pipeline `17.3.0`
- Input system: Unity Input System `1.18.0` with the template input action asset
- Target platforms: Windows is the current build target; broader support is not yet defined

## Important Packages And Frameworks

| Area | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| Networking | Photon Fusion 2.0.12 Stable, build 1861; App ID configured | Confirmed | `Assets/Photon/Fusion/build_info.txt`, `PhotonAppSettings.asset` |
| Navigation | AI Navigation 2.0.10 | Confirmed | `Packages/manifest.json` |
| Rendering | URP 17.3.0 | Confirmed | `Packages/manifest.json`, `Assets/Settings/` |
| Input | Input System 1.18.0 | Confirmed | `Packages/manifest.json`, `Assets/InputSystem_Actions.inputactions` |
| UI | uGUI 2.0.0 available | Confirmed | `Packages/manifest.json` |
| Tests | Unity Test Framework 1.6.0 available | Confirmed | `Packages/manifest.json` |

## Directory Structure

| Path | Purpose | Confidence | Evidence |
| --- | --- | --- | --- |
| `Assets/ActionHunters/` | First-party project content | Confirmed | Created for Action Hunters implementation |
| `Assets/Scenes/` | Game scenes | Confirmed | `Main.unity` |
| `Assets/Photon/` | Imported Photon Fusion vendor SDK | Confirmed | Fusion package assets and assemblies |
| `Assets/KayKit/` | Imported Adventurers and Platformer packs | Confirmed | Character and environment production prefabs |
| `Assets/NOTFUN/` | Imported Monsters Pack 04 | Confirmed | Creature production prefabs |
| `Assets/PixPlays/` | Imported Elemental Spells Full Pack VFX | Confirmed | Particle-system prefabs and source materials |
| `Assets/Layer Lab/` | Imported GUI Pro resources | Confirmed | Minimal Game Dark sprites/prefabs and Bundle1 handoff |
| `Assets/Settings/` | URP and volume configuration | Confirmed | Pipeline and renderer assets |
| `Assets/TutorialInfo/` | Unity template documentation | Confirmed | Template scripts and assets |

## Assembly Boundaries

| Assembly | Responsibility | Key references | Notes |
| --- | --- | --- | --- |
| `Assembly-CSharp` | First-party runtime scripts | Unity modules, installed packages | No custom runtime asmdef yet |
| `Assembly-CSharp-Editor` | First-party editor tooling | UnityEditor, Assembly-CSharp | Scene builder lives under `Assets/ActionHunters/Editor` |
| `Fusion.Unity` | Photon Fusion Unity integration | Fusion runtime assemblies | Vendor-owned; do not edit |

## Scenes And Startup Flow

- Build scenes: `Assets/Scenes/Main.unity`
- Likely startup scene: `Main`
- Scene loading flow: Fusion bootstrap prototype; production loading flow is not established

## Architecture

| Pattern | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| Gameplay architecture | Not established | Confirmed | No first-party runtime gameplay scripts before onboarding |
| Networking topology | Host Mode is the MVP working assumption | Likely | Project execution plan and Fusion bootstrap spike |
| Scene composition | Main scene is the current composition root; its generated hierarchy is asset-informed and idempotent | Confirmed | Build Settings, `ActionHuntersSceneBuilder.cs` |

## Coding Conventions

- Namespace style: `ActionHunters.<Area>` for first-party code
- Serialized fields: prefer `[SerializeField] private` and explicit references
- Async: no project convention established; follow Fusion task/callback patterns
- Comments/docs: explain authority, lifecycle, and non-obvious constraints

## Testing And Validation

- EditMode tests: none yet
- PlayMode tests: none yet
- CI/build validation: none detected
- Current baseline: `Main` scene builder uses imported production assets directly; validation is repeated after each generated-scene rebuild

## Available Unity Tooling

| Capability | Status | Evidence |
| --- | --- | --- |
| Unity Editor UI control | available | Computer Use connection to Unity 6000.3.8f1 |
| Unity Console read | available through visible Editor UI | Console tab and counters |
| Scene inspection | available through files and Editor UI | `Main.unity`, Hierarchy |
| Direct Unity MCP bridge | unavailable | no Unity-side MCP provider detected |
| Test runner automation | unverified | package installed, no bridge or tests detected |

## Important Constraints

- Preserve user-created `Main.unity` and Build Settings changes.
- Treat `Assets/Photon/` as vendor code.
- Keep authoritative multiplayer simulation in Fusion callbacks, not ordinary `Update`.
- Do not expose or copy the configured Photon App ID.

## Unknowns And Confidence

- Final input mappings, gameplay architecture, target hardware, and production scene flow are unknown.
- Host Mode remains a working assumption until a two-peer spike validates it.
- The Notion-referenced asset packages are imported and mapped in `Docs/AI/ActionHuntersAssetMapping.md`. GUI Pro Bundle1 itself contains only the publisher handoff/readme, so the cached GUI Pro Minimal Game Dark package supplies the actual HUD sprites.

## Source Files Inspected

- `ProjectSettings/ProjectVersion.txt`
- `ProjectSettings/EditorBuildSettings.asset`
- `Packages/manifest.json`
- `Assets/Scenes/Main.unity`
- `Assets/InputSystem_Actions.inputactions`
- `Assets/Photon/Fusion/build_info.txt`
- `Assets/Photon/Fusion/Runtime/FusionBootstrap.cs`
- `Assets/Photon/Fusion/Runtime/NetworkSceneManagerDefault.cs`
- `Assets/ActionHunters/Editor/ActionHuntersSceneBuilder.cs`
- `Docs/AI/ActionHuntersAssetMapping.md`

<!-- unity-onboarding:generated:end -->
