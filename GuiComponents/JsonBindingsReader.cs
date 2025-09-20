using UnityEngine;
using UnityEngine.InputSystem;

namespace SultanFeelsGood.BetterJoystick.GuiComponents;

/// <summary>
/// 直接从Unity InputActionAsset读取和显示按键绑定信息的类
/// 不再依赖JSON解析，而是直接使用Unity的输入系统API
/// 主要功能是以表格形式展示键盘和手柄的按键映射
/// </summary>
public class InputActionBindingsReader
{
    /// <summary>
    /// Unity的InputActionAsset对象，包含所有输入动作和绑定信息
    /// 使用readonly确保只能在构造函数中初始化
    /// </summary>
    private readonly InputActionAsset _actionAsset;

    /// <summary>
    /// GUI滚动视图的当前位置
    /// 用于支持长列表的滚动显示
    /// </summary>
    private Vector2 _scrollPosition;

    // 常量定义，用于标识不同的输入设备组
    private const string KEYBOARD_GROUP = "Keyboard";   // 键盘设备组标识
    private const string GAMEPAD_GROUP = "Gamepad";      // 手柄设备组标识

    /// <summary>
    /// 构造函数，初始化输入动作绑定读取器
    /// </summary>
    /// <param name="actionAsset">Unity的InputActionAsset对象</param>
    public InputActionBindingsReader(InputActionAsset actionAsset)
    {
        // 检查InputActionAsset是否为空
        if (actionAsset == null)
        {
            Debug.LogError("InputActionAsset for bindings reader was null.");
            _actionAsset = ScriptableObject.CreateInstance<InputActionAsset>();
            return;
        }
        _actionAsset = actionAsset;
    }

    /// <summary>
    /// 绘制按键绑定显示界面
    /// 这是主要的GUI绘制方法，以表格形式展示所有按键绑定
    /// </summary>
    public void Draw()
    {
        try
        {
            // 检查数据是否有效，如果没有动作映射则显示错误信息
            if (_actionAsset == null || _actionAsset.actionMaps == null || _actionAsset.actionMaps.Count == 0)
            {
                GUILayout.Label("No action maps found in InputActionAsset.");
                return;
            }

            // --- 绘制表格头部 ---
            GUILayout.BeginHorizontal();
            GUILayout.Label("Action", GUILayout.Width(120));    // 动作名称列
            GUILayout.Label("Keyboard", GUILayout.ExpandWidth(true));  // 键盘按键列
            GUILayout.Label("Gamepad", GUILayout.ExpandWidth(true));   // 手柄按键列
            GUILayout.EndHorizontal();
            // 绘制分隔线
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2));

            // 开始滚动视图，支持长列表滚动
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            // 遍历所有动作映射
            foreach (var actionMap in _actionAsset.actionMaps)
            {
                // 显示当前动作映射的标题
                GUILayout.Label($"--- {actionMap.name} Map ---");

                // 遍历该映射中的所有动作
                foreach (var action in actionMap.actions)
                {
                    // 只处理按钮类型的动作，跳过其他类型
                    if (action.type != InputActionType.Button) continue;

                    // 开始水平布局，绘制单行按键绑定信息
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(action.name, GUILayout.Width(120));  // 显示动作名称

                    // 查找并显示该动作的键盘和手柄绑定
                    string keyboardBinding = FindBindingPathForAction(action, KEYBOARD_GROUP);
                    string gamepadBinding = FindBindingPathForAction(action, GAMEPAD_GROUP);

                    // 在方框中显示按键绑定信息
                    GUILayout.Box(keyboardBinding, GUILayout.ExpandWidth(true));
                    GUILayout.Box(gamepadBinding, GUILayout.ExpandWidth(true));

                    GUILayout.EndHorizontal();
                    GUILayout.Space(5);  // 行间距
                }
            }

            // 结束滚动视图
            GUILayout.EndScrollView();
        }
        catch (System.Exception ex)
        {
            // 在IL2CPP环境中，对象可能被垃圾回收，捕获异常并显示错误信息
            GUILayout.Label($"Error drawing bindings: {ex.Message}");
            Debug.LogError($"JsonBindingsReader Draw error: {ex}");
        }
    }

    /// <summary>
    /// 在指定动作中查找特定设备组的按键绑定路径
    /// </summary>
    /// <param name="action">要搜索的输入动作</param>
    /// <param name="group">设备组（键盘或手柄）</param>
    /// <returns>格式化后的按键路径，如果未找到则返回"None"</returns>
    private string FindBindingPathForAction(InputAction action, string group)
    {
        // 遍历该动作的所有绑定
        foreach (var binding in action.bindings)
        {
            // 检查绑定是否匹配指定的设备组，并且不是复合绑定
            if (!binding.isComposite && binding.groups.Contains(group))
            {
                // 格式化路径：移除设备前缀，只保留按键名称
                // 例如："<Keyboard>/space" → "space"
                return FormatBindingPath(binding.path, group);
            }
        }
        // 如果未找到匹配的绑定，返回"None"
        return "None";
    }

    /// <summary>
    /// 格式化按键绑定路径，移除设备前缀使显示更简洁
    /// </summary>
    /// <param name="path">原始绑定路径</param>
    /// <param name="group">设备组</param>
    /// <returns>格式化后的路径</returns>
    private string FormatBindingPath(string path, string group)
    {
        if (string.IsNullOrEmpty(path))
            return "None";

        // 根据设备组移除相应的前缀
        switch (group)
        {
            case KEYBOARD_GROUP:
                return path.Replace("<Keyboard>/", "");
            case GAMEPAD_GROUP:
                return path.Replace("<Gamepad>/", "");
            default:
                return path;
        }
    }
}

/// <summary>
/// 保持向后兼容性的JsonBindingsReader类
/// 现在内部使用新的InputActionBindingsReader实现
/// </summary>
public class JsonBindingsReader
{
    /// <summary>
    /// 内部的实际绑定读取器
    /// </summary>
    private readonly InputActionBindingsReader _internalReader;

    /// <summary>
    /// 构造函数，保持原有的JSON字符串参数接口
    /// 注意：此构造函数已不再使用JSON，而是需要传入InputActionAsset
    /// </summary>
    /// <param name="jsonContent">保留的参数名，实际应传入InputActionAsset的JSON字符串</param>
    public JsonBindingsReader(string jsonContent)
    {
        // 由于现在直接使用InputActionAsset，这个构造函数不再适用
        // 创建空的读取器
        _internalReader = new InputActionBindingsReader(ScriptableObject.CreateInstance<InputActionAsset>());
        Debug.LogWarning("JsonBindingsReader now uses InputActionAsset directly. Please use the new constructor with InputActionAsset parameter.");
    }

    /// <summary>
    /// 新的构造函数，直接接受InputActionAsset
    /// </summary>
    /// <param name="actionAsset">Unity的InputActionAsset对象</param>
    public JsonBindingsReader(InputActionAsset actionAsset)
    {
        _internalReader = new InputActionBindingsReader(actionAsset);
    }

    /// <summary>
    /// 绘制按键绑定显示界面
    /// 委托给内部的InputActionBindingsReader
    /// </summary>
    public void Draw()
    {
        _internalReader.Draw();
    }
}

