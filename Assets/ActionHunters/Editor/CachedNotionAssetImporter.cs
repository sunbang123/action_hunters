using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ActionHunters.Editor
{
    internal static class CachedNotionAssetImporter
    {
        private static readonly string[] KayKitUrpPackages =
        {
            "Assets/KayKit/URP/URP - KayKit - Adventurers Character Pack (for Unity).unitypackage",
            "Assets/KayKit/URP/URP - KayKit - Platformer Pack (for Unity).unitypackage"
        };

        private readonly struct PackSpec
        {
            public PackSpec(string displayName, string fileName, bool required = true)
            {
                DisplayName = displayName;
                FileName = fileName;
                Required = required;
            }

            public string DisplayName { get; }
            public string FileName { get; }
            public bool Required { get; }
        }

        private static readonly PackSpec[] Packs =
        {
            new PackSpec("KayKit Adventurers", "KayKit - Adventurers Character Pack for Unity.unitypackage"),
            new PackSpec("KayKit Platformer", "KayKit - Platformer Pack for Unity.unitypackage"),
            new PackSpec("Monsters Pack 04", "Monsters Pack 04.unitypackage"),
            new PackSpec("Elemental Spells Full Pack VFX", "Elemental Spells Full Pack VFX.unitypackage"),
            new PackSpec("GUI Pro Bundle1", "GUI Pro - Bundle1.unitypackage"),
            new PackSpec("GUI Pro Minimal Game Dark", "GUI Pro - Minimal Game Dark.unitypackage", false)
        };

        [MenuItem("Action Hunters/Import Cached Notion Asset Packs")]
        private static void ImportCachedNotionAssetPacks()
        {
            var cacheRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Unity",
                "Asset Store-5.x");

            if (!Directory.Exists(cacheRoot))
            {
                Debug.LogError($"[Action Hunters] Unity Asset Store cache was not found at: {cacheRoot}");
                return;
            }

            var cachedPackages = Directory
                .EnumerateFiles(cacheRoot, "*.unitypackage", SearchOption.AllDirectories)
                .ToDictionary(Path.GetFileName, StringComparer.OrdinalIgnoreCase);

            var missingRequired = new List<string>();
            foreach (var pack in Packs)
            {
                if (pack.Required && !cachedPackages.ContainsKey(pack.FileName))
                {
                    missingRequired.Add(pack.DisplayName);
                }
            }

            if (missingRequired.Count > 0)
            {
                Debug.LogError($"[Action Hunters] Required cached packages are missing: {string.Join(", ", missingRequired)}");
                return;
            }

            try
            {
                for (var index = 0; index < Packs.Length; index++)
                {
                    var pack = Packs[index];
                    if (!cachedPackages.TryGetValue(pack.FileName, out var packagePath))
                    {
                        Debug.LogWarning($"[Action Hunters] Optional cached package was not found: {pack.DisplayName}");
                        continue;
                    }

                    EditorUtility.DisplayProgressBar(
                        "Action Hunters Asset Import",
                        $"Importing {pack.DisplayName}",
                        index / (float)Packs.Length);
                    AssetDatabase.ImportPackage(packagePath, false);
                    Debug.Log($"[Action Hunters] Imported cached package: {pack.DisplayName}");
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            Debug.Log("[Action Hunters] Cached Notion asset import completed. Review the Console before rebuilding Main.");
        }

        [MenuItem("Action Hunters/Apply KayKit URP Materials")]
        private static void ApplyKayKitUrpMaterials()
        {
            try
            {
                for (var index = 0; index < KayKitUrpPackages.Length; index++)
                {
                    var packagePath = KayKitUrpPackages[index];
                    var absolutePath = Path.GetFullPath(packagePath);
                    if (!File.Exists(absolutePath))
                    {
                        Debug.LogError($"[Action Hunters] KayKit URP conversion package was not found: {packagePath}");
                        return;
                    }

                    EditorUtility.DisplayProgressBar(
                        "Action Hunters URP Conversion",
                        $"Importing {Path.GetFileNameWithoutExtension(packagePath)}",
                        index / (float)KayKitUrpPackages.Length);
                    AssetDatabase.ImportPackage(absolutePath, false);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            Debug.Log("[Action Hunters] Applied the KayKit-provided URP material conversion packages.");
        }
    }
}
