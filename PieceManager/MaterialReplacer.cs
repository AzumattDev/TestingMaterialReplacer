using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using TestingMaterialReplacer;
using UnityEngine;

namespace PieceManager
{
    [PublicAPI]
    public static class MaterialReplacer
    {
        static MaterialReplacer()
        {
            originalMaterials = new Dictionary<string, Material>();
            _objectToSwap = new Dictionary<GameObject, bool>();
            _objectsForShaderReplace = new Dictionary<GameObject, ShaderType>();
            Harmony harmony = new("org.bepinex.helpers.PieceManager");
            harmony.Patch(AccessTools.DeclaredMethod(typeof(ObjectDB), nameof(ObjectDB.Awake)),
                postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(MaterialReplacer),
                    nameof(GetAllMaterials))));
            harmony.Patch(AccessTools.DeclaredMethod(typeof(ObjectDB), nameof(ObjectDB.Awake)),
                postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(MaterialReplacer),
                    nameof(ReplaceAllMaterialsWithOriginal))));
        }

        public enum ShaderType
        {
            PieceShader,
            VegetationShader,
            RockShader,
            RugShader,
            GrassShader
        }

        private static Dictionary<GameObject, bool> _objectToSwap;
        internal static Dictionary<string, Material> originalMaterials;
        private static Dictionary<GameObject, ShaderType> _objectsForShaderReplace;

        public static void RegisterGameObjectForShaderSwap(GameObject go, ShaderType type = ShaderType.PieceShader)
        {
            _objectsForShaderReplace?.Add(go, type);
        }

        public static void RegisterGameObjectForMatSwap(GameObject go, bool isJotunnMock = false)
        {
            _objectToSwap.Add(go, isJotunnMock);
        }

        [HarmonyPriority(Priority.VeryHigh)]
        private static void GetAllMaterials()
        {
            var allmats = Resources.FindObjectsOfTypeAll<Material>();
            foreach (var item in allmats)
            {
                originalMaterials[item.name] = item;
            }
        }

        [HarmonyPriority(Priority.VeryHigh)]
        private static void ReplaceAllMaterialsWithOriginal()
        {
            if (originalMaterials.Count <= 0) GetAllMaterials();
            foreach (var renderer in _objectToSwap.SelectMany(gameObject =>
                         gameObject.Key.GetComponentsInChildren<Renderer>(true)))
            {
                _objectToSwap.TryGetValue(renderer.gameObject, out bool jotunnPrefabFlag);
                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    if (jotunnPrefabFlag)
                    {
                        if (!renderer.materials[i].name.StartsWith("JVLmock_")) continue;
                        var matName = renderer.material.name.Replace(" (Instance)", string.Empty)
                            .Replace("JVLmock_", "");

                        if (originalMaterials.ContainsKey(matName))
                        {
                            renderer.material = originalMaterials[matName];
                        }
                        else
                        {
                            Debug.LogWarning("No suitable material found to replace: " + matName);
                            // Skip over this material in future
                            originalMaterials[matName] = renderer.material;
                        }
                    }
                    else
                    {
                        if (!renderer.materials[i].name.Contains("_REPLACE_")) continue;
                        TestingMaterialReplacerPlugin.TestingMaterialReplacerLogger.LogWarning(
                            $"----BEFORE: {renderer.material.name}");
                        string matName = renderer.material.name.Replace(" (Instance)", string.Empty)
                            .Replace("_REPLACE_", string.Empty);
                        TestingMaterialReplacerPlugin.TestingMaterialReplacerLogger.LogWarning(
                            $"----AFTER: {matName}");

                        TestingMaterialReplacerPlugin.TestingMaterialReplacerLogger.LogWarning(
                            $"----BEFORE#2: {renderer.materials[i].name}");
                        string name = renderer.materials[i].name.Replace(" (Instance)", string.Empty)
                            .Replace("_REPLACE_", string.Empty);
                        TestingMaterialReplacerPlugin.TestingMaterialReplacerLogger.LogWarning(
                            $"----AFTER#2: {name}");
                        if (originalMaterials.ContainsKey(matName))
                        {
                            renderer.material = originalMaterials[matName];
                        }
                        else
                        {
                            Debug.LogWarning("No suitable material found to replace: " + matName);
                            // Skip over this material in future
                            originalMaterials[matName] = renderer.material;
                        }

                        if (originalMaterials.ContainsKey(name))
                        {
                            renderer.materials[i] = originalMaterials[name];
                        }
                        else
                        {
                            Debug.LogWarning("No suitable material found to replaceSECOND: " + name);
                            // Skip over this material in future
                            originalMaterials[name] = renderer.material;
                        }
                    }
                }
            }

            foreach (var renderer in _objectsForShaderReplace.SelectMany(gameObject =>
                         gameObject.Key.GetComponentsInChildren<Renderer>(true)))
            {
                _objectsForShaderReplace.TryGetValue(renderer.gameObject, out ShaderType shaderType);
                foreach (var t in renderer.materials)
                {
                    switch (shaderType)
                    {
                        case ShaderType.PieceShader:
                            t.shader = Shader.Find("Custom/Piece");
                            break;
                        case ShaderType.VegetationShader:
                            t.shader = Shader.Find("Custom/Vegetation");
                            break;
                        case ShaderType.RockShader:
                            t.shader = Shader.Find("Custom/StaticRock");
                            break;
                        case ShaderType.RugShader:
                            t.shader = Shader.Find("Custom/Rug");
                            break;
                        case ShaderType.GrassShader:
                            t.shader = Shader.Find("Custom/Grass");
                            break;
                        default:
                            t.shader = Shader.Find("ToonDeferredShading2017");
                            break;
                    }
                }
            }
        }
    }
}