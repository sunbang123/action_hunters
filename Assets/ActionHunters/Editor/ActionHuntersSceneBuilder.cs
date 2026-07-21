using System.Collections.Generic;
using Fusion;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ActionHunters.Editor
{
    internal static class ActionHuntersSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string RootName = "__ActionHunters";
        private const string AssetRoot = "Assets/ActionHunters";
        private const string MaterialsPath = AssetRoot + "/Materials";
        private const string PrefabsPath = AssetRoot + "/Prefabs";
        private const string RunnerPrefabPath = PrefabsPath + "/NetworkRunner.prefab";
        private const string AdventurersPrefabRoot = "Assets/KayKit/Characters/KayKit - Adventurers (for Unity)/Prefabs/Characters";
        private const string PlatformerPrefabRoot = "Assets/KayKit/Packs/KayKit - Platformer Pack (for Unity)/Prefabs";
        private const string MonstersPrefabRoot = "Assets/NOTFUN/Monsters Pack 04/Prefab";
        private const string GuiSpriteRoot = "Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Dark/Sprites";

        [MenuItem("Action Hunters/Build Asset-Informed Main Scene")]
        private static void BuildAssetInformedMainScene()
        {
            EnsureFolder(AssetRoot, "Materials");
            EnsureFolder(AssetRoot, "Prefabs");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RemoveGeneratedRoot();

            var palette = CreatePalette();
            var root = new GameObject(RootName);

            ConfigureCamera();
            ConfigureLighting();
            CreateNetworkBootstrap();
            CreateArena(root.transform, palette);
            CreateHunterShowcase(root.transform, palette);
            CreateMonsterCamps(root.transform, palette);
            CreateElementalVfxAnchors(root.transform, palette);
            CreateGuiProInspiredHud(root.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EnsureSceneInBuildSettings(ScenePath);

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Action Hunters] Main scene rebuilt with imported Notion assets: " +
                      "KayKit Adventurers/Platformer, NOTFUN Monsters Pack 04, PixPlays Elemental Spells and GUI Pro Minimal Game Dark.");
        }

        private static void RemoveGeneratedRoot()
        {
            var existingRoot = GameObject.Find(RootName);
            if (existingRoot != null)
            {
                Object.DestroyImmediate(existingRoot);
            }
        }

        private static void ConfigureCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.transform.SetPositionAndRotation(
                new Vector3(0f, 34f, -38f),
                Quaternion.Euler(45f, 0f, 0f));
            camera.fieldOfView = 55f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 250f;
            camera.backgroundColor = new Color(0.12f, 0.18f, 0.3f);
        }

        private static void ConfigureLighting()
        {
            var light = Object.FindFirstObjectByType<Light>();
            if (light == null || light.type != LightType.Directional)
            {
                var lightObject = new GameObject("Directional Light");
                light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
            }

            light.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            light.color = new Color(1f, 0.95f, 0.82f);
            light.intensity = 1.45f;
            light.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.28f, 0.4f, 0.62f);
            RenderSettings.ambientEquatorColor = new Color(0.2f, 0.27f, 0.34f);
            RenderSettings.ambientGroundColor = new Color(0.08f, 0.1f, 0.14f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.15f, 0.23f, 0.36f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 35f;
            RenderSettings.fogEndDistance = 95f;
        }

        private static void CreateNetworkBootstrap()
        {
            var existingNetworkRoot = GameObject.Find("Network_Fusion");
            if (existingNetworkRoot != null)
            {
                Object.DestroyImmediate(existingNetworkRoot);
            }

            var networkRoot = new GameObject("Network_Fusion");

            var runnerPrefab = CreateRunnerPrefab();
            var bootstrap = networkRoot.AddComponent<FusionBootstrap>();
            bootstrap.RunnerPrefab = runnerPrefab.GetComponent<NetworkRunner>();
            bootstrap.StartMode = FusionBootstrap.StartModes.UserInterface;
            bootstrap.AutoHideGUI = false;
            bootstrap.AutoClients = 1;
            bootstrap.ClientStartDelay = 0.1f;
            bootstrap.ServerPort = 0;
            bootstrap.DefaultRoomName = "ActionHunters-Spike";
            bootstrap.InitialScenePath = ScenePath;
            bootstrap.AutoConnectVirtualInstances = false;

            networkRoot.AddComponent<FusionBootstrapDebugGUI>();
        }

        private static GameObject CreateRunnerPrefab()
        {
            var source = new GameObject("NetworkRunner");
            source.AddComponent<NetworkRunner>();
            source.AddComponent<NetworkSceneManagerDefault>();
            source.AddComponent<NetworkObjectProviderDefault>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(source, RunnerPrefabPath);
            Object.DestroyImmediate(source);
            return prefab;
        }

        private static void CreateArena(Transform root, IReadOnlyDictionary<string, Material> palette)
        {
            var arena = CreateGroup("Arena_KayKit_Platformer_Style", root);
            var foundation = CreateGroup("Floating_Island_Foundation", arena);

            CreateBlock("Island_Core", new Vector3(0f, -0.75f, 0f), new Vector3(50f, 1.5f, 34f), palette["Ground"], foundation);
            CreateBlock("Island_Underlayer", new Vector3(0f, -1.7f, 0f), new Vector3(46f, 0.5f, 30f), palette["GroundDark"], foundation);
            CreateBlock("Blue_Territory", new Vector3(-16.5f, 0.03f, 0f), new Vector3(17f, 0.1f, 32f), palette["Blue"], foundation);
            CreateBlock("Neutral_Territory", new Vector3(0f, 0.04f, 0f), new Vector3(16f, 0.12f, 32f), palette["Sand"], foundation);
            CreateBlock("Red_Territory", new Vector3(16.5f, 0.03f, 0f), new Vector3(17f, 0.1f, 32f), palette["Red"], foundation);

            var platforms = CreateGroup("Modular_Platforms_And_Bridges", arena);
            CreateTeamPlatform("Blue_Base", new Vector3(-18f, 0f, 0f), palette["BlueAccent"], palette, platforms);
            CreateTeamPlatform("Red_Base", new Vector3(18f, 0f, 0f), palette["RedAccent"], palette, platforms);
            CreateBlock("Blue_Upper_North", new Vector3(-10.5f, 0.65f, 10.5f), new Vector3(6f, 1.3f, 4.5f), palette["BlueSoft"], platforms);
            CreateBlock("Blue_Upper_South", new Vector3(-10.5f, 0.65f, -10.5f), new Vector3(6f, 1.3f, 4.5f), palette["BlueSoft"], platforms);
            CreateBlock("Red_Upper_North", new Vector3(10.5f, 0.65f, 10.5f), new Vector3(6f, 1.3f, 4.5f), palette["RedSoft"], platforms);
            CreateBlock("Red_Upper_South", new Vector3(10.5f, 0.65f, -10.5f), new Vector3(6f, 1.3f, 4.5f), palette["RedSoft"], platforms);
            CreateBlock("Bridge_North", new Vector3(0f, 1.35f, 10.5f), new Vector3(16f, 0.35f, 3f), palette["Yellow"], platforms);
            CreateBlock("Bridge_South", new Vector3(0f, 1.35f, -10.5f), new Vector3(16f, 0.35f, 3f), palette["Yellow"], platforms);
            CreateCylinder("Central_Objective_Platform", new Vector3(0f, 0.45f, 0f), new Vector3(6.5f, 0.45f, 6.5f), palette["Objective"], platforms);
            CreateCylinder("Central_Objective_Core", new Vector3(0f, 1.25f, 0f), new Vector3(1.35f, 0.8f, 1.35f), palette["ObjectiveGlow"], platforms);

            var rails = CreateGroup("Rounded_Rails_And_Signage", arena);
            CreateRailRun("North_Rail", new Vector3(0f, 1.25f, 16.35f), 48f, true, palette["White"], rails);
            CreateRailRun("South_Rail", new Vector3(0f, 1.25f, -16.35f), 48f, true, palette["White"], rails);
            CreateRailRun("West_Rail", new Vector3(-24.35f, 1.25f, 0f), 32f, false, palette["BlueAccent"], rails);
            CreateRailRun("East_Rail", new Vector3(24.35f, 1.25f, 0f), 32f, false, palette["RedAccent"], rails);
            CreateFlag("Blue_Flag", new Vector3(-22.2f, 1.1f, 13.7f), palette["BlueAccent"], rails);
            CreateFlag("Red_Flag", new Vector3(22.2f, 1.1f, 13.7f), palette["RedAccent"], rails);

            var props = CreateGroup("Platformer_Props_And_Trap_Silhouettes", arena);
            CreatePlatformerAssetSet(props);
        }

        private static void CreatePlatformerAssetSet(Transform parent)
        {
            var platforms = CreateGroup("Imported_KayKit_Platformer_Prefabs", parent);

            InstantiatePrefab(PlatformerPath("blue", "platform_4x4x1_blue"), "Blue_North_Platform", new Vector3(-10.5f, 1.3f, 10.5f), Quaternion.identity, new Vector3(1.35f, 1f, 1.35f), platforms);
            InstantiatePrefab(PlatformerPath("blue", "platform_4x4x1_blue"), "Blue_South_Platform", new Vector3(-10.5f, 1.3f, -10.5f), Quaternion.identity, new Vector3(1.35f, 1f, 1.35f), platforms);
            InstantiatePrefab(PlatformerPath("red", "platform_4x4x1_red"), "Red_North_Platform", new Vector3(10.5f, 1.3f, 10.5f), Quaternion.identity, new Vector3(1.35f, 1f, 1.35f), platforms);
            InstantiatePrefab(PlatformerPath("red", "platform_4x4x1_red"), "Red_South_Platform", new Vector3(10.5f, 1.3f, -10.5f), Quaternion.identity, new Vector3(1.35f, 1f, 1.35f), platforms);

            InstantiatePrefab(PlatformerPath("blue", "chest_large_blue"), "Blue_Supply_Chest", new Vector3(-14.5f, 0.08f, 5.5f), Quaternion.Euler(0f, 35f, 0f), Vector3.one, platforms);
            InstantiatePrefab(PlatformerPath("red", "chest_large_red"), "Red_Supply_Chest", new Vector3(14.5f, 0.08f, -5.5f), Quaternion.Euler(0f, 215f, 0f), Vector3.one, platforms);
            InstantiatePrefab(PlatformerPath("blue", "pipe_straight_A_blue"), "Blue_Pipe", new Vector3(-21f, 0.08f, -11.5f), Quaternion.identity, Vector3.one, platforms);
            InstantiatePrefab(PlatformerPath("red", "pipe_straight_A_red"), "Red_Pipe", new Vector3(21f, 0.08f, 11.5f), Quaternion.identity, Vector3.one, platforms);
            InstantiatePrefab(PlatformerPath("yellow", "floor_spikes_trap_4x4x1_yellow"), "North_Spike_Trap", new Vector3(0f, 1.55f, 13.2f), Quaternion.identity, new Vector3(0.8f, 0.8f, 0.8f), platforms);
            InstantiatePrefab(PlatformerPath("yellow", "floor_spikes_trap_4x4x1_yellow"), "South_Spike_Trap", new Vector3(0f, 1.55f, -13.2f), Quaternion.identity, new Vector3(0.8f, 0.8f, 0.8f), platforms);
            InstantiatePrefab(PlatformerPath("blue", "flag_A_blue"), "Blue_Flag_Imported", new Vector3(-22.2f, 0.08f, 13.7f), Quaternion.identity, Vector3.one, platforms);
            InstantiatePrefab(PlatformerPath("red", "flag_A_red"), "Red_Flag_Imported", new Vector3(22.2f, 0.08f, 13.7f), Quaternion.identity, Vector3.one, platforms);
            InstantiatePrefab(PlatformerPath("blue", "spring_pad_blue"), "Blue_Jump_Pad", new Vector3(-17f, 0.08f, -12.5f), Quaternion.identity, Vector3.one, platforms);
            InstantiatePrefab(PlatformerPath("red", "spring_pad_red"), "Red_Jump_Pad", new Vector3(17f, 0.08f, 12.5f), Quaternion.identity, Vector3.one, platforms);

            InstantiatePrefab(PlatformerPath("blue", "barrier_4x1x2_blue"), "Blue_Mid_Cover_North", new Vector3(-15f, 0.08f, 8f), Quaternion.Euler(0f, 90f, 0f), Vector3.one, platforms);
            InstantiatePrefab(PlatformerPath("blue", "barrier_4x1x2_blue"), "Blue_Mid_Cover_South", new Vector3(-15f, 0.08f, -8f), Quaternion.Euler(0f, 90f, 0f), Vector3.one, platforms);
            InstantiatePrefab(PlatformerPath("red", "barrier_4x1x2_red"), "Red_Mid_Cover_North", new Vector3(15f, 0.08f, 8f), Quaternion.Euler(0f, 90f, 0f), Vector3.one, platforms);
            InstantiatePrefab(PlatformerPath("red", "barrier_4x1x2_red"), "Red_Mid_Cover_South", new Vector3(15f, 0.08f, -8f), Quaternion.Euler(0f, 90f, 0f), Vector3.one, platforms);
        }

        private static string PlatformerPath(string color, string assetName)
        {
            return $"{PlatformerPrefabRoot}/{color}/{assetName}.prefab";
        }

        private static void CreateTeamPlatform(string name, Vector3 position, Material accent, IReadOnlyDictionary<string, Material> palette, Transform parent)
        {
            var teamPlatform = CreateGroup(name, parent);
            teamPlatform.localPosition = position;
            CreateCylinder("Spawn_Pad", new Vector3(0f, 0.25f, 0f), new Vector3(6f, 0.25f, 6f), accent, teamPlatform);
            CreateCylinder("Spawn_Rim", new Vector3(0f, 0.52f, 0f), new Vector3(4.7f, 0.08f, 4.7f), palette["White"], teamPlatform);
            CreateBlock("Back_Step", new Vector3(0f, 0.45f, 4.6f), new Vector3(8f, 0.9f, 2.4f), accent, teamPlatform);
        }

        private static void CreateRailRun(string name, Vector3 position, float length, bool alongX, Material material, Transform parent)
        {
            var rail = CreateGroup(name, parent);
            rail.localPosition = position;
            var beamScale = alongX ? new Vector3(length, 0.18f, 0.18f) : new Vector3(0.18f, 0.18f, length);
            CreateBlock("Top_Beam", new Vector3(0f, 0.55f, 0f), beamScale, material, rail);

            var count = Mathf.CeilToInt(length / 3.5f);
            for (var index = 0; index <= count; index++)
            {
                var distance = Mathf.Lerp(-length * 0.5f, length * 0.5f, index / (float)count);
                var localPosition = alongX ? new Vector3(distance, 0f, 0f) : new Vector3(0f, 0f, distance);
                CreateCylinder($"Post_{index:00}", localPosition, new Vector3(0.16f, 0.65f, 0.16f), material, rail);
            }
        }

        private static void CreateFlag(string name, Vector3 position, Material flagMaterial, Transform parent)
        {
            var flag = CreateGroup(name, parent);
            flag.localPosition = position;
            CreateCylinder("Pole", new Vector3(0f, 1.25f, 0f), new Vector3(0.12f, 1.25f, 0.12f), flagMaterial, flag);
            CreateBlock("Banner", new Vector3(0.7f, 2.05f, 0f), new Vector3(1.4f, 0.85f, 0.08f), flagMaterial, flag);
        }

        private static void CreateCrate(string name, Vector3 position, Material accent, IReadOnlyDictionary<string, Material> palette, Transform parent)
        {
            var crate = CreateGroup(name, parent);
            crate.localPosition = position;
            CreateBlock("Body", Vector3.zero, new Vector3(1.35f, 1.35f, 1.35f), palette["Wood"], crate);
            CreateBlock("Band_X", new Vector3(0f, 0f, -0.7f), new Vector3(0.2f, 1.15f, 0.08f), accent, crate, Quaternion.Euler(0f, 0f, 45f));
            CreateBlock("Band_Y", new Vector3(0f, 0f, -0.71f), new Vector3(0.2f, 1.15f, 0.08f), accent, crate, Quaternion.Euler(0f, 0f, -45f));
        }

        private static void CreatePipe(string name, Vector3 position, Material material, Transform parent)
        {
            var pipe = CreateGroup(name, parent);
            pipe.localPosition = position;
            CreateCylinder("Pipe_Stem", new Vector3(0f, 0.55f, 0f), new Vector3(0.65f, 0.55f, 0.65f), material, pipe);
            CreateCylinder("Pipe_Lip", new Vector3(0f, 1.12f, 0f), new Vector3(0.85f, 0.18f, 0.85f), material, pipe);
        }

        private static void CreateSpikeTrap(string name, Vector3 position, Material material, Transform parent)
        {
            var trap = CreateGroup(name, parent);
            trap.localPosition = position;
            CreateBlock("Trap_Base", Vector3.zero, new Vector3(2.8f, 0.18f, 1.1f), material, trap);
            for (var index = -2; index <= 2; index++)
            {
                CreateBlock($"Spike_{index + 3}", new Vector3(index * 0.48f, 0.38f, 0f), new Vector3(0.2f, 0.7f, 0.2f), material, trap, Quaternion.Euler(0f, 0f, 45f));
            }
        }

        private static void CreateHunterShowcase(Transform root, IReadOnlyDictionary<string, Material> palette)
        {
            var showcase = CreateGroup("AssetReferences_KayKit_Adventurers", root);
            var blue = CreateGroup("Blue_Hunter_Prefab_Slots", showcase);
            var red = CreateGroup("Red_Hunter_Prefab_Slots", showcase);

            var roles = new[] { "Knight", "Ranger", "Mage", "Barbarian" };
            var zPositions = new[] { -10.5f, -3.5f, 3.5f, 10.5f };
            for (var index = 0; index < roles.Length; index++)
            {
                CreateHunter($"Blue_{roles[index]}", roles[index], new Vector3(-21.5f, 0f, zPositions[index]), 90f, palette["BlueAccent"], blue);
                CreateHunter($"Red_{roles[index]}", roles[index], new Vector3(21.5f, 0f, zPositions[index]), -90f, palette["RedAccent"], red);
            }
        }

        private static void CreateHunter(string name, string role, Vector3 position, float yaw, Material teamMaterial, Transform parent)
        {
            var hunter = CreateGroup(name, parent);
            hunter.localPosition = position;
            CreateCylinder("Team_Pad", new Vector3(0f, 0.12f, 0f), new Vector3(1.05f, 0.12f, 1.05f), teamMaterial, hunter);
            InstantiatePrefab($"{AdventurersPrefabRoot}/{role}.prefab", $"{role}_KayKit_Prefab", new Vector3(0f, 0.24f, 0f), Quaternion.Euler(0f, yaw, 0f), Vector3.one, hunter);
        }

        private static void CreateMonsterCamps(Transform root, IReadOnlyDictionary<string, Material> palette)
        {
            var camps = CreateGroup("AssetReferences_NOTFUN_MonstersPack04", root);
            CreateMonsterCamp("North_Catcher_Camp", "Catcher_Medium", new Vector3(-4.5f, 1.6f, 10.5f), 180f, palette["MonsterGreen"], palette, camps);
            CreateMonsterCamp("North_Imp_Camp", "Imp_Medium", new Vector3(4.5f, 1.6f, 10.5f), 180f, palette["MonsterPurple"], palette, camps);
            CreateMonsterCamp("South_Treestor_Camp", "Treestor_Medium", new Vector3(-4.5f, 1.6f, -10.5f), 0f, palette["MonsterGreen"], palette, camps);
            CreateMonsterCamp("South_Spike_Camp", "Spike_Medium", new Vector3(4.5f, 1.6f, -10.5f), 0f, palette["MonsterPurple"], palette, camps);
        }

        private static void CreateMonsterCamp(string name, string prefabName, Vector3 position, float yaw, Material padMaterial, IReadOnlyDictionary<string, Material> palette, Transform parent)
        {
            var camp = CreateGroup(name, parent);
            camp.localPosition = position;
            CreateCylinder("Evolution_Pad", new Vector3(0f, 0.08f, 0f), new Vector3(1.35f, 0.08f, 1.35f), padMaterial, camp);
            CreateCylinder("Evolution_Rim", new Vector3(0f, 0.17f, 0f), new Vector3(1.05f, 0.03f, 1.05f), palette["Danger"], camp);
            InstantiatePrefab($"{MonstersPrefabRoot}/{prefabName}.prefab", $"{prefabName}_NOTFUN_Prefab", new Vector3(0f, 0.2f, 0f), Quaternion.Euler(0f, yaw, 0f), new Vector3(0.8f, 0.8f, 0.8f), camp);
        }

        private static void CreateElementalVfxAnchors(Transform root, IReadOnlyDictionary<string, Material> palette)
        {
            var anchors = CreateGroup("AssetReferences_PixPlays_ElementalSpells", root);
            CreateVfxAnchor("Water_Shield", "Assets/PixPlays/ElementalShields/WaterShield/Version_BuiltIn/WaterShield.prefab", new Vector3(-5f, 1.35f, 0f), palette["WaterVfx"], anchors, 7f);
            CreateVfxAnchor("Fire_Projectile", "Assets/PixPlays/ElementalProjectiles/Fireball/Version_BuiltIn/Fireball.prefab", new Vector3(5f, 1.35f, 0f), palette["FireVfx"], anchors, 7f);
            CreateVfxAnchor("Wind_Beam", "Assets/PixPlays/ElementalBeams/WindBeam/Version_BuiltIn/WindBeam.prefab", new Vector3(0f, 2.2f, 5f), palette["AirVfx"], anchors, 5f);
            CreateVfxAnchor("Earth_AOE", "Assets/PixPlays/ElementalAOE/EarthAOE/Version_BuiltIn/EarthSlamSpikesAoeVFX.prefab", new Vector3(0f, 2.2f, -5f), palette["EarthVfx"], anchors, 5f);
        }

        private static void CreateVfxAnchor(string name, string prefabPath, Vector3 position, Material material, Transform parent, float lightRange)
        {
            var anchor = CreateGroup(name, parent);
            anchor.localPosition = position;
            CreateCylinder("VFX_Pedestal", new Vector3(0f, -0.12f, 0f), new Vector3(0.75f, 0.08f, 0.75f), material, anchor);
            InstantiatePrefab(prefabPath, name + "_PixPlays_Prefab", Vector3.zero, Quaternion.identity, Vector3.one, anchor);

            var pointLight = anchor.gameObject.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = material.color;
            pointLight.intensity = 2.2f;
            pointLight.range = lightRange;
            pointLight.shadows = LightShadows.None;
        }

        private static void CreateGuiProInspiredHud(Transform root)
        {
            var hud = new GameObject("HUD_GUIPro_MinimalGameDark");
            hud.transform.SetParent(root, false);

            var canvas = hud.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            var scaler = hud.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            hud.AddComponent<GraphicRaycaster>();

            var resourceBar = LoadSprite($"{GuiSpriteRoot}/HUD/ResourceBar_Bg.png");
            var blueTitle = LoadSprite($"{GuiSpriteRoot}/Title/Title_02_NoDeco_Blue.png");
            var redTitle = LoadSprite($"{GuiSpriteRoot}/Title/Title_02_NoDeco_Red.png");
            var top = CreatePanel("Top_Round_Status", hud.transform, new Vector2(0.5f, 1f), new Vector2(900f, 116f), new Vector2(0f, -24f), Color.white, resourceBar);
            CreatePanel("Blue_Score_Backplate", top, new Vector2(0f, 0.5f), new Vector2(300f, 82f), new Vector2(172f, 0f), Color.white, blueTitle);
            CreatePanel("Red_Score_Backplate", top, new Vector2(1f, 0.5f), new Vector2(300f, 82f), new Vector2(-172f, 0f), Color.white, redTitle);
            CreateUiText("Blue_Score", top, "BLUE  0", 29, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(260f, 72f), new Vector2(44f, 0f));
            CreateUiText("Red_Score", top, "0  RED", 29, TextAnchor.MiddleRight, new Vector2(1f, 0.5f), new Vector2(260f, 72f), new Vector2(-44f, 0f));
            CreateUiText("Round_Timer", top, "02:30", 38, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(220f, 72f), Vector2.zero);
            CreateUiText("Mode_Label", top, "HUNT & HOLD", 16, TextAnchor.UpperCenter, new Vector2(0.5f, 0f), new Vector2(260f, 28f), new Vector2(0f, 8f), new Color(1f, 0.76f, 0.18f));

            var blueRoster = CreateRosterPanel("Blue_Hunter_Roster", hud.transform, new Vector2(0f, 0.5f), new Vector2(32f, 0f), new Color(0.035f, 0.17f, 0.36f, 0.9f));
            var redRoster = CreateRosterPanel("Red_Hunter_Roster", hud.transform, new Vector2(1f, 0.5f), new Vector2(-32f, 0f), new Color(0.36f, 0.045f, 0.075f, 0.9f));
            CreateRosterRows(blueRoster, "BLUE HUNTERS", new[] { "KNIGHT", "RANGER", "MAGE", "BARBARIAN" }, TextAnchor.MiddleLeft);
            CreateRosterRows(redRoster, "RED HUNTERS", new[] { "KNIGHT", "RANGER", "MAGE", "BARBARIAN" }, TextAnchor.MiddleRight);

            var objective = CreatePanel("Objective_Card", hud.transform, new Vector2(0.5f, 0f), new Vector2(600f, 92f), new Vector2(0f, 28f), Color.white, resourceBar);
            CreateUiText("Objective", objective, "CAPTURE THE CORE  |  MONSTERS EMPOWER YOUR TEAM", 20, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.62f), new Vector2(560f, 38f), Vector2.zero, new Color(1f, 0.82f, 0.3f));
            CreateUiText("Network", objective, "Fusion Room: ActionHunters-Spike  |  Select Host or Client in Play Mode", 14, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.22f), new Vector2(560f, 30f), Vector2.zero, new Color(0.75f, 0.82f, 0.92f));
        }

        private static Transform CreateRosterPanel(string name, Transform parent, Vector2 anchor, Vector2 position, Color color)
        {
            var panel = CreatePanel(name, parent, anchor, new Vector2(280f, 330f), position, color);
            var rect = panel.GetComponent<RectTransform>();
            rect.pivot = new Vector2(anchor.x, 0.5f);
            return panel;
        }

        private static void CreateRosterRows(Transform parent, string title, IReadOnlyList<string> roles, TextAnchor alignment)
        {
            CreateUiText("Title", parent, title, 20, alignment, new Vector2(0.5f, 1f), new Vector2(238f, 44f), new Vector2(0f, -30f), new Color(1f, 0.82f, 0.3f));
            for (var index = 0; index < roles.Count; index++)
            {
                var y = -82f - index * 57f;
                var row = CreatePanel($"Slot_{index + 1}_{roles[index]}", parent, new Vector2(0.5f, 1f), new Vector2(238f, 46f), new Vector2(0f, y), new Color(0.02f, 0.03f, 0.055f, 0.78f));
                CreateUiText("Role", row, $"{index + 1:00}   {roles[index]}", 17, alignment, new Vector2(0.5f, 0.5f), new Vector2(208f, 40f), Vector2.zero);
            }
        }

        private static Transform CreatePanel(string name, Transform parent, Vector2 anchor, Vector2 size, Vector2 position, Color color, Sprite sprite = null)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var image = panel.AddComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            if (sprite != null && sprite.border.sqrMagnitude > 0f)
            {
                image.type = Image.Type.Sliced;
            }
            image.raycastTarget = false;
            var outline = panel.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.84f, 0.36f, 0.55f);
            outline.effectDistance = new Vector2(2f, -2f);
            return panel.transform;
        }

        private static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                throw new System.IO.FileNotFoundException($"Required GUI Pro sprite was not imported: {path}");
            }

            return sprite;
        }

        private static GameObject InstantiatePrefab(string path, string name, Vector3 position, Quaternion rotation, Vector3 scale, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                throw new System.IO.FileNotFoundException($"Required Notion asset prefab was not imported: {path}");
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (instance == null)
            {
                throw new System.InvalidOperationException($"Could not instantiate prefab: {path}");
            }

            instance.name = name;
            instance.transform.localPosition = position;
            instance.transform.localRotation = rotation;
            instance.transform.localScale = scale;
            return instance;
        }

        private static void CreateUiText(string name, Transform parent, string value, int fontSize, TextAnchor alignment, Vector2 anchor, Vector2 size, Vector2 position, Color? color = null)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            var rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.color = color ?? Color.white;
            text.raycastTarget = false;
            text.text = value;
        }

        private static Dictionary<string, Material> CreatePalette()
        {
            return new Dictionary<string, Material>
            {
                ["Ground"] = CreateMaterial("Ground", new Color(0.18f, 0.23f, 0.28f), 0f, 0.12f),
                ["GroundDark"] = CreateMaterial("GroundDark", new Color(0.08f, 0.11f, 0.15f), 0f, 0.08f),
                ["Sand"] = CreateMaterial("Sand", new Color(0.68f, 0.58f, 0.36f), 0f, 0.15f),
                ["Blue"] = CreateMaterial("BlueZone", new Color(0.08f, 0.28f, 0.56f), 0f, 0.16f),
                ["BlueSoft"] = CreateMaterial("BlueSoft", new Color(0.13f, 0.48f, 0.82f), 0f, 0.18f),
                ["BlueAccent"] = CreateMaterial("BlueAccent", new Color(0.04f, 0.62f, 1f), 0.05f, 0.3f),
                ["Red"] = CreateMaterial("RedZone", new Color(0.56f, 0.09f, 0.13f), 0f, 0.16f),
                ["RedSoft"] = CreateMaterial("RedSoft", new Color(0.84f, 0.18f, 0.22f), 0f, 0.18f),
                ["RedAccent"] = CreateMaterial("RedAccent", new Color(1f, 0.13f, 0.17f), 0.05f, 0.3f),
                ["Yellow"] = CreateMaterial("Yellow", new Color(1f, 0.72f, 0.12f), 0f, 0.2f),
                ["Objective"] = CreateMaterial("Objective", new Color(0.96f, 0.52f, 0.06f), 0.05f, 0.28f),
                ["ObjectiveGlow"] = CreateMaterial("ObjectiveGlow", new Color(1f, 0.78f, 0.16f), 0f, 0.4f, new Color(1f, 0.34f, 0.02f) * 2.6f),
                ["White"] = CreateMaterial("White", new Color(0.86f, 0.91f, 0.96f), 0.05f, 0.24f),
                ["Wood"] = CreateMaterial("Wood", new Color(0.47f, 0.27f, 0.13f), 0f, 0.12f),
                ["Metal"] = CreateMaterial("Metal", new Color(0.62f, 0.7f, 0.78f), 0.45f, 0.45f),
                ["Danger"] = CreateMaterial("Danger", new Color(0.95f, 0.24f, 0.08f), 0f, 0.18f),
                ["Skin"] = CreateMaterial("Skin", new Color(0.95f, 0.68f, 0.48f), 0f, 0.22f),
                ["Cloth"] = CreateMaterial("Cloth", new Color(0.19f, 0.22f, 0.29f), 0f, 0.1f),
                ["Horn"] = CreateMaterial("Horn", new Color(0.9f, 0.78f, 0.55f), 0f, 0.18f),
                ["MonsterGreen"] = CreateMaterial("MonsterGreen", new Color(0.32f, 0.72f, 0.25f), 0f, 0.14f),
                ["MonsterPurple"] = CreateMaterial("MonsterPurple", new Color(0.62f, 0.25f, 0.78f), 0f, 0.14f),
                ["Magic"] = CreateMaterial("Magic", new Color(0.67f, 0.3f, 1f), 0f, 0.35f, new Color(0.4f, 0.08f, 1f) * 2f),
                ["WaterVfx"] = CreateMaterial("WaterVfx", new Color(0.12f, 0.58f, 1f), 0f, 0.4f, new Color(0.02f, 0.25f, 1f) * 2.5f),
                ["FireVfx"] = CreateMaterial("FireVfx", new Color(1f, 0.3f, 0.04f), 0f, 0.4f, new Color(1f, 0.08f, 0f) * 2.8f),
                ["AirVfx"] = CreateMaterial("AirVfx", new Color(0.72f, 1f, 0.92f), 0f, 0.4f, new Color(0.2f, 1f, 0.72f) * 2.2f),
                ["EarthVfx"] = CreateMaterial("EarthVfx", new Color(0.62f, 0.42f, 0.12f), 0f, 0.3f, new Color(0.8f, 0.3f, 0.04f) * 1.8f)
            };
        }

        private static Material CreateMaterial(string name, Color color, float metallic = 0f, float smoothness = 0.2f, Color? emission = null)
        {
            var path = $"{MaterialsPath}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader == null)
                {
                    throw new System.InvalidOperationException("A compatible Lit shader could not be found.");
                }

                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            if (emission.HasValue && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Transform CreateGroup(string name, Transform parent)
        {
            var group = new GameObject(name);
            group.transform.SetParent(parent, false);
            return group.transform;
        }

        private static GameObject CreateBlock(string name, Vector3 position, Vector3 scale, Material material, Transform parent, Quaternion? rotation = null)
        {
            return CreatePrimitive(PrimitiveType.Cube, name, position, scale, material, parent, rotation);
        }

        private static GameObject CreateCylinder(string name, Vector3 position, Vector3 scale, Material material, Transform parent, Quaternion? rotation = null)
        {
            return CreatePrimitive(PrimitiveType.Cylinder, name, position, scale, material, parent, rotation);
        }

        private static GameObject CreateSphere(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            return CreatePrimitive(PrimitiveType.Sphere, name, position, scale, material, parent, null);
        }

        private static GameObject CreateCapsule(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            return CreatePrimitive(PrimitiveType.Capsule, name, position, scale, material, parent, null);
        }

        private static GameObject CreatePrimitive(PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material, Transform parent, Quaternion? rotation)
        {
            var primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = position;
            primitive.transform.localRotation = rotation ?? Quaternion.identity;
            primitive.transform.localScale = scale;
            primitive.GetComponent<Renderer>().sharedMaterial = material;
            return primitive;
        }

        private static void EnsureFolder(string parent, string child)
        {
            var fullPath = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(fullPath))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static void EnsureSceneInBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            var found = false;
            for (var index = 0; index < scenes.Count; index++)
            {
                if (scenes[index].path != scenePath)
                {
                    continue;
                }

                scenes[index] = new EditorBuildSettingsScene(scenePath, true);
                found = true;
                break;
            }

            if (!found)
            {
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
