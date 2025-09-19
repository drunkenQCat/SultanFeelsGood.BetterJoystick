using UnityEngine;
using UnityEngine.InputSystem;
using BepInEx;
using BepInEx.Logging;
using System.Collections.Generic;
using System.Linq;

namespace SultanFeelsGood.BetterJoystick.Keybinding
{
    class KeybindingCollector
    {
        private List<ButtonActionBinder> _binders = new List<ButtonActionBinder>();

        public IReadOnlyList<ButtonActionBinder> Binders => _binders;

        static ManualLogSource Logger = new("BindingDumper");

        public KeybindingCollector()
        {
            BepInEx.Logging.Logger.Sources.Add(Logger);
        }

        public void Collect()
        {
            // 获取所有 ButtonActionBinder 组件并存储
            _binders = Object.FindObjectsOfType<ButtonActionBinder>().ToList();

            // 遍历并输出信息
            foreach (var binder in _binders)
            {
                Logger.LogInfo($"Found ButtonActionBinder on: {binder.gameObject.name}");

                // 获取关联的按钮组件
                var button = binder.Button;
                if (button != null)
                {
                    Logger.LogInfo($"  - Associated with button: {button.gameObject.name}");
                }

                // 获取输入动作引用
                var actionRef = binder.Action;
                if (actionRef != null)
                {
                    Logger.LogInfo($"  - Bound to action: {actionRef.name}");
                }
            }
            Logger.LogInfo($"Total ButtonActionBinder components found: {_binders.Count}");
        }

        public InputActionReference FindSpecifiedActionRef(string name)
        {
            return _binders
                .FirstOrDefault(binder => binder.Button != null && binder.Button.name == name)
                ?.Action;
        }
    }
}
