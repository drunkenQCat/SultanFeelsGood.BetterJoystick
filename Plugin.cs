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


namespace SultanFeelsGood.BetterJoystick;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class ExamplePlugin : BasePlugin
{
    internal static new ManualLogSource Log;

    public override void Load()
    {
        // Plugin startup logic
        Log = base.Log;
        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        Log.LogInfo($"Plugin Info: {MyPluginInfo.PLUGIN_NAME}");
        Log.LogInfo($"Plugin Version: {MyPluginInfo.PLUGIN_VERSION}");
        this.AddComponent<ExampleComponent>();
    }

    [RegisterInIl2Cpp]
    public class ExampleComponent : MonoBehaviour
    {
        [HideFromIl2Cpp]
        public DragWindow TestWindow { get; }

        private float _timer = 0f;
        private bool _windowShown = false;
        private bool _shouldQuit = false;
        private bool _keybindingExported = false;
        GameController gameController = GameController.Inst;


        private ExampleComponent()
        {
            HandCardsController handCardsController = gameController.GetComponent<HandCardsController>();

        }

        public ExampleComponent(IntPtr ptr) : base(ptr)
        {
            TestWindow = new DragWindow(new Rect(0, 0, 0, 0), "🎮 手柄映射工具", () =>
            {
                // 简化的窗口内容，避免复杂的 GUILayout
                GUI.Label(new Rect(10, 10, 430, 30), "🚀 Sultan's Game 手柄映射助手");
                GUI.Label(new Rect(10, 50, 430, 20), "====================================");

                GUI.Label(new Rect(10, 80, 200, 20), $"⏱️ 运行时间: {Mathf.Floor(_timer)}秒");
                GUI.Label(new Rect(10, 100, 200, 20), $"🎯 窗口状态: {(TestWindow.Enabled ? "已显示" : "已隐藏")}");
                GUI.Label(new Rect(10, 120, 200, 20), $"📝 game controller是否有效: {gameController is not null}");
                GUI.Label(new Rect(10, 140, 200, 20), $"📝 模组最后日志: {ModLogger.instance.ErrorLogFileName}");

                if (GUI.Button(new Rect(10, 170, 130, 30), "🔄 刷新配置"))
                {
                    Log.LogInfo("配置已刷新");
                }

                if (GUI.Button(new Rect(150, 170, 130, 30), "📊 显示状态"))
                {
                    Log.LogInfo($"当前状态 - 时间: {_timer:F1}s, 日志: {ModLogger.instance.logs.Count}");
                }

                if (GUI.Button(new Rect(290, 170, 130, 30), "❌ 关闭窗口"))
                {
                    TestWindow.Enabled = false;
                }

                GUI.Label(new Rect(10, 210, 430, 60), "💡 使用提示:\n• 按 F2 键可手动切换窗口显示\n• 窗口将在 25 秒后自动关闭游戏\n• 可以拖拽窗口标题栏移动位置");
            })
            {
                Enabled = false,
            };
        }
        private void Start()
        {
            TestWindow.Rect = new Rect(Screen.width / 2 - 225, Screen.height / 2 - 140, 450, 280);
            Log.LogInfo("Start() called - Window position initialized");
        }

        private void Update()
        {
            // 更新计时器
            _timer += Time.deltaTime;

            // 20秒后显示窗口
            // if (_timer >= 20f && !_windowShown)
            // {
            //     // 居中窗口位置
            //     TestWindow.Enabled = true;
            //     _windowShown = true;
            //     Log.LogInfo("Window ENABLED after 20 seconds - TestWindow.Enabled = " + TestWindow.Enabled);
            // }

            // 23秒后退出游戏
            // if (_timer >= 25f && !_shouldQuit)
            // {
            //     _shouldQuit = true;
            //     Log.LogInfo("Quitting game after 25 seconds");
            //     Application.Quit();
            // }

            // 检测F2键按下
            var currentSceneName = SceneManager.GetActiveScene().name;
            var isInitOver = currentSceneName != "InitScene";

            if (Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame)
            {
                if (!isInitOver)
                {
                    Log.LogWarning("Init is not over! Don't press F2 again.");
                    return;
                }
                Log.LogInfo("F2 pushed - Toggling window from " + TestWindow.Enabled + " to " + !TestWindow.Enabled);
                TestWindow.Enabled = !TestWindow.Enabled;
                _windowShown = !_windowShown;
            }
            if (currentSceneName == "GameScene" && !_keybindingExported && !KeybindingManager.DefaultBindingsExists())
            {
                Log.LogInfo("Dump Start");
                var buttonObject = GameObject.Find("Sort");
                if (buttonObject == null)
                {
                    Log.LogError("Sort not found!");
                    return;
                    // 键位导出功能
                }
                ButtonActionBinder binder = buttonObject.GetComponent<ButtonActionBinder>();
                Log.LogInfo(binder.Action.asset);
                _keybindingExported = true;
                ButtonActionDumper dumper = new();
                dumper.Run();
            }
        }

        private void OnGUI()
        {
            TestWindow.OnGUI();
        }
    }
}
