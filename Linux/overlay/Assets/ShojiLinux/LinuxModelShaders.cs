using System;
using System.Collections.Generic;
using UnityEngine;

// Strong references distinguish player shaders from identically named shaders
// loaded later from Windows .me bundles. Shader.Find cannot make that distinction.
public sealed class LinuxModelShaders : ScriptableObject
{
    public Shader[] shaders;
    static Dictionary<string, Shader> replacements;

    public static void Restore(GameObject model, AssetBundle bundle)
    {
        if (Application.platform != RuntimePlatform.LinuxPlayer) return;
        if (replacements == null)
        {
            var library = Resources.Load<LinuxModelShaders>("LinuxModelShaders");
            if (library == null)
            {
                Debug.LogError("[LinuxModelShaders] Player shader library is missing");
                return;
            }
            replacements = new Dictionary<string, Shader>(StringComparer.Ordinal);
            foreach (var shader in library.shaders)
                if (shader != null) replacements.Add(shader.name, shader);
        }

        var materials = new HashSet<Material>(bundle.LoadAllAssets<Material>());
        foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            foreach (var material in renderer.sharedMaterials)
                if (material != null) materials.Add(material);

        int restored = 0;
        var missing = new HashSet<string>();
        foreach (var material in materials)
        {
            if (material == null || material.shader == null) continue;
            string name = material.shader.name;
            if (!replacements.TryGetValue(name, out var shader))
            {
                missing.Add(name);
                continue;
            }
            if (material.shader == shader) continue;
            if (!shader.isSupported)
            {
                missing.Add(name);
                continue;
            }
            // Same-name shaders retain the material's serialized properties.
            // Shader assignment resets the queue and may alter keyword state.
            // An unsupported bundle shader can report the fallback's effective
            // queue (2000). Preserve -1 so the replacement uses its own queue.
            int queue = material.rawRenderQueue;
            string[] keywords = material.shaderKeywords;
            material.shader = shader;
            material.shaderKeywords = keywords;
            material.renderQueue = queue;
            if (Environment.GetEnvironmentVariable("MATEENGINE_SHADER_AUDIT") == "1")
                Debug.Log($"[LinuxModelShaders] {material.name}: {name} " +
                    $"queueOverride={queue} resolvedQueue={material.renderQueue}");
            restored++;
        }
        Debug.Log($"[LinuxModelShaders] Restored {restored} materials with player-compiled shaders");
        foreach (var name in missing)
            Debug.LogWarning("[LinuxModelShaders] No supported exact shader replacement: " + name);
    }
}
