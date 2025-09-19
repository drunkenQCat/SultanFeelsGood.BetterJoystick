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
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class ExamplePlugin : BasePlugin
    {
        internal static new ManualLogSource Log;

        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
            this.AddComponent<ExampleComponent>();
        }

        [RegisterInIl2Cpp]
        public class ExampleComponent : MonoBehaviour
        {
            [HideFromIl2Cpp]
            public DragWindow TestWindow { get; }
            [HideFromIl2Cpp]
            public DragWindow BindingsDisplayWindow { get; }

            private float _timer = 0f;
            private bool _windowShown = false;
            private bool _keybindingExported = false;
            private JsonBindingsReader _bindingsReader;

            public ExampleComponent(IntPtr ptr) : base(ptr)
            {
                TestWindow = new DragWindow(new Rect(0, 0, 450, 280), "🎮 手柄映射工具", () =>
                {
                    GUI.Label(new Rect(10, 20, 430, 30), "🚀 Sultan's Game 手柄映射助手");
                    GUI.Label(new Rect(10, 50, 430, 20), "====================================");
                    GUI.Label(new Rect(10, 80, 200, 20), $"⏱️ 运行时间: {Mathf.Floor(_timer)}秒");

                    if (GUI.Button(new Rect(10, 140, 180, 30), "Show Default Bindings"))
                    {
                        if (_bindingsReader == null)
                        {
                            try
                            {
                                var jsonContent = System.IO.File.ReadAllText("C:\\TechProjects\\About_MyRepos\\SultanFeelsGood.BetterJoystick\\modified_bindings.json");
                                _bindingsReader = new JsonBindingsReader(jsonContent);
                                Log.LogInfo("Successfully loaded and parsed bindings JSON.");
                            }
                            catch (Exception e)
                            {
                                Log.LogError($"Failed to read or parse bindings JSON: {e.Message}");
                            }
                        }
                        BindingsDisplayWindow.Enabled = !BindingsDisplayWindow.Enabled;
                    }

                    if (GUI.Button(new Rect(290, 170, 130, 30), "❌ 关闭窗口"))
                    {
                        TestWindow.Enabled = false;
                    }
                })
                {
                    Enabled = false,
                };

                BindingsDisplayWindow = new DragWindow(new Rect(0, 0, 500, 600), "📜 默认按键绑定", () =>
                {
                    _bindingsReader?.Draw();
                    if (GUILayout.Button("Close"))
                    {
                        BindingsDisplayWindow.Enabled = false;
                    }
                })
                {
                    Enabled = false
                };
            }

            private void Start()
            {
                TestWindow.Rect = new Rect(Screen.width / 2 - 225, Screen.height / 2 - 140, 450, 280);
                BindingsDisplayWindow.Rect = new Rect(Screen.width / 2 + 235, Screen.height / 2 - 300, 500, 600);
                Log.LogInfo("Start() called - Windows initialized");
            }

            private void Update()
            {
                _timer += Time.deltaTime;

                var currentSceneName = SceneManager.GetActiveScene().name;
                var isInitOver = currentSceneName != "InitScene";

                if (Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame)
                {
                    if (!isInitOver)
                    {
                        Log.LogWarning("Init is not over! Don't press F2 again.");
                        return;
                    }
                    TestWindow.Enabled = !TestWindow.Enabled;
                }

                if (currentSceneName == "GameScene" && !_keybindingExported)
                {
                    Log.LogInfo("Executing one-time key rebind logic...");
                    var buttonObject = GameObject.Find("Sort");
                    if (buttonObject != null)
                    {
                        var binder = buttonObject.GetComponent<ButtonActionBinder>();
                        if (binder != null)
                        {
                            InputActionReference currentAction = binder.Action;
                            currentAction.action.ChangeBinding(0).WithPath("<Keyboard>/k").WithInteractions("hold(duration=1.0)");
                            Log.LogInfo($"Rebound {currentAction.action.name} to long press K.");
                        }
                    }
                    _keybindingExported = true; // Ensure this runs only once
                }
            }

            private void OnGUI()
            {
                TestWindow.OnGUI();
                BindingsDisplayWindow.OnGUI();
            }
        }
    }
}