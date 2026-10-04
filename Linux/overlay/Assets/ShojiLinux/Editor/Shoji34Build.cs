using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.Rendering;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using UnityEngine.TextCore.LowLevel;

public static class Shoji34Build
{
    [Serializable] class Bundle { public string name; public string[] assets; }
    [Serializable] class Inputs { public Bundle[] bundles; }
    static string Root => Directory.GetParent(Application.dataPath).Parent.FullName;
    static string BundleName(string id) => Path.GetFileName(id.Replace('\\', '/'));
    static object Call(object owner, string name, params object[] args) =>
        (owner is Type type ? type : owner.GetType()).GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic |
            (owner is Type ? BindingFlags.Static : BindingFlags.Instance)).Invoke(owner is Type ? null : owner, args);

    public static void BuildContent()
    {
        var inputs = JsonUtility.FromJson<Inputs>(File.ReadAllText(Path.Combine(Root, "inspection/linux-bundle-inputs.json")));
        var output = "Assets/StreamingAssets/aa/StandaloneLinux64";
        Directory.CreateDirectory(output);
        var builds = inputs.bundles.Select(b => new AssetBundleBuild
        { assetBundleName = b.name, assetNames = b.assets, addressableNames = b.assets }).ToArray();
        var manifest = BuildPipeline.BuildAssetBundles(output, builds, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneLinux64);
        if (manifest == null) throw new Exception("Linux localization bundle build failed");
        var originalPath = Path.Combine(Root, "steam-source/MateEngineX_Data/StreamingAssets/aa");
        ContentCatalogData.ExtractBinaryCatalog(Path.Combine(originalPath, "catalog.bin"), Path.Combine(Root, "inspection/original-catalog.txt"));
        var original = (ContentCatalogData)Call(typeof(ContentCatalogData), "LoadFromFile", Path.Combine(originalPath, "catalog.bin"), false);
        var locator = (IResourceLocator)Call(original, "CreateCustomLocator", "", null);
        var locations = new Dictionary<string, (IResourceLocation location, HashSet<object> keys)>();
        foreach (var key in locator.Keys)
        {
            if (!locator.Locate(key, typeof(object), out var found)) continue;
            foreach (var location in found)
            {
                if (!locations.TryGetValue(location.PrimaryKey, out var item))
                    locations.Add(location.PrimaryKey, item = (location, new HashSet<object>()));
                item.keys.Add(key);
            }
        }
        var names = new HashSet<string>(inputs.bundles.Select(b => b.name));
        var bundleKeys = locations.Values.Where(v => v.location.Data is AssetBundleRequestOptions && names.Contains(BundleName(v.location.InternalId)))
            .ToDictionary(v => BundleName(v.location.InternalId), v => v.location.PrimaryKey);
        if (bundleKeys.Count != names.Count) throw new Exception("Original catalog does not cover all restored bundles: " + bundleKeys.Count + "/" + names.Count);
        var removed = new HashSet<string>(locations.Values.Where(v => v.location.Data is AssetBundleRequestOptions && !names.Contains(BundleName(v.location.InternalId)))
            .Select(v => v.location.PrimaryKey));
        var entries = new List<ContentCatalogDataEntry>();
        foreach (var item in locations.Values)
        {
            var location = item.location;
            if (removed.Contains(location.PrimaryKey)) continue;
            string internalId = location.InternalId.Replace('\\', '/');
            var dependencies = location.Dependencies.Where(d => !removed.Contains(d.PrimaryKey)).Select(d => (object)d.PrimaryKey).ToList();
            if (location.Data is AssetBundleRequestOptions options)
            {
                var name = Path.GetFileName(internalId);
                if (!BuildPipeline.GetCRCForAssetBundle(Path.Combine(output, name), out var crc)) throw new Exception("Cannot obtain bundle CRC: " + name);
                options.Crc = crc;
                options.Hash = manifest.GetAssetBundleHash(name).ToString();
                options.BundleName = name;
                options.BundleSize = new FileInfo(Path.Combine(output, name)).Length;
                internalId = "{UnityEngine.AddressableAssets.Addressables.RuntimePath}/StandaloneLinux64/" + name;
                dependencies = manifest.GetAllDependencies(name).Select(d => (object)bundleKeys[d]).ToList();
            }
            entries.Add(new ContentCatalogDataEntry(location.ResourceType, internalId, location.ProviderId, item.keys, dependencies, location.Data));
        }
        var catalog = new ContentCatalogData(entries, original.ProviderId)
        {
            BuildResultHash = Hash128.Compute(string.Join(";", names.Select(n => manifest.GetAssetBundleHash(n).ToString()))).ToString(),
            InstanceProviderData = ObjectInitializationData.CreateSerializedInitializationData<InstanceProvider>(),
            ResourceProviderData = new List<ObjectInitializationData>
            {
                ObjectInitializationData.CreateSerializedInitializationData<AssetBundleProvider>(),
                ObjectInitializationData.CreateSerializedInitializationData<BundledAssetProvider>()
            },
            SceneProviderData = ObjectInitializationData.CreateSerializedInitializationData<SceneProvider>()
        };
        var catalogPath = "Assets/StreamingAssets/aa/catalog.bin";
        Call(catalog, "SaveToFile", catalogPath);
        File.WriteAllText("Assets/StreamingAssets/aa/catalog.hash", Hash128.Compute(File.ReadAllBytes(catalogPath)).ToString());
        File.WriteAllText("Assets/StreamingAssets/aa/settings.json", File.ReadAllText(Path.Combine(originalPath, "settings.json"))
            .Replace("StandaloneWindows64", "StandaloneLinux64"));
        ContentCatalogData.ExtractBinaryCatalog(catalogPath, Path.Combine(Root, "inspection/linux-catalog.txt"));
        AssetDatabase.Refresh();
        Debug.Log($"Shoji34 localization: {builds.Length} Linux bundles, {entries.Count} catalog entries; original keys retained");
    }

    public static void BuildAll()
    {
        RestoreHumanoidAvatars();
        RestoreDynamicFont();
        PrepareSceneInputAndTransparency();
        BuildContent();
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneLinux64, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneLinux64, new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLCore });
        var playerPath = Environment.GetEnvironmentVariable("MATEENGINE_BUILD_OUTPUT")
            ?? Path.Combine(Root, "builds/linux-3.4/MateEngineX.x86_64");
        var report = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            playerPath, BuildTarget.StandaloneLinux64, BuildOptions.None);
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Linux player build failed: " + report.summary.result);
        Debug.Log($"Shoji34 player build succeeded: {report.summary.totalSize} bytes");
    }

    public static void RestoreDynamicFont()
    {
        const string path = "Assets/ShojiLinux/RebuiltFonts/LiberationSansDynamic.asset";
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (font == null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Font/LiberationSans.ttf");
            font = TMP_FontAsset.CreateFontAsset(source, 86, 9, GlyphRenderMode.SDFAA,
                1024, 1024, AtlasPopulationMode.Dynamic, true);
            font.name = "LiberationSans Dynamic";
            var serialized = new SerializedObject(font);
            serialized.FindProperty("m_ClearDynamicDataOnBuild").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            const string russian = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя";
            if (!font.TryAddCharacters(russian, out string missing))
                throw new Exception("Cannot restore Cyrillic glyphs: " + missing);
            AssetDatabase.CreateAsset(font, path);
            font.material.name = font.name + " Material";
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (var texture in font.atlasTextures)
            {
                texture.name = font.name + " Atlas";
                AssetDatabase.AddObjectToAsset(texture, font);
            }
        }
        var primary = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Resources/fonts & materials/LiberationSans SDF.asset");
        var old = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Resources/fonts & materials/LiberationSans SDF - Fallback.asset");
        for (int i = 0; i < primary.fallbackFontAssetTable.Count; i++)
            if (primary.fallbackFontAssetTable[i] == old) primary.fallbackFontAssetTable[i] = font;
        EditorUtility.SetDirty(primary);
        AssetDatabase.SaveAssets();
        Debug.Log($"Shoji34 font: {font.name}, {font.characterTable.Count} glyphs, {font.atlasTexture.format}");
    }

    public static void RestoreHumanoidAvatars()
    {
        const string output = "Assets/ShojiLinux/RebuiltAvatars";
        Directory.CreateDirectory(output);
        foreach (var sceneEntry in EditorBuildSettings.scenes.Where(s => s.enabled))
        {
            var scene = EditorSceneManager.OpenScene(sceneEntry.path);
            var animators = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Animator>(true)).ToArray();
            RebuildHumanoids(animators, output);
            EditorSceneManager.SaveScene(scene);
        }
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefab.GetComponentsInChildren<Animator>(true).Any(a => a.avatar != null && a.avatar.isHuman &&
                !AssetDatabase.GetAssetPath(a.avatar).StartsWith(output))) continue;
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (RebuildHumanoids(root.GetComponentsInChildren<Animator>(true), output))
                    PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
    }

    static bool RebuildHumanoids(Animator[] animators, string output)
    {
        bool changed = false;
        foreach (var group in animators.Where(a => a.avatar != null && a.avatar.isHuman &&
            !AssetDatabase.GetAssetPath(a.avatar).StartsWith(output)).GroupBy(a => a.avatar))
        {
            var original = group.Key;
            var description = original.humanDescription;
            var hips = description.human.First(b => b.humanName == "Hips").boneName;
            // CustomVRM is an empty inactive loader target, sharing the default Avatar.
            // Build against the real rig, then also update that shared reference.
            var animator = group.FirstOrDefault(a => a.GetComponentsInChildren<Transform>(true).Any(t => t.name == hips));
            if (animator == null) throw new Exception("No skeleton for humanoid: " + original.name);
            // Scene models and catalog prefabs both need native runtime tables.
            // Extracted tables imported successfully but crashed GetBoneTransform.
            var rebuilt = AvatarBuilder.BuildHumanAvatar(animator.gameObject, description);
            if (!rebuilt.isValid || !rebuilt.isHuman) throw new Exception("Cannot rebuild humanoid: " + animator.name);
            rebuilt.name = original.name + "Linux";
            var path = AssetDatabase.GenerateUniqueAssetPath(output + "/" + rebuilt.name + ".asset");
            AssetDatabase.CreateAsset(rebuilt, path);
            foreach (var target in group)
            {
                target.avatar = rebuilt;
                EditorUtility.SetDirty(target);
            }
            changed = true;
            Debug.Log("Shoji34 rebuilt humanoid: " + animator.name + " -> " + path);
        }
        return changed;
    }

    public static void PrepareSceneInputAndTransparency()
    {
        foreach (var entry in EditorBuildSettings.scenes.Where(s => s.enabled))
        {
            var scene = EditorSceneManager.OpenScene(entry.path);
            var roots = scene.GetRootGameObjects();
            foreach (var camera in roots.SelectMany(root => root.GetComponentsInChildren<Camera>(true)))
            {
                // Wayland composites premultiplied ARGB: zero alpha must clear RGB too.
                if (camera.backgroundColor.a == 0) camera.backgroundColor = Color.clear;
            }
            foreach (var handler in roots.SelectMany(root => root.GetComponentsInChildren<AvatarWindowHandler>(true)))
            {
                // X3.4 retains hide animations/code but omits the handler from
                // both the default model and the VRM component template.
                if (handler.GetComponent<AvatarHideHandler>() == null)
                    handler.gameObject.AddComponent<AvatarHideHandler>();
                // Keep X3.4's poses; use the installed Shoji X3.2 acquisition settings.
                handler.minDragHoldSecondsToSit = 1f;
                handler.unsnapCooldownSeconds = 0.3f;
                handler.probeZoneYOffsetLocal = 0f;
                handler.probeRadiusPx = 24f;
                handler.useGuardZone = false;
                handler.probeGuardPx = 240f;
                handler.blockSitIfBoolTrue.Clear();
                handler.minDragPixelsToSnap = 4;
                handler.snapSmoothingTime = 0.12f;
                handler.snapSmoothingMaxSpeed = 6000f;
                EditorUtility.SetDirty(handler);
                Debug.Log($"Shoji34 seating: {handler.name}, pelvis probe, radius=24, guard=false, poses={handler.totalWindowSitAnimations}");
            }
            foreach (var walking in roots.SelectMany(root => root.GetComponentsInChildren<AvatarLocomotionController>(true)))
            {
                walking.BoundsInsetLeft = 0;
                walking.BoundsInsetRight = 0;
                EditorUtility.SetDirty(walking);
            }
            foreach (var system in roots.SelectMany(root => root.GetComponentsInChildren<EventSystem>(true)))
                Debug.Log($"Shoji34 input: {system.name}, active={system.gameObject.activeInHierarchy}, " +
                    $"enabled={system.enabled}, modules=" + string.Join(",", system.GetComponents<BaseInputModule>().Select(m => m.GetType().Name + ":" + m.enabled)));
            foreach (var canvas in roots.SelectMany(root => root.GetComponentsInChildren<Canvas>(true)))
                Debug.Log($"Shoji34 canvas: {canvas.name}, active={canvas.gameObject.activeInHierarchy}, " +
                    $"mode={canvas.renderMode}, raycaster={canvas.GetComponent<GraphicRaycaster>() != null}");
            Debug.Log("Shoji34 buttons: " + roots.SelectMany(root => root.GetComponentsInChildren<Button>(true)).Count());
            EditorSceneManager.SaveScene(scene);
        }
    }
}
