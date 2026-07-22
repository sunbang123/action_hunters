# Action Hunters Offline Demo Playbook

This is the Phase 1 local vertical slice derived from the Action Hunters Notion plan. It is deliberately offline: the serialized `Network_Fusion` root is inactive until the networking phase.

## Run the demo

1. Open `Assets/Scenes/Main.unity` in Unity `6000.3.8f1`.
2. If generated content is missing, run `Action Hunters > Build Asset-Informed Main Scene` once.
3. Enter Play Mode and allow the three-second countdown to complete.

The scene builder owns everything under `__ActionHunters`. Put persistent scene changes in `ActionHuntersSceneBuilder.cs`, not directly under that generated root.

## Controls

| Action | Keyboard and mouse | Gamepad |
| --- | --- | --- |
| Move | WASD / arrow keys | Left stick |
| Basic attack | Left mouse / Enter | Right trigger |
| Role skill | Space | Right bumper |
| Select hunter | 1-4 / Tab | D-pad |
| Hire at blue base | E | A / south button |
| Restart match | R | — |

## Demo loop

- A match starts after a three-second countdown and runs for five minutes.
- Blue begins with one directly controlled Guardian and 30 gold. Red begins with two AI hunters.
- Neutral monsters award gold; the boss also awards score. Hunter defeats award score.
- At 60 gold, return to the blue base and hire the next roster slot. Up to four Blue hunters can be active, with non-selected hunters controlled by AI.
- A tied regulation match enters a 60-second sudden death. The first hunter defeat ends sudden death immediately.
- The HUD shows timer, team score, gold/hire cost, roster health/lock/respawn state, and context help. Press R to reset the match.

## Verification baseline — 2026-07-22

- `dotnet build action_hunters.sln --no-restore`: zero warnings, zero errors.
- Unity EditMode Test Runner: 9 passed, 0 failed.
- Scene-builder idempotence: one inactive `Network_Fusion` root after regeneration; the existing config asset was not rewritten.
- Editor Play smoke: countdown, regulation timer, AI movement/combat, monster and boss defeats, hunter death/respawn, score/health HUD updates, and clean Play Mode exit observed for about 2.5 minutes.
- Unity Console during smoke: zero warnings, zero errors.

No fresh WebGL or Windows player artifact was produced in this pass; validation covers the Unity Editor demo.
