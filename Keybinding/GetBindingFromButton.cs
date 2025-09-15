using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.IO;

namespace SultanFeelsGood.BetterJoystick.Keybinding;
public class ButtonActionDumper
{
    private InputActionAsset inputAsset;
    static ManualLogSource Logger = new("BindingDumper");
    public ButtonActionDumper()
    {
        BepInEx.Logging.Logger.Sources.Add(Logger);
    }

    public void Run()
    {
        Logger.LogInfo("Dump Start");
        // 步骤 1：找到按钮 GameScene/MainUI/Next Round
        var buttonObject = GameObject.Find("MainUI/Next Round");
        if (buttonObject == null)
        {
            Logger.LogError("Button at MainUI/Next Round not found!");
            return;
        }

        var button = buttonObject.GetComponent<Button>();
        if (button == null)
        {
            Logger.LogError("No Button component on GameScene/MainUI/Next Round!");
            return;
        }

        // 步骤 2：获取 ButtonActionBinder 组件
        var binder = buttonObject.GetComponent<ButtonActionBinder>();
        if (binder == null)
        {
            Logger.LogError("ButtonActionBinder component not found on button!");
            return;
        }

        // 步骤 3：从 ButtonActionBinder 获取 InputActionAsset
        inputAsset = GetInputActionAssetFromBinder(binder);
        if (inputAsset == null)
        {
            Logger.LogError("Could not retrieve InputActionAsset from ButtonActionBinder!");
            return;
        }

        // 步骤 4：修改绑定（可选）
        // ModifyBindings();

        // 步骤 5：保存到 JSON 文件
        SaveJsonToPath(inputAsset, "modified_bindings.json");
    }

    private InputActionAsset GetInputActionAssetFromBinder(ButtonActionBinder binder)
    {
        binder.Action.action.LoadBindingOverridesFromJson("");
        return binder.Action.asset;
    }

    private void ModifyBindings()
    {
        // 示例：修改 "NextRound" 动作的绑定（假设存在）
        var actionMap = inputAsset.FindActionMap("UI"); // 替换为实际 Action Map 名称，可能为 "UI" 或其他
        if (actionMap == null)
        {
            Logger.LogError("UI Action Map not found!");
            return;
        }

        var nextRoundAction = actionMap.FindAction("NextRound"); // 替换为实际 Action 名称
        if (nextRoundAction == null)
        {
            Logger.LogError("NextRound Action not found!");
            return;
        }

        // 清除旧绑定（可选）
        if (nextRoundAction.bindings.Count > 0)
        {
            nextRoundAction.ChangeBinding(0).Erase();
        }

        // 添加新绑定（例如将 NextRound 改为 <Keyboard>/n）
        nextRoundAction.AddBinding("<Keyboard>/n").WithGroup("Keyboard");
        nextRoundAction.ApplyBindingOverride("<Keyboard>/n");

        inputAsset.Enable();
        Logger.LogInfo("NextRound binding modified to <Keyboard>/n");
    }

    private void SaveJsonToPath(InputActionAsset asset, string filename)
    {
        if (asset == null) return;

        // 保存绑定覆盖
        string json = asset.ToJson(); // 只保存绑定更改
        // 或：string json = asset.ToJson(); // 保存完整资产

        // 保存到 BepInEx/plugins/ButtonActionBinderMod
        string modFolder = Path.Combine(Paths.PluginPath, "ButtonActionBinderMod");
        Directory.CreateDirectory(modFolder);

        string fullPath = Path.Combine(modFolder, filename);
        File.WriteAllText(fullPath, json, System.Text.Encoding.UTF8);
        Logger.LogInfo($"Bindings saved to: {fullPath}");
    }
}
