# Unity Project Context

<!-- unity-onboarding:generated:start -->

## Project Summary

- Project root: `C:/Github/action_hunters/action_hunters`
- Last analyzed: 2026-07-22
- Last analyzed commit: `0726349` (working tree contains the offline-demo implementation)
- State: Phase 1 offline vertical slice is playable in `Main`; production networking is still a later phase.

## Confirmed Environment

- Unity version: Unity 6.3 LTS, `6000.3.8f1`
- Render pipeline: Universal Render Pipeline `17.3.0`
- Input system: Unity Input System `1.18.0` with the template input action asset
- Active Editor build target: WebGL; a fresh player build has not been produced for this vertical slice

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
| `ActionHunters.Runtime` | Offline demo rules, combatants, match flow, camera, and HUD | Unity, Input System | First-party runtime asmdef under `Assets/ActionHunters/Runtime` |
| `ActionHunters.EditModeTests` | Pure gameplay-rule tests | Runtime assembly, NUnit, Test Framework | Nine EditMode cases |
| `Assembly-CSharp-Editor` | First-party editor tooling | UnityEditor, runtime and vendor assemblies | Scene builder lives under `Assets/ActionHunters/Editor` |
| `Fusion.Unity` | Photon Fusion Unity integration | Fusion runtime assemblies | Vendor-owned; do not edit |

## Scenes And Startup Flow

- Build scenes: `Assets/Scenes/Main.unity`
- Likely startup scene: `Main`
- Scene loading flow: `Main` starts the local `Offline_Demo_VerticalSlice`. The existing Fusion bootstrap remains serialized but inactive until the networking phase.

## Architecture

| Pattern | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| Gameplay architecture | Config-driven local match controller with combatant, camera, and HUD components | Confirmed | `Assets/ActionHunters/Runtime`, generated `Main` references |
| Networking topology | Host Mode is the MVP working assumption | Likely | Project execution plan and Fusion bootstrap spike |
| Scene composition | Main scene is the current composition root; its generated hierarchy is asset-informed and idempotent | Confirmed | Build Settings, `ActionHuntersSceneBuilder.cs` |

## Coding Conventions

- Namespace style: `ActionHunters.<Area>` for first-party code
- Serialized fields: prefer `[SerializeField] private` and explicit references
- Async: no project convention established; follow Fusion task/callback patterns
- Comments/docs: explain authority, lifecycle, and non-obvious constraints

## Testing And Validation

- EditMode tests: 9/9 passing for hiring, winner resolution, and timer formatting (verified in Unity Test Runner on 2026-07-22)
- PlayMode tests: none yet
- CI/build validation: none detected
- Current baseline: solution compile succeeds with zero warnings/errors; `Main` scene generation is idempotent; an Editor Play smoke ran for about 2.5 minutes with zero Console warnings/errors

## Available Unity Tooling

| Capability | Status | Evidence |
| --- | --- | --- |
| Unity Editor UI control | available | Computer Use connection to Unity 6000.3.8f1 |
| Unity Console read | available through visible Editor UI | Console tab and counters |
| Scene inspection | available through files and Editor UI | `Main.unity`, Hierarchy |
| Direct Unity MCP bridge | unavailable | no Unity-side MCP provider detected |
| Test runner automation | available through visible Editor UI | 9/9 EditMode run verified |

## Important Constraints

- Treat `ActionHuntersSceneBuilder.cs` as the source of truth for generated content under `__ActionHunters`; direct edits under that root will be replaced on regeneration.
- Preserve the user-adjusted blue/red pipe positions in the builder source when regenerating `Main`.
- Treat `Assets/Photon/` as vendor code.
- Keep authoritative multiplayer simulation in Fusion callbacks, not ordinary `Update`.
- Do not expose or copy the configured Photon App ID.

## Unknowns And Confidence

- Production target hardware, online lobby/session UX, authoritative state ownership, and production scene flow remain unresolved.
- Host Mode remains a working assumption until a two-peer spike validates it.
- The offline slice intentionally uses flat transform movement and arena bounds; navigation/collision authoring is deferred.
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
- `Assets/ActionHunters/Runtime/*.cs`
- `Assets/ActionHunters/Tests/EditMode/DemoGameRulesTests.cs`
- `Assets/ActionHunters/Config/DemoGameConfig.asset`
- `Docs/AI/ActionHuntersAssetMapping.md`

<!-- unity-onboarding:generated:end -->
