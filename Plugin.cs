using System;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using SultanFeelsGood.BetterJoystick.Attributes;
using SultanFeelsGood.BetterJoystick.Window;
using SultanFeelsGood.BetterJoystick.Keybinding;
using SultanFeelsGood.BetterJoystick.GuiComponents;

namespace SultanFeelsGood.BetterJoystick
{
    /// <summary>
    /// 主要的BepInEx插件类，作为插件的入口点
    /// 继承自BasePlugin，提供BepInEx插件的基本功能
    /// </summary>
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class ExamplePlugin : BasePlugin
    {
        /// <summary>
        /// 插件的日志源，用于输出调试信息和错误
        /// 使用internal static确保在整个插件中都可以访问
        /// </summary>
        internal static new ManualLogSource Log;

        /// <summary>
        /// 插件加载时调用此方法
        /// 这是BepInEx插件的主要入口点，在游戏启动时执行
        /// </summary>
        public override void Load()
        {
            // 初始化日志系统，继承自BasePlugin的Log属性
            Log = base.Log;
            // 输出插件加载成功的日志信息
            Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
            // 向游戏对象添加BetterJoystickPlugin组件，启动核心功能
            this.AddComponent<BetterJoystickPlugin>();
        }

        /// <summary>
        /// 核心功能组件，继承自MonoBehaviour以便使用Unity的生命周期方法
        /// 使用RegisterInIl2Cpp属性注册到IL2CPP运行时
        /// </summary>
        [RegisterInIl2Cpp]
        public class BetterJoystickPlugin : MonoBehaviour
        {
            /// <summary>
            /// 主测试窗口，用于显示手柄映射工具的UI
            /// 使用HideFromIl2Cpp属性避免被IL2CPP运行时干扰
            /// </summary>
            [HideFromIl2Cpp]
            public DragWindow TestWindow { get; }

            /// <summary>
            /// 按键绑定显示窗口，用于展示默认的按键配置
            /// 使用HideFromIl2Cpp属性避免被IL2CPP运行时干扰
            /// </summary>
            [HideFromIl2Cpp]
            public DragWindow BindingsDisplayWindow { get; }

            // 私有字段用于内部状态管理
            private float _timer = 0f;                    // 计时器，用于显示插件运行时间
            // private bool _windowShown = false;            // 窗口显示状态标志
            private bool _keybindingExported = false;     // 按键绑定导出状态标志，确保只执行一次
            private JsonBindingsReader _bindingsReader;   // JSON绑定读取器，用于解析按键配置文件
            private KeybindingCollector _keybindingCollector; // 按键绑定收集器，用于查找和管理按键绑定
            private InputActionAsset _inputActionAsset;   // 保持对InputActionAsset的强引用，防止被GC回收

            /// <summary>
            /// 构造函数，初始化插件组件
            /// IntPtr参数用于IL2CPP运行时的对象引用
            /// </summary>
            /// <param name="ptr">IL2CPP对象指针</param>
            public BetterJoystickPlugin(IntPtr ptr) : base(ptr)
            {
                // 初始化主测试窗口
                TestWindow = new DragWindow(new Rect(0, 0, 450, 280), "🎮 手柄映射工具", () =>
                {
                    // 窗口内容绘制逻辑
                    GUI.Label(new Rect(10, 20, 430, 30), "🚀 Sultan's Game 手柄映射助手");
                    GUI.Label(new Rect(10, 50, 430, 20), "====================================");
                    GUI.Label(new Rect(10, 80, 200, 20), $"⏱️ 运行时间: {Mathf.Floor(_timer)}秒");

                    // 显示默认绑定按钮
                    if (GUI.Button(new Rect(10, 140, 180, 30), "Show Default Bindings"))
                    {
                        // 检查绑定读取器是否已初始化
                        if (_bindingsReader == null)
                        {
                            Log.LogError("Bindings reader not initialized. Please wait for GameScene to load.");
                        }
                        else
                        {
                            // 切换绑定显示窗口的可见状态
                            BindingsDisplayWindow.Enabled = !BindingsDisplayWindow.Enabled;
                        }
                    }

                    // 关闭窗口按钮
                    if (GUI.Button(new Rect(290, 170, 130, 30), "❌ 关闭窗口"))
                    {
                        TestWindow.Enabled = false;
                    }
                })
                {
                    Enabled = false, // 初始状态下窗口不可见
                };

                // 初始化绑定显示窗口
                BindingsDisplayWindow = new DragWindow(new Rect(0, 0, 500, 600), "📜 默认按键绑定", () =>
                {
                    // 绘制绑定内容，如果读取器存在则调用其Draw方法
                    _bindingsReader?.Draw();
                    // 关闭按钮
                    if (GUILayout.Button("Close"))
                    {
                        BindingsDisplayWindow.Enabled = false;
                    }
                })
                {
                    Enabled = false // 初始状态下窗口不可见
                };
            }

            /// <summary>
            /// Unity Start生命周期方法，在对象创建后调用一次
            /// 用于初始化窗口位置和记录启动日志
            /// </summary>
            private void Start()
            {
                // 设置主窗口位置（屏幕中央偏左）
                TestWindow.Rect = new Rect(Screen.width / 2 - 225, Screen.height / 2 - 140, 450, 280);
                // 设置绑定显示窗口位置（屏幕中央偏右）
                BindingsDisplayWindow.Rect = new Rect(Screen.width / 2 + 235, Screen.height / 2 - 300, 500, 600);
                Log.LogInfo("Start() called - Windows initialized");
            }

            /// <summary>
            /// Unity Update生命周期方法，每帧调用一次
            /// 用于处理计时器更新、按键检测和场景相关的逻辑
            /// </summary>
            private void Update()
            {
                // 更新计时器
                _timer += Time.deltaTime;

                // 获取当前场景名称并检查初始化状态
                var currentSceneName = SceneManager.GetActiveScene().name;
                var isInitOver = currentSceneName != "InitScene";

                // 检测F2按键按下事件（用于显示/隐藏主窗口）
                if (Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame)
                {
                    // 如果初始化未完成，警告用户不要重复按键
                    if (!isInitOver)
                    {
                        Log.LogWarning("Init is not over! Don't press F2 again.");
                        return;
                    }
                    // 切换主窗口的显示状态
                    TestWindow.Enabled = !TestWindow.Enabled;
                }

                // 在游戏场景中执行一次性的按键重新绑定和绑定读取器初始化逻辑
                if (currentSceneName == "GameScene" && !_keybindingExported)
                {
                    Log.LogInfo("Executing one-time key rebind and bindings reader initialization logic...");

                    // 初始化按键绑定收集器
                    InitializeKeybindingCollector();

                    // 通过收集器查找名为"Sort"的ButtonActionBinder
                    var sortBinder = _keybindingCollector.FindSpecifiedActionRef("Sort");
                    if (sortBinder != null)
                    {
                        // 通过动作引用反向查找对应的ButtonActionBinder
                        // 使用同一个binder执行初始化和重新绑定
                        InitializeBindingsReader(sortBinder);

                        // 执行按键重新绑定
                        sortBinder.action.ChangeBinding(0).WithPath("<Keyboard>/k").WithInteractions("hold(duration=1.0)");
                        Log.LogInfo($"Rebound {sortBinder.name} to long press K.");
                    }
                    else
                    {
                        Log.LogWarning("Could not find action reference for 'Sort' button.");
                    }

                    // 设置标志位确保此逻辑只执行一次
                    _keybindingExported = true;
                }
            }

            /// <summary>
            /// Unity OnGUI生命周期方法，用于绘制GUI元素
            /// 在这里调用两个窗口的OnGUI方法进行渲染
            /// </summary>
            private void OnGUI()
            {
                // 渲染主测试窗口
                TestWindow.OnGUI();
                // 渲染绑定显示窗口
                BindingsDisplayWindow.OnGUI();
            }

            /// <summary>
            /// 初始化按键绑定收集器
            /// 收集场景中所有的ButtonActionBinder组件并提供查询功能
            /// </summary>
            private void InitializeKeybindingCollector()
            {
                try
                {
                    _keybindingCollector = new KeybindingCollector();
                    _keybindingCollector.Collect();
                    Log.LogInfo($"Successfully initialized keybinding collector with {_keybindingCollector.Binders.Count} binders found.");
                }
                catch (Exception e)
                {
                    Log.LogError($"Failed to initialize keybinding collector: {e.Message}");
                }
            }

            /// <summary>
            /// 初始化按键绑定读取器
            /// 从已获取的ButtonActionBinder获取InputActionAsset并创建绑定读取器
            /// </summary>
            /// <param name="binder">已获取的ButtonActionBinder组件</param>
            private void InitializeBindingsReader(InputActionReference binder)
            {
                try
                {
                    // 从ButtonActionBinder获取InputActionAsset
                    // 使用与GetBindingFromButton.cs相同的方法
                    _inputActionAsset = binder.asset;

                    if (_inputActionAsset == null)
                    {
                        Log.LogError("Could not retrieve InputActionAsset from ButtonActionBinder.");
                        return;
                    }

                    // 使用新的构造函数创建JsonBindingsReader
                    _bindingsReader = new JsonBindingsReader(_inputActionAsset);
                    Log.LogInfo("Successfully initialized bindings reader from InputActionAsset.");
                }
                catch (Exception e)
                {
                    Log.LogError($"Failed to initialize bindings reader: {e.Message}");
                }
            }
        }
    }
}
