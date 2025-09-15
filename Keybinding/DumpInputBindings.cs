using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SultanFeelsGood.BetterJoystick.Keybinding;

public static class DumpInputBindings
{
    public static void Run()
    {
        try
        {
            var foundAssets = new Dictionary<InputActionAsset, string>();

            // 1. 找 PlayerInput
            var playerInputs = UnityEngine.Object.FindObjectsOfType<UnityEngine.InputSystem.PlayerInput>(true);
            foreach (var pi in playerInputs)
            {
                if (pi.actions != null && !foundAssets.ContainsKey(pi.actions))
                    foundAssets.Add(pi.actions, $"PlayerInput on {pi.gameObject.name}");
            }

            // 2. 反射查找
            var allGameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var go in allGameObjects)
            {
                var comps = go.GetComponents<Component>();
                if (comps == null) continue;

                foreach (var comp in comps)
                {
                    if (comp == null) continue;
                    var t = comp.GetType();
                    foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        var val = field.GetValue(comp);
                        if (val == null) continue;

                        if (val is InputActionAsset asset1)
                        {
                            if (!foundAssets.ContainsKey(asset1))
                                foundAssets.Add(asset1, $"Field {t.FullName}.{field.Name} on {go.name}");
                        }
                        else if (val is ScriptableObject refObj && field.FieldType.Name.Contains("InputActionReference"))
                        {
                            var prop = refObj.GetType().GetProperty("action", BindingFlags.Public | BindingFlags.Instance);
                            if (prop != null)
                            {
                                if (prop.GetValue(refObj) is InputAction action && action.actionMap?.asset != null)
                                {
                                    var asset = action.actionMap.asset;
                                    if (!foundAssets.ContainsKey(asset))
                                        foundAssets.Add(asset, $"InputActionReference field {t.FullName}.{field.Name} on {go.name}");
                                }
                            }
                        }
                    }
                }
            }

            // 3. 转 JSON
            var sb = new StringBuilder();
            sb.Append("{\"items\":[");
            bool firstItem = true;
            foreach (var kv in foundAssets)
            {
                if (!firstItem) sb.Append(",");
                firstItem = false;

                var assetJson = AssetToJson(kv.Key);
                sb.Append("{");
                sb.AppendFormat("\"source\":\"{0}\",\"data\":{1}", Escape(kv.Value), assetJson);
                sb.Append("}");
            }
            sb.Append("]}");

            // 写文件
            var folder = Path.Combine(BepInEx.Paths.PluginPath, "DumpInputBindings");
            Directory.CreateDirectory(folder);
            var outfile = Path.Combine(folder, $"bindings-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            File.WriteAllText(outfile, sb.ToString());

            BepInEx.Logging.Logger.CreateLogSource("DumpInputBindings").LogInfo($"Dumped {foundAssets.Count} assets to {outfile}");
        }
        catch (Exception ex)
        {
            BepInEx.Logging.Logger.CreateLogSource("DumpInputBindings").LogError(ex);
        }
    }

    private static string AssetToJson(InputActionAsset asset)
    {
        var sb = new StringBuilder();
        sb.Append("{");
        sb.AppendFormat("\"name\":\"{0}\",", Escape(asset.name));
        sb.Append("\"maps\":[");
        bool firstMap = true;
        foreach (var map in asset.actionMaps)
        {
            if (!firstMap) sb.Append(",");
            firstMap = false;

            sb.Append("{");
            sb.AppendFormat("\"name\":\"{0}\",\"id\":\"{1}\",\"actions\":[", Escape(map.name), map.id);
            bool firstAction = true;
            foreach (var action in map.actions)
            {
                if (!firstAction) sb.Append(",");
                firstAction = false;

                sb.Append("{");
                sb.AppendFormat("\"name\":\"{0}\",\"type\":\"{1}\",\"expectedControlType\":\"{2}\",\"bindings\":[",
                    Escape(action.name), Escape(action.type.ToString()), Escape(action.expectedControlType));

                bool firstBinding = true;
                foreach (var b in action.bindings)
                {
                    if (!firstBinding) sb.Append(",");
                    firstBinding = false;
                    sb.Append("{");
                    sb.AppendFormat("\"path\":\"{0}\",\"interactions\":\"{1}\",\"processors\":\"{2}\",\"groups\":\"{3}\",\"action\":\"{4}\",\"isComposite\":{5},\"isPartOfComposite\":{6}",
                        Escape(b.path), Escape(b.interactions), Escape(b.processors), Escape(b.groups), Escape(b.action),
                        b.isComposite.ToString().ToLower(), b.isPartOfComposite.ToString().ToLower());
                    sb.Append("}");
                }

                sb.Append("]}");
            }
            sb.Append("]}");
        }
        sb.Append("]}");
        return sb.ToString();
    }

    private static string Escape(string s)
    {
        return s == null ? "" : s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
