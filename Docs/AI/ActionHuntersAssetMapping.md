# Action Hunters Notion Asset Mapping

Source: `Action Hunters` planning page in Notion.

The referenced packages are imported into this Unity project and `Main.unity` now instantiates their production prefabs or sprites directly. The simple first-party geometry remains only for arena blockout, collision readability, team zones, and VFX pedestals.

## Scene mapping

| Notion asset | Imported root | Main scene usage |
| --- | --- | --- |
| KayKit - Platformer Pack | `Assets/KayKit/Packs/KayKit - Platformer Pack (for Unity)` | Blue/red platforms, supply chests, pipes, yellow spike traps, team flags, and jump pads under `Imported_KayKit_Platformer_Prefabs` |
| KayKit - Adventurers Character Pack | `Assets/KayKit/Characters/KayKit - Adventurers (for Unity)` | Knight, Ranger, Mage, and Barbarian production prefabs for both teams under `AssetReferences_KayKit_Adventurers` |
| Monsters Pack 04 | `Assets/NOTFUN/Monsters Pack 04` | Catcher, Imp, Treestor, and Spike medium evolution prefabs under `AssetReferences_NOTFUN_MonstersPack04` |
| Elemental Spells Full Pack VFX | `Assets/PixPlays` | Water shield, fire projectile, wind beam, and earth AOE prefabs under `AssetReferences_PixPlays_ElementalSpells` |
| GUI Pro - Bundle1 | `Assets/Layer Lab/GUI Pro-Bundle1` | The downloaded Bundle1 package contains the publisher handoff/readme only |
| GUI Pro - Minimal Game Dark | `Assets/Layer Lab/GUI Pro-MinimalGame` | Real GUI Pro HUD, title, and resource-bar sprites used by `HUD_GUIPro_MinimalGameDark` |

## Selected production assets

- Hunters: `Knight.prefab`, `Ranger.prefab`, `Mage.prefab`, `Barbarian.prefab`
- Monsters: `Catcher_Medium.prefab`, `Imp_Medium.prefab`, `Treestor_Medium.prefab`, `Spike_Medium.prefab`
- VFX: `WaterShield.prefab`, `Fireball.prefab`, `WindBeam.prefab`, `EarthSlamSpikesAoeVFX.prefab`
- GUI: `ResourceBar_Bg.png`, `Title_02_NoDeco_Blue.png`, `Title_02_NoDeco_Red.png`

## Composition rules

1. Keep vendor prefabs visual-only until gameplay ownership and Fusion authority are implemented in first-party components.
2. Preserve mirrored blue/red silhouettes and the neutral yellow/orange objective language.
3. Keep the imported prefab instances connected to their source assets; make local scene overrides only for transforms and names.
4. Treat the current Elemental Spells content as Built-in-renderer source material; validate or upgrade shaders before shipping on URP.
5. Avoid GUI Pro demo-scene prefabs in the gameplay scene. They are not required for the HUD and some legacy demo prefabs report import compatibility errors in Unity 6.

## Current arena scale

- Playable island blockout: approximately `50 x 34` Unity units, expanded from the original `30 x 20` prototype.
- Team bases sit at `x = +/-18`; hunter displays sit at `x = +/-21.5`.
- North/south contest routes and monster camps sit near `z = +/-10.5`, leaving a larger central approach and meaningful flanking space.
- Outer rails sit near `x = +/-24.35` and `z = +/-16.35`.
