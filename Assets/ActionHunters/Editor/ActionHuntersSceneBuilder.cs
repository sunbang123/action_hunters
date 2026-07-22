using System.Collections.Generic;
using ActionHunters.Runtime;
using Fusion;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ActionHunters.Editor
{
    public static class ActionHuntersSceneBuilder
    {
        private enum GeneratedColliderMode
        {
            Primitive,
            Mesh,
            None
        }

        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string RootName = "__ActionHunters";
        private const string AssetRoot = "Assets/ActionHunters";
        private const string MaterialsPath = AssetRoot + "/Materials";
        private const string PrefabsPath = AssetRoot + "/Prefabs";
        private const string ConfigPath = AssetRoot + "/Config";
        private const string AnimationsPath = AssetRoot + "/Animations";
        private const string DemoConfigPath = ConfigPath + "/DemoGameConfig.asset";
        private const string RunnerPrefabPath = PrefabsPath + "/NetworkRunner.prefab";
        private const string AdventurersPrefabRoot = "Assets/KayKit/Characters/KayKit - Adventurers (for Unity)/Prefabs/Characters";
        private const string PlatformerPrefabRoot = "Assets/KayKit/Packs/KayKit - Platformer Pack (for Unity)/Prefabs";
        private const string MonstersPrefabRoot = "Assets/NOTFUN/Monsters Pack 04/Prefab";
        private const string GuiSpriteRoot = "Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Dark/Sprites";
        private const string WindHitPath = "Assets/PixPlays/ElementalProjectiles/Windbullet/Version_BuiltIn/WindbulletHit/WindbulletHit.prefab";
        private const string FireHitPath = "Assets/PixPlays/ElementalProjectiles/Fireball/Version_BuiltIn/FireballHit/FireballHit.prefab";
        private const string WaterHitPath = "Assets/PixPlays/ElementalProjectiles/Waterball/Version_BuiltIn/WaterballHit/WaterballHit.prefab";

        [MenuItem("Action Hunters/Build Asset-Informed Main Scene")]
        public static void BuildAssetInformedMainScene()
        {
            EnsureFolder(AssetRoot, "Materials");
            EnsureFolder(AssetRoot, "Prefabs");
            EnsureFolder(AssetRoot, "Config");
            EnsureFolder(AssetRoot, "Animations");

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
            var demoHud = CreateGuiProInspiredHud(root.transform);
            CreateDemoVerticalSlice(root.transform, demoHud);

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
            var rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var rootObject in rootObjects)
            {
                if (rootObject.name == "Network_Fusion")
                {
                    Object.DestroyImmediate(rootObject);
                }
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
            networkRoot.SetActive(false);
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
            CreateCylinder("Central_Objective_Platform", new Vector3(0f, 0.45f, 0f), new Vector3(6.5f, 0.45f, 6.5f), palette["Objective"], platforms, GeneratedColliderMode.Mesh);
            CreateCylinder("Central_Objective_Core", new Vector3(0f, 1.25f, 0f), new Vector3(1.35f, 0.8f, 1.35f), palette["ObjectiveGlow"], platforms, GeneratedColliderMode.Mesh);

            var rails = CreateGroup("Rounded_Rails_And_Signage", arena);
            CreateRailRun("North_Rail", new Vector3(0f, 1.25f, 16.35f), 48f, true, palette["White"], rails);
            CreateRailRun("South_Rail", new Vector3(0f, 1.25f, -16.35f), 48f, true, palette["White"], rails);
            CreateRailRun("West_Rail", new Vector3(-24.35f, 1.25f, 0f), 32f, false, palette["BlueAccent"], rails);
            CreateRailRun("East_Rail", new Vector3(24.35f, 1.25f, 0f), 32f, false, palette["RedAccent"], rails);
            CreateFlag("Blue_Flag", new Vector3(-22.2f, 1.1f, 13.7f), palette["BlueAccent"], rails);
            CreateFlag("Red_Flag", new Vector3(22.2f, 1.1f, 13.7f), palette["RedAccent"], rails);

            var collision = CreateGroup("Gameplay_Collision", arena);
            CreateCollisionBox("West_Boundary", new Vector3(-24.7f, 1.75f, 0f), new Vector3(0.6f, 3.5f, 34f), collision);
            CreateCollisionBox("East_Boundary", new Vector3(24.7f, 1.75f, 0f), new Vector3(0.6f, 3.5f, 34f), collision);
            CreateCollisionBox("North_Boundary", new Vector3(0f, 1.75f, 16.7f), new Vector3(50f, 3.5f, 0.6f), collision);
            CreateCollisionBox("South_Boundary", new Vector3(0f, 1.75f, -16.7f), new Vector3(50f, 3.5f, 0.6f), collision);

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

            AddSimpleBoxCollider(InstantiatePrefab(PlatformerPath("blue", "chest_large_blue"), "Blue_Supply_Chest", new Vector3(-14.5f, 0.08f, 5.5f), Quaternion.Euler(0f, 35f, 0f), Vector3.one, platforms), new Vector3(0f, 0.65f, 0f), new Vector3(1.5f, 1.3f, 1.2f));
            AddSimpleBoxCollider(InstantiatePrefab(PlatformerPath("red", "chest_large_red"), "Red_Supply_Chest", new Vector3(14.5f, 0.08f, -5.5f), Quaternion.Euler(0f, 215f, 0f), Vector3.one, platforms), new Vector3(0f, 0.65f, 0f), new Vector3(1.5f, 1.3f, 1.2f));
            AddSimpleBoxCollider(InstantiatePrefab(PlatformerPath("blue", "pipe_straight_A_blue"), "Blue_Pipe", new Vector3(-21f, 0.08f, -12.94f), Quaternion.identity, Vector3.one, platforms), new Vector3(0f, 0.9f, 0f), new Vector3(1.25f, 1.8f, 1.25f));
            AddSimpleBoxCollider(InstantiatePrefab(PlatformerPath("red", "pipe_straight_A_red"), "Red_Pipe", new Vector3(21f, 0.08f, 12.76f), Quaternion.identity, Vector3.one, platforms), new Vector3(0f, 0.9f, 0f), new Vector3(1.25f, 1.8f, 1.25f));
            InstantiatePrefab(PlatformerPath("yellow", "floor_spikes_trap_4x4x1_yellow"), "North_Spike_Trap", new Vector3(0f, 1.55f, 13.2f), Quaternion.identity, new Vector3(0.8f, 0.8f, 0.8f), platforms);
            InstantiatePrefab(PlatformerPath("yellow", "floor_spikes_trap_4x4x1_yellow"), "South_Spike_Trap", new Vector3(0f, 1.55f, -13.2f), Quaternion.identity, new Vector3(0.8f, 0.8f, 0.8f), platforms);
            InstantiatePrefab(PlatformerPath("blue", "flag_A_blue"), "Blue_Flag_Imported", new Vector3(-22.2f, 0.08f, 13.7f), Quaternion.identity, Vector3.one, platforms);
            InstantiatePrefab(PlatformerPath("red", "flag_A_red"), "Red_Flag_Imported", new Vector3(22.2f, 0.08f, 13.7f), Quaternion.identity, Vector3.one, platforms);
            InstantiatePrefab(PlatformerPath("blue", "spring_pad_blue"), "Blue_Jump_Pad", new Vector3(-17f, 0.08f, -12.5f), Quaternion.identity, Vector3.one, platforms);
            InstantiatePrefab(PlatformerPath("red", "spring_pad_red"), "Red_Jump_Pad", new Vector3(17f, 0.08f, 12.5f), Quaternion.identity, Vector3.one, platforms);

            AddSimpleBoxCollider(InstantiatePrefab(PlatformerPath("blue", "barrier_4x1x2_blue"), "Blue_Mid_Cover_North", new Vector3(-15f, 0.08f, 8f), Quaternion.Euler(0f, 90f, 0f), Vector3.one, platforms), new Vector3(0f, 0.7f, 0f), new Vector3(4f, 1.4f, 1f));
            AddSimpleBoxCollider(InstantiatePrefab(PlatformerPath("blue", "barrier_4x1x2_blue"), "Blue_Mid_Cover_South", new Vector3(-15f, 0.08f, -8f), Quaternion.Euler(0f, 90f, 0f), Vector3.one, platforms), new Vector3(0f, 0.7f, 0f), new Vector3(4f, 1.4f, 1f));
            AddSimpleBoxCollider(InstantiatePrefab(PlatformerPath("red", "barrier_4x1x2_red"), "Red_Mid_Cover_North", new Vector3(15f, 0.08f, 8f), Quaternion.Euler(0f, 90f, 0f), Vector3.one, platforms), new Vector3(0f, 0.7f, 0f), new Vector3(4f, 1.4f, 1f));
            AddSimpleBoxCollider(InstantiatePrefab(PlatformerPath("red", "barrier_4x1x2_red"), "Red_Mid_Cover_South", new Vector3(15f, 0.08f, -8f), Quaternion.Euler(0f, 90f, 0f), Vector3.one, platforms), new Vector3(0f, 0.7f, 0f), new Vector3(4f, 1.4f, 1f));
        }

        private static string PlatformerPath(string color, string assetName)
        {
            return $"{PlatformerPrefabRoot}/{color}/{assetName}.prefab";
        }

        private static void CreateTeamPlatform(string name, Vector3 position, Material accent, IReadOnlyDictionary<string, Material> palette, Transform parent)
        {
            var teamPlatform = CreateGroup(name, parent);
            teamPlatform.localPosition = position;
            CreateCylinder("Spawn_Pad", new Vector3(0f, 0.25f, 0f), new Vector3(6f, 0.25f, 6f), accent, teamPlatform, GeneratedColliderMode.Mesh);
            CreateCylinder("Spawn_Rim", new Vector3(0f, 0.52f, 0f), new Vector3(4.7f, 0.08f, 4.7f), palette["White"], teamPlatform, GeneratedColliderMode.None);
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
            hunter.localRotation = Quaternion.Euler(0f, yaw, 0f);
            CreateCylinder("Team_Pad", new Vector3(0f, 0.025f, 0f), new Vector3(1.05f, 0.025f, 1.05f), teamMaterial, hunter, GeneratedColliderMode.None);
            var visual = InstantiatePrefab($"{AdventurersPrefabRoot}/{role}.prefab", $"{role}_KayKit_Prefab", Vector3.zero, Quaternion.identity, Vector3.one, hunter);
            AttachHunterAccessories(role, visual.transform);
        }

        private static void AttachHunterAccessories(string role, Transform visual)
        {
            var accessoryRoot = "Assets/KayKit/Characters/KayKit - Adventurers (for Unity)/Prefabs/Accessories";
            switch (role)
            {
                case "Knight":
                    AttachAccessory(visual, "handslot.r", $"{accessoryRoot}/sword_1handed.prefab", "Sword");
                    AttachAccessory(visual, "handslot.l", $"{accessoryRoot}/shield_round.prefab", "Shield");
                    break;
                case "Ranger":
                    AttachAccessory(visual, "handslot.r", $"{accessoryRoot}/bow_withString.prefab", "Bow");
                    break;
                case "Mage":
                    AttachAccessory(visual, "handslot.r", $"{accessoryRoot}/staff.prefab", "Staff");
                    break;
                case "Barbarian":
                    AttachAccessory(visual, "handslot.r", $"{accessoryRoot}/axe_2handed.prefab", "Axe");
                    break;
            }
        }

        private static void AttachAccessory(Transform visual, string slotName, string prefabPath, string instanceName)
        {
            var slot = FindOptionalDescendant(visual, slotName);
            if (slot == null)
            {
                Debug.LogWarning($"[Action Hunters] KayKit hand slot {slotName} was not found below {visual.name}.");
                return;
            }

            InstantiatePrefab(prefabPath, instanceName, Vector3.zero, Quaternion.identity, Vector3.one, slot);
        }

        private static Transform FindOptionalDescendant(Transform root, string objectName)
        {
            var descendants = root.GetComponentsInChildren<Transform>(true);
            for (var index = 0; index < descendants.Length; index++)
            {
                if (descendants[index].name == objectName)
                {
                    return descendants[index];
                }
            }

            return null;
        }

        private static void CreateMonsterCamps(Transform root, IReadOnlyDictionary<string, Material> palette)
        {
            var camps = CreateGroup("AssetReferences_NOTFUN_MonstersPack04", root);
            CreateMonsterCamp("North_Catcher_Camp", "North_Catcher_Actor", "Catcher_Medium", new Vector3(-4.5f, 1.55f, 10.5f), 180f, palette["MonsterGreen"], palette, camps);
            CreateMonsterCamp("North_Imp_Camp", "North_Imp_Actor", "Imp_Medium", new Vector3(4.5f, 1.55f, 10.5f), 180f, palette["MonsterPurple"], palette, camps);
            CreateMonsterCamp("South_Treestor_Camp", "South_Treestor_Actor", "Treestor_Medium", new Vector3(-4.5f, 1.55f, -10.5f), 0f, palette["MonsterGreen"], palette, camps);
            CreateMonsterCamp("South_Spike_Camp", "South_Spike_Actor", "Spike_Medium", new Vector3(4.5f, 1.55f, -10.5f), 0f, palette["MonsterPurple"], palette, camps);
        }

        private static void CreateMonsterCamp(string name, string actorName, string prefabName, Vector3 position, float yaw, Material padMaterial, IReadOnlyDictionary<string, Material> palette, Transform parent)
        {
            var camp = CreateGroup(name, parent);
            camp.localPosition = position;
            CreateCylinder("Evolution_Pad", new Vector3(0f, 0.04f, 0f), new Vector3(1.35f, 0.04f, 1.35f), padMaterial, camp, GeneratedColliderMode.None);
            CreateCylinder("Evolution_Rim", new Vector3(0f, 0.09f, 0f), new Vector3(1.05f, 0.015f, 1.05f), palette["Danger"], camp, GeneratedColliderMode.None);

            var actor = CreateGroup(actorName, parent);
            actor.localPosition = position;
            actor.localRotation = Quaternion.Euler(0f, yaw, 0f);
            InstantiatePrefab($"{MonstersPrefabRoot}/{prefabName}.prefab", $"{prefabName}_NOTFUN_Prefab", Vector3.zero, Quaternion.identity, new Vector3(0.8f, 0.8f, 0.8f), actor);
        }

        private static void CreateElementalVfxAnchors(Transform root, IReadOnlyDictionary<string, Material> palette)
        {
            var anchors = CreateGroup("AssetReferences_PixPlays_ElementalSpells", root);
            CreateVfxAnchor("Water_Shield", "Assets/PixPlays/ElementalShields/WaterShield/Version_BuiltIn/WaterShield.prefab", new Vector3(-5f, 1.35f, 0f), palette["WaterVfx"], anchors, 7f);
            CreateVfxAnchor("Fire_Projectile", "Assets/PixPlays/ElementalProjectiles/Fireball/Version_BuiltIn/Fireball.prefab", new Vector3(5f, 1.35f, 0f), palette["FireVfx"], anchors, 7f);
            CreateVfxAnchor("Wind_Beam", "Assets/PixPlays/ElementalBeams/WindBeam/Version_BuiltIn/WindBeam.prefab", new Vector3(0f, 2.2f, 5f), palette["AirVfx"], anchors, 5f);
            CreateVfxAnchor("Earth_AOE", "Assets/PixPlays/ElementalAOE/EarthAOE/Version_BuiltIn/EarthSlamSpikesAoeVFX.prefab", new Vector3(0f, 2.2f, -5f), palette["EarthVfx"], anchors, 5f);
            anchors.gameObject.SetActive(false);
        }

        private static void CreateVfxAnchor(string name, string prefabPath, Vector3 position, Material material, Transform parent, float lightRange)
        {
            var anchor = CreateGroup(name, parent);
            anchor.localPosition = position;
            CreateCylinder("VFX_Pedestal", new Vector3(0f, -0.12f, 0f), new Vector3(0.75f, 0.08f, 0.75f), material, anchor, GeneratedColliderMode.None);
            InstantiatePrefab(prefabPath, name + "_PixPlays_Prefab", Vector3.zero, Quaternion.identity, Vector3.one, anchor);

            var pointLight = anchor.gameObject.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = material.color;
            pointLight.intensity = 2.2f;
            pointLight.range = lightRange;
            pointLight.shadows = LightShadows.None;
        }

        private static void CreateDemoVerticalSlice(Transform root, DemoHud hud)
        {
            var config = AssetDatabase.LoadAssetAtPath<DemoGameConfig>(DemoConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<DemoGameConfig>();
                config.ApplyDemoDefaults();
                AssetDatabase.CreateAsset(config, DemoConfigPath);
                EditorUtility.SetDirty(config);
            }

            var blueHunters = new List<DemoCombatant>
            {
                ConfigureCombatant(root, "Blue_Knight", DemoTeam.Blue, DemoRole.Guardian, 0, new Vector3(-18f, 0.52f, -2.7f)),
                ConfigureCombatant(root, "Blue_Ranger", DemoTeam.Blue, DemoRole.Ranger, 1, new Vector3(-18f, 0.52f, -0.9f)),
                ConfigureCombatant(root, "Blue_Mage", DemoTeam.Blue, DemoRole.Medic, 2, new Vector3(-18f, 0.52f, 0.9f)),
                ConfigureCombatant(root, "Blue_Barbarian", DemoTeam.Blue, DemoRole.Striker, 3, new Vector3(-18f, 0.52f, 2.7f))
            };
            var redHunters = new List<DemoCombatant>
            {
                ConfigureCombatant(root, "Red_Knight", DemoTeam.Red, DemoRole.Guardian, 0, new Vector3(18f, 0.52f, 2.7f)),
                ConfigureCombatant(root, "Red_Ranger", DemoTeam.Red, DemoRole.Ranger, 1, new Vector3(18f, 0.52f, 0.9f)),
                ConfigureCombatant(root, "Red_Mage", DemoTeam.Red, DemoRole.Medic, 2, new Vector3(18f, 0.52f, -0.9f)),
                ConfigureCombatant(root, "Red_Barbarian", DemoTeam.Red, DemoRole.Striker, 3, new Vector3(18f, 0.52f, -2.7f))
            };
            var monsters = new List<DemoCombatant>
            {
                ConfigureCombatant(root, "North_Catcher_Actor", DemoTeam.Neutral, DemoRole.Monster, 0, new Vector3(-4.5f, 1.55f, 10.5f)),
                ConfigureCombatant(root, "North_Imp_Actor", DemoTeam.Neutral, DemoRole.Monster, 1, new Vector3(4.5f, 1.55f, 10.5f)),
                ConfigureCombatant(root, "South_Treestor_Actor", DemoTeam.Neutral, DemoRole.Monster, 2, new Vector3(-4.5f, 1.55f, -10.5f)),
                ConfigureCombatant(root, "South_Spike_Actor", DemoTeam.Neutral, DemoRole.Boss, 3, new Vector3(0f, 0.92f, 0f))
            };

            var camera = Camera.main;
            if (camera == null)
            {
                throw new System.InvalidOperationException("Main Camera is required for the demo vertical slice.");
            }

            var cameraRig = camera.GetComponent<DemoCameraRig>();
            if (cameraRig == null)
            {
                cameraRig = camera.gameObject.AddComponent<DemoCameraRig>();
            }

            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            if (inputActions == null)
            {
                throw new System.IO.FileNotFoundException("The project Input System action asset is required for the demo.");
            }

            var demoRoot = new GameObject("Offline_Demo_VerticalSlice");
            demoRoot.transform.SetParent(root, false);
            var effectPool = demoRoot.AddComponent<DemoEffectPool>();
            effectPool.Configure(new[]
            {
                LoadRequiredPrefab(WindHitPath),
                LoadRequiredPrefab(FireHitPath),
                LoadRequiredPrefab(WaterHitPath)
            });
            var tutorial = demoRoot.AddComponent<DemoTutorialDirector>();
            tutorial.Configure(hud);
            var controller = demoRoot.AddComponent<DemoMatchController>();
            controller.Configure(config, inputActions, cameraRig, hud, tutorial, effectPool, blueHunters, redHunters, monsters);
        }

        private static DemoCombatant ConfigureCombatant(
            Transform root,
            string objectName,
            DemoTeam team,
            DemoRole role,
            int slot,
            Vector3 spawnPosition)
        {
            var combatantTransform = FindRequiredDescendant(root, objectName);
            combatantTransform.position = spawnPosition;
            var combatant = combatantTransform.GetComponent<DemoCombatant>();
            if (combatant == null)
            {
                combatant = combatantTransform.gameObject.AddComponent<DemoCombatant>();
            }

            ConfigureCharacterController(combatantTransform.gameObject, role);
            ConfigureCombatantPresentation(combatantTransform, role);
            combatant.Configure(team, role, slot, spawnPosition);
            return combatant;
        }

        private static void ConfigureCharacterController(GameObject target, DemoRole role)
        {
            var controller = target.GetComponent<CharacterController>();
            if (controller == null)
            {
                controller = target.AddComponent<CharacterController>();
            }

            var height = role == DemoRole.Boss ? 2.2f : role == DemoRole.Monster ? 1.7f : 1.65f;
            controller.height = height;
            controller.radius = role == DemoRole.Boss ? 0.8f : role == DemoRole.Monster ? 0.55f : 0.42f;
            controller.center = Vector3.up * (height * 0.5f);
            controller.slopeLimit = 50f;
            controller.stepOffset = 0.3f;
            controller.skinWidth = 0.05f;
            controller.minMoveDistance = 0f;
        }

        private static void ConfigureCombatantPresentation(Transform combatant, DemoRole role)
        {
            var animator = combatant.GetComponentInChildren<Animator>(true);
            if (animator != null && role is DemoRole.Guardian or DemoRole.Ranger or DemoRole.Medic or DemoRole.Striker)
            {
                animator.runtimeAnimatorController = GetOrCreateRoleAnimatorController(role);
                animator.applyRootMotion = false;
                animator.updateMode = AnimatorUpdateMode.Normal;
            }

            var presentation = combatant.GetComponent<DemoCombatantPresentation>();
            if (presentation == null)
            {
                presentation = combatant.gameObject.AddComponent<DemoCombatantPresentation>();
            }

            var visualRoot = animator != null ? animator.transform : FindCombatantVisual(combatant);
            var wind = LoadRequiredPrefab(WindHitPath);
            var fire = LoadRequiredPrefab(FireHitPath);
            var water = LoadRequiredPrefab(WaterHitPath);
            var attack = role is DemoRole.Ranger or DemoRole.Striker or DemoRole.Boss ? fire : wind;
            var skill = role == DemoRole.Medic ? water : fire;
            presentation.Configure(visualRoot, animator, attack, wind, skill, water);
        }

        private static Transform FindCombatantVisual(Transform combatant)
        {
            for (var index = 0; index < combatant.childCount; index++)
            {
                var child = combatant.GetChild(index);
                if (!child.name.Contains("Pad") && child.GetComponentInChildren<Renderer>(true) != null)
                {
                    return child;
                }
            }

            throw new System.InvalidOperationException($"A visual root was not found below combatant {combatant.name}.");
        }

        private static RuntimeAnimatorController GetOrCreateRoleAnimatorController(DemoRole role)
        {
            var path = $"{AnimationsPath}/{role}.controller";
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (existing != null)
            {
                return existing;
            }

            var root = "Assets/KayKit/Characters/Animations/Animations/Rig_Medium";
            var attackPath = role switch
            {
                DemoRole.Guardian => $"{root}/Combat Melee/Melee_1H_Attack_Slice_Horizontal.anim",
                DemoRole.Ranger => $"{root}/Combat Ranged/Ranged_Bow_Release.anim",
                DemoRole.Medic => $"{root}/Combat Ranged/Ranged_Magic_Shoot.anim",
                _ => $"{root}/Combat Melee/Melee_2H_Attack_Chop.anim"
            };
            var skillPath = role switch
            {
                DemoRole.Guardian => $"{root}/Combat Melee/Melee_Block_Attack.anim",
                DemoRole.Ranger => $"{root}/Combat Ranged/Ranged_Bow_Release_Up.anim",
                DemoRole.Medic => $"{root}/Combat Ranged/Ranged_Magic_Raise.anim",
                _ => $"{root}/Combat Melee/Melee_2H_Attack_Spin.anim"
            };

            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Skill", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);

            var stateMachine = controller.layers[0].stateMachine;
            var idle = stateMachine.AddState("Idle", new Vector3(240f, 60f));
            var run = stateMachine.AddState("Run", new Vector3(480f, 60f));
            var attack = stateMachine.AddState("Attack", new Vector3(480f, 150f));
            var skill = stateMachine.AddState("Skill", new Vector3(480f, 240f));
            var hit = stateMachine.AddState("Hit", new Vector3(240f, 240f));
            var death = stateMachine.AddState("Death", new Vector3(20f, 240f));
            idle.motion = LoadRequiredClip($"{root}/General/Idle_A.anim");
            run.motion = LoadRequiredClip($"{root}/Movement Basic/Running_A.anim");
            attack.motion = LoadRequiredClip(attackPath);
            skill.motion = LoadRequiredClip(skillPath);
            hit.motion = LoadRequiredClip($"{root}/General/Hit_A.anim");
            death.motion = LoadRequiredClip($"{root}/General/Death_A.anim");
            stateMachine.defaultState = idle;

            AddFloatTransition(idle, run, "Speed", AnimatorConditionMode.Greater, 0.08f);
            AddFloatTransition(run, idle, "Speed", AnimatorConditionMode.Less, 0.08f);
            AddTriggerTransition(stateMachine, attack, "Attack");
            AddTriggerTransition(stateMachine, skill, "Skill");
            AddTriggerTransition(stateMachine, hit, "Hit");
            AddExitTransition(attack, idle, 0.84f);
            AddExitTransition(skill, idle, 0.9f);
            AddExitTransition(hit, idle, 0.82f);

            var deathTransition = stateMachine.AddAnyStateTransition(death);
            deathTransition.hasExitTime = false;
            deathTransition.duration = 0.08f;
            deathTransition.canTransitionToSelf = false;
            deathTransition.AddCondition(AnimatorConditionMode.If, 0f, "Dead");
            var respawnTransition = death.AddTransition(idle);
            respawnTransition.hasExitTime = false;
            respawnTransition.duration = 0.12f;
            respawnTransition.AddCondition(AnimatorConditionMode.IfNot, 0f, "Dead");

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void AddFloatTransition(AnimatorState from, AnimatorState to, string parameter, AnimatorConditionMode mode, float threshold)
        {
            var transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0.12f;
            transition.AddCondition(mode, threshold, parameter);
        }

        private static void AddTriggerTransition(AnimatorStateMachine stateMachine, AnimatorState destination, string trigger)
        {
            var transition = stateMachine.AddAnyStateTransition(destination);
            transition.hasExitTime = false;
            transition.duration = 0.06f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        }

        private static void AddExitTransition(AnimatorState from, AnimatorState to, float exitTime)
        {
            var transition = from.AddTransition(to);
            transition.hasExitTime = true;
            transition.exitTime = exitTime;
            transition.duration = 0.1f;
        }

        private static AnimationClip LoadRequiredClip(string path)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                throw new System.IO.FileNotFoundException($"Required KayKit animation clip was not imported: {path}");
            }

            return clip;
        }

        private static GameObject LoadRequiredPrefab(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                throw new System.IO.FileNotFoundException($"Required demo prefab was not imported: {path}");
            }

            return prefab;
        }

        private static Transform FindRequiredDescendant(Transform root, string objectName)
        {
            var descendants = root.GetComponentsInChildren<Transform>(true);
            for (var index = 0; index < descendants.Length; index++)
            {
                if (descendants[index].name == objectName)
                {
                    return descendants[index];
                }
            }

            throw new System.InvalidOperationException($"Required generated object was not found: {objectName}");
        }

        private static DemoHud CreateGuiProInspiredHud(Transform root)
        {
            var hud = new GameObject("HUD_GUIPro_MinimalGameDark");
            hud.transform.SetParent(root, false);

            var canvas = hud.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            var scaler = hud.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1440f, 810f);
            scaler.matchWidthOrHeight = 0.5f;
            hud.AddComponent<GraphicRaycaster>();

            var resourceBar = LoadSprite($"{GuiSpriteRoot}/HUD/ResourceBar_Bg.png");
            var blueTitle = LoadSprite($"{GuiSpriteRoot}/Title/Title_02_NoDeco_Blue.png");
            var redTitle = LoadSprite($"{GuiSpriteRoot}/Title/Title_02_NoDeco_Red.png");
            var top = CreatePanel("Top_Round_Status", hud.transform, new Vector2(0.5f, 1f), new Vector2(900f, 116f), new Vector2(0f, -24f), Color.white, resourceBar);
            CreatePanel("Blue_Score_Backplate", top, new Vector2(0f, 0.5f), new Vector2(300f, 82f), new Vector2(172f, 0f), Color.white, blueTitle);
            CreatePanel("Red_Score_Backplate", top, new Vector2(1f, 0.5f), new Vector2(300f, 82f), new Vector2(-172f, 0f), Color.white, redTitle);
            var blueScore = CreateUiText("Blue_Score", top, "BLUE  0", 29, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(260f, 72f), new Vector2(44f, 0f));
            var redScore = CreateUiText("Red_Score", top, "0  RED", 29, TextAnchor.MiddleRight, new Vector2(1f, 0.5f), new Vector2(260f, 72f), new Vector2(-44f, 0f));
            var roundTimer = CreateUiText("Round_Timer", top, "05:00", 38, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(220f, 72f), Vector2.zero);
            var modeLabel = CreateUiText("Mode_Label", top, "OFFLINE VERTICAL SLICE", 18, TextAnchor.UpperCenter, new Vector2(0.5f, 0f), new Vector2(500f, 28f), new Vector2(0f, 8f), new Color(1f, 0.76f, 0.18f));

            var blueRoster = CreateRosterPanel("Blue_Hunter_Roster", hud.transform, new Vector2(0f, 0.5f), new Vector2(32f, 0f), new Color(0.035f, 0.17f, 0.36f, 0.9f));
            var redRoster = CreateRosterPanel("Red_Hunter_Roster", hud.transform, new Vector2(1f, 0.5f), new Vector2(-32f, 0f), new Color(0.36f, 0.045f, 0.075f, 0.9f));
            var blueRosterRows = CreateRosterRows(blueRoster, "BLUE HUNTERS", new[] { "GUARDIAN", "RANGER", "MEDIC", "STRIKER" }, TextAnchor.MiddleLeft);
            var redRosterRows = CreateRosterRows(redRoster, "RED HUNTERS", new[] { "GUARDIAN", "RANGER", "MEDIC", "STRIKER" }, TextAnchor.MiddleRight);

            var objectivePanel = CreatePanel("Objective_Card", hud.transform, new Vector2(0.5f, 0f), new Vector2(920f, 104f), new Vector2(0f, 24f), Color.white, resourceBar);
            var objective = CreateUiText("Objective", objectivePanel, "HUNT MONSTERS FOR GOLD  |  DEFEAT RED HUNTERS FOR SCORE", 19, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.67f), new Vector2(860f, 34f), Vector2.zero, new Color(1f, 0.82f, 0.3f));
            var help = CreateUiText("Help", objectivePanel, "WASD MOVE | LMB ATTACK | SPACE SKILL | 1-4 SWITCH | E HIRE | F1/H RULES | R RESTART", 17, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.24f), new Vector2(880f, 30f), Vector2.zero, new Color(0.75f, 0.82f, 0.92f));

            var tutorialCard = CreatePanel("Tutorial_Step_Card", hud.transform, new Vector2(0.5f, 1f), new Vector2(720f, 122f), new Vector2(0f, -150f), new Color(0.025f, 0.045f, 0.08f, 0.94f));
            var tutorialTitle = CreateUiText("Step_Title", tutorialCard, "1 / 6   MOVE", 22, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(530f, 34f), new Vector2(24f, -26f), new Color(1f, 0.82f, 0.25f));
            var tutorialProgress = CreateUiText("Step_Progress", tutorialCard, "PROGRESS  0 / 6", 18, TextAnchor.MiddleRight, new Vector2(1f, 1f), new Vector2(190f, 34f), new Vector2(-24f, -26f), new Color(0.64f, 0.76f, 0.92f));
            var tutorialInstruction = CreateUiText("Step_Instruction", tutorialCard, "Move 1.5m with WASD or the left stick.", 20, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(670f, 34f), new Vector2(24f, -1f));
            var tutorialProgressFill = CreateFilledBar("Tutorial_Progress", tutorialCard, new Vector2(0.5f, 0f), new Vector2(670f, 12f), new Vector2(0f, 14f), new Color(0.08f, 0.12f, 0.2f), new Color(1f, 0.68f, 0.1f));

            var toast = CreatePanel("Event_Toast", hud.transform, new Vector2(0.5f, 1f), new Vector2(610f, 56f), new Vector2(0f, -286f), new Color(0.025f, 0.04f, 0.07f, 0.94f));
            var toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
            toastGroup.alpha = 0f;
            var toastText = CreateUiText("Event_Text", toast, string.Empty, 21, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(570f, 46f), Vector2.zero, new Color(1f, 0.82f, 0.25f));

            var playerCard = CreatePanel("Controlled_Hunter_Card", hud.transform, Vector2.zero, new Vector2(360f, 132f), new Vector2(32f, 28f), new Color(0.025f, 0.08f, 0.16f, 0.94f));
            var playerRole = CreateUiText("Controlled_Role", playerCard, "CONTROL  |  GUARDIAN", 20, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(318f, 36f), new Vector2(20f, -25f), new Color(0.28f, 0.78f, 1f));
            var playerStats = CreateUiText("Controlled_Stats", playerCard, "HP 230 / 230     SKILL READY", 17, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(318f, 28f), new Vector2(20f, 4f), new Color(0.78f, 0.86f, 0.96f));
            var playerHealthFill = CreateFilledBar("Health", playerCard, new Vector2(0f, 0f), new Vector2(318f, 14f), new Vector2(20f, 38f), new Color(0.12f, 0.06f, 0.08f), new Color(0.18f, 0.9f, 0.48f));
            var playerSkillFill = CreateFilledBar("Skill", playerCard, new Vector2(0f, 0f), new Vector2(318f, 9f), new Vector2(20f, 18f), new Color(0.06f, 0.08f, 0.13f), new Color(0.2f, 0.65f, 1f));

            var rulesOverlay = CreatePanel("Beginner_Rules_Overlay", hud.transform, new Vector2(0.5f, 0.5f), new Vector2(1920f, 1080f), Vector2.zero, new Color(0.008f, 0.012f, 0.025f, 0.9f));
            var rulesCard = CreatePanel("Rules_Card", rulesOverlay, new Vector2(0.5f, 0.5f), new Vector2(860f, 520f), Vector2.zero, new Color(0.035f, 0.065f, 0.12f, 0.98f));
            CreateUiText("Rules_Title", rulesCard, "ACTION HUNTERS — BEGINNER GUIDE", 32, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(790f, 64f), new Vector2(0f, -48f), new Color(1f, 0.78f, 0.18f));
            var ruleBody = "WIN THE 5-MINUTE MATCH WITH MORE SCORE\n\n" +
                           "1. HUNT NEUTRAL MONSTERS  →  +30 GOLD\n" +
                           "2. RETURN TO BLUE BASE WITH 60 GOLD  →  PRESS E / A TO HIRE\n" +
                           "3. DEFEAT AN ENEMY HUNTER  →  +10 SCORE\n" +
                           "4. DEFEAT THE CENTER BOSS  →  +60 GOLD AND +5 SCORE\n\n" +
                           "TIED AT 00:00?  SUDDEN DEATH STARTS FOR 60 SECONDS.\n" +
                           "THE FIRST HUNTER KNOCKOUT WINS.";
            var rulesBody = CreateUiText("Rules_Body", rulesCard, ruleBody, 21, TextAnchor.MiddleLeft, new Vector2(0.5f, 0.5f), new Vector2(760f, 320f), new Vector2(0f, -10f), new Color(0.88f, 0.93f, 1f));
            rulesBody.lineSpacing = 1.18f;
            CreateUiText("Rules_Confirm", rulesCard, "CLICK / ENTER / SPACE / GAMEPAD A TO START     •     F1 / H / SELECT OPENS THIS GUIDE", 19, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(790f, 54f), new Vector2(0f, 34f), new Color(0.28f, 1f, 0.55f));

            var demoHud = hud.AddComponent<DemoHud>();
            demoHud.Configure(
                blueScore, redScore, roundTimer, modeLabel, objective, help, blueRosterRows, redRosterRows,
                playerRole, playerStats, playerHealthFill, playerSkillFill, toastGroup, toastText,
                tutorialCard.gameObject, tutorialTitle, tutorialInstruction, tutorialProgress, tutorialProgressFill,
                rulesOverlay.gameObject);
            return demoHud;
        }

        private static Transform CreateRosterPanel(string name, Transform parent, Vector2 anchor, Vector2 position, Color color)
        {
            var panel = CreatePanel(name, parent, anchor, new Vector2(280f, 330f), position, color);
            var rect = panel.GetComponent<RectTransform>();
            rect.pivot = new Vector2(anchor.x, 0.5f);
            return panel;
        }

        private static List<Text> CreateRosterRows(Transform parent, string title, IReadOnlyList<string> roles, TextAnchor alignment)
        {
            var rows = new List<Text>();
            CreateUiText("Title", parent, title, 20, alignment, new Vector2(0.5f, 1f), new Vector2(238f, 44f), new Vector2(0f, -30f), new Color(1f, 0.82f, 0.3f));
            for (var index = 0; index < roles.Count; index++)
            {
                var y = -82f - index * 57f;
                var row = CreatePanel($"Slot_{index + 1}_{roles[index]}", parent, new Vector2(0.5f, 1f), new Vector2(238f, 46f), new Vector2(0f, y), new Color(0.02f, 0.03f, 0.055f, 0.78f));
                rows.Add(CreateUiText("Role", row, $"{index + 1:00}   {roles[index]}", 19, alignment, new Vector2(0.5f, 0.5f), new Vector2(208f, 40f), Vector2.zero));
            }

            return rows;
        }

        private static Image CreateFilledBar(
            string name,
            Transform parent,
            Vector2 anchor,
            Vector2 size,
            Vector2 position,
            Color backgroundColor,
            Color fillColor)
        {
            var background = CreatePanel(name + "_Background", parent, anchor, size, position, backgroundColor);
            var fillObject = new GameObject(name + "_Fill");
            fillObject.transform.SetParent(background, false);
            var rect = fillObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(2f, 2f);
            rect.offsetMax = new Vector2(-2f, -2f);
            var image = fillObject.AddComponent<Image>();
            image.color = fillColor;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = 0;
            image.fillAmount = 1f;
            image.raycastTarget = false;
            return image;
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

        private static Text CreateUiText(string name, Transform parent, string value, int fontSize, TextAnchor alignment, Vector2 anchor, Vector2 size, Vector2 position, Color? color = null)
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
            return text;
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

        private static GameObject CreateCylinder(
            string name,
            Vector3 position,
            Vector3 scale,
            Material material,
            Transform parent,
            GeneratedColliderMode colliderMode = GeneratedColliderMode.Primitive,
            Quaternion? rotation = null)
        {
            var cylinder = CreatePrimitive(PrimitiveType.Cylinder, name, position, scale, material, parent, rotation);
            var primitiveCollider = cylinder.GetComponent<Collider>();
            if (colliderMode == GeneratedColliderMode.None)
            {
                Object.DestroyImmediate(primitiveCollider);
            }
            else if (colliderMode == GeneratedColliderMode.Mesh)
            {
                Object.DestroyImmediate(primitiveCollider);
                var meshCollider = cylinder.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = cylinder.GetComponent<MeshFilter>().sharedMesh;
            }

            return cylinder;
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

        private static void CreateCollisionBox(string name, Vector3 position, Vector3 size, Transform parent)
        {
            var collision = new GameObject(name);
            collision.transform.SetParent(parent, false);
            collision.transform.localPosition = position;
            var collider = collision.AddComponent<BoxCollider>();
            collider.size = size;
        }

        private static void AddSimpleBoxCollider(GameObject target, Vector3 center, Vector3 size)
        {
            var collider = target.AddComponent<BoxCollider>();
            collider.center = center;
            collider.size = size;
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
