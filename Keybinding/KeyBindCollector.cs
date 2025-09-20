using UnityEngine;
using UnityEngine.InputSystem;
using BepInEx.Logging;
using System.Collections.Generic;
using System.Linq;
using System;

namespace SultanFeelsGood.BetterJoystick.Keybinding
{
    /// <summary>
    /// 按键绑定收集器类，用于收集和管理游戏中的按键绑定信息
    /// 主要功能是扫描场景中的所有ButtonActionBinder和ToggleActionBinder组件并提供查询接口
    /// </summary>
    class KeybindingCollector
    {
        /// <summary>
        /// 存储收集到的所有按键绑定器的私有列表
        /// 使用泛型列表来动态管理ButtonActionBinder组件
        /// </summary>
        private List<ButtonActionBinder> _buttonBinders = new List<ButtonActionBinder>();

        /// <summary>
        /// 存储收集到的所有开关绑定器的私有列表
        /// 使用泛型列表来动态管理ToggleActionBinder组件
        /// </summary>
        private List<ToggleActionBinder> _toggleBinders = new List<ToggleActionBinder>();

        /// <summary>
        /// 提供只读访问接口，外部代码可以通过此属性获取所有按钮绑定器
        /// 使用IReadOnlyList确保外部无法修改内部列表
        /// </summary>
        public IReadOnlyList<ButtonActionBinder> ButtonBinders => _buttonBinders;

        /// <summary>
        /// 提供只读访问接口，外部代码可以通过此属性获取所有开关绑定器
        /// 使用IReadOnlyList确保外部无法修改内部列表
        /// </summary>
        public IReadOnlyList<ToggleActionBinder> ToggleBinders => _toggleBinders;

        /// <summary>
        /// 保持向后兼容性的属性，返回按钮绑定器列表
        /// </summary>
        public IReadOnlyList<ButtonActionBinder> Binders => _buttonBinders;

        /// <summary>
        /// 静态日志源，用于输出按键绑定相关的调试信息
        /// 使用静态确保整个类共享同一个日志实例
        /// </summary>
        static ManualLogSource Logger = new("BindingDumper");

        /// <summary>
        /// 构造函数，初始化按键绑定收集器
        /// 主要负责注册日志源到BepInEx日志系统
        /// </summary>
        public KeybindingCollector()
        {
            // 将自定义日志源添加到BepInEx的日志源列表中
            // 这样日志信息就能在BepInEx的日志系统中正确显示
            BepInEx.Logging.Logger.Sources.Add(Logger);
        }

        /// <summary>
        /// 收集场景中所有的按键绑定信息
        /// 这是核心方法，会扫描整个场景并收集所有ButtonActionBinder和ToggleActionBinder组件
        /// 现在包含多种查找策略以确保找到所有按钮（包括未激活的）
        /// </summary>
        public void Collect()
        {
            _buttonBinders.Clear();
            _toggleBinders.Clear();

            // 收集ButtonActionBinder组件
            CollectButtonBinders();

            // 收集ToggleActionBinder组件
            CollectToggleBinders();

            // 输出最终收集结果的详细信息
            LogDetailedCollectionInfo();
        }

        /// <summary>
        /// 收集所有ButtonActionBinder组件
        /// </summary>
        private void CollectButtonBinders()
        {
            // 策略1：使用标准的FindObjectsOfType查找已激活的对象
            var activeBinders = UnityEngine.Object.FindObjectsOfType<ButtonActionBinder>();
            _buttonBinders.AddRange(activeBinders);
            Logger.LogInfo($"Found {activeBinders.Length} active ButtonActionBinder components.");

            // 策略2：使用Resources.FindObjectsOfTypeAll查找所有对象（包括未激活的）
            var allBinders = Resources.FindObjectsOfTypeAll<ButtonActionBinder>();
            foreach (var binder in allBinders)
            {
                // 避免重复添加已在策略1中找到的对象
                if (!_buttonBinders.Contains(binder))
                {
                    // 检查是否是场景中的对象（不是预制体）
                    if (IsSceneObject(binder.gameObject))
                    {
                        _buttonBinders.Add(binder);
                    }
                }
            }
            Logger.LogInfo($"Found {allBinders.Length} total ButtonActionBinder components (including inactive).");

            // 策略3：遍历所有UI Root和Canvas查找按钮
            FindButtonsInUIHierarchy<ButtonActionBinder>();
        }

        /// <summary>
        /// 收集所有ToggleActionBinder组件
        /// </summary>
        private void CollectToggleBinders()
        {
            // 策略1：使用标准的FindObjectsOfType查找已激活的对象
            var activeBinders = UnityEngine.Object.FindObjectsOfType<ToggleActionBinder>();
            _toggleBinders.AddRange(activeBinders);
            Logger.LogInfo($"Found {activeBinders.Length} active ToggleActionBinder components.");

            // 策略2：使用Resources.FindObjectsOfTypeAll查找所有对象（包括未激活的）
            var allBinders = Resources.FindObjectsOfTypeAll<ToggleActionBinder>();
            foreach (var binder in allBinders)
            {
                // 避免重复添加已在策略1中找到的对象
                if (!_toggleBinders.Contains(binder))
                {
                    // 检查是否是场景中的对象（不是预制体）
                    if (IsSceneObject(binder.gameObject))
                    {
                        _toggleBinders.Add(binder);
                    }
                }
            }
            Logger.LogInfo($"Found {allBinders.Length} total ToggleActionBinder components (including inactive).");

            // 策略3：遍历所有UI Root和Canvas查找开关
            FindButtonsInUIHierarchy<ToggleActionBinder>();
        }

        /// <summary>
        /// 检查游戏对象是否是场景中的对象（不是预制体或资源）
        /// </summary>
        /// <param name="obj">要检查的游戏对象</param>
        /// <returns>如果是场景对象则返回true</returns>
        private bool IsSceneObject(GameObject obj)
        {
            if (obj == null) return false;

            // 检查对象是否在场景中
            return obj.scene.IsValid() && obj.scene.name != null && !obj.scene.name.Equals("");
        }

        /// <summary>
        /// 在UI层次结构中查找指定类型的绑定器
        /// 遍历所有Canvas和UI Root来查找可能包含绑定器组件的对象
        /// </summary>
        /// <typeparam name="T">要查找的绑定器类型</typeparam>
        private void FindButtonsInUIHierarchy<T>() where T : Component
        {
            try
            {
                // 查找所有Canvas
                var canvases = UnityEngine.Object.FindObjectsOfType<Canvas>();
                foreach (var canvas in canvases)
                {
                    FindComponentsInTransform<T>(canvas.transform, 0);
                }

                // 查找所有UI Root（有些游戏可能使用不同的UI根节点）
                var uiRoots = UnityEngine.Object.FindObjectsOfType<Transform>()
                    .Where(t => t.name.ToLower().Contains("ui") || t.name.ToLower().Contains("canvas"))
                    .ToArray();

                foreach (var root in uiRoots)
                {
                    // 避免重复处理已经检查过的Canvas
                    if (root.GetComponent<Canvas>() == null)
                    {
                        FindComponentsInTransform<T>(root.transform, 0);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"Error during UI hierarchy search for {typeof(T).Name}: {e.Message}");
            }
        }

        /// <summary>
        /// 递归查找Transform层次结构中的指定类型组件
        /// </summary>
        /// <param name="parent">要搜索的父Transform</param>
        /// <param name="depth">当前递归深度</param>
        private void FindComponentsInTransform<T>(Transform parent, int depth) where T : Component
        {
            if (depth > 10) return; // 防止无限递归

            // 检查当前对象是否有指定类型的组件
            var component = parent.GetComponent<T>();
            if (component != null)
            {
                // 根据类型添加到相应的列表
                if (typeof(T) == typeof(ButtonActionBinder))
                {
                    var binder = component as ButtonActionBinder;
                    if (binder != null && !_buttonBinders.Contains(binder))
                    {
                        _buttonBinders.Add(binder);
                        Logger.LogInfo($"Found ButtonActionBinder in UI hierarchy: {parent.name}");
                    }
                }
                else if (typeof(T) == typeof(ToggleActionBinder))
                {
                    var binder = component as ToggleActionBinder;
                    if (binder != null && !_toggleBinders.Contains(binder))
                    {
                        _toggleBinders.Add(binder);
                        Logger.LogInfo($"Found ToggleActionBinder in UI hierarchy: {parent.name}");
                    }
                }
            }

            // 递归检查所有子对象
            foreach (Transform child in parent)
            {
                FindComponentsInTransform<T>(child, depth + 1);
            }
        }

        /// <summary>
        /// 输出详细的收集信息
        /// </summary>
        private void LogDetailedCollectionInfo()
        {
            Logger.LogInfo($"=== Action Binder Collection Summary ===");
            Logger.LogInfo($"ButtonActionBinder components: {_buttonBinders.Count}");
            Logger.LogInfo($"ToggleActionBinder components: {_toggleBinders.Count}");
            Logger.LogInfo($"Total components: {_buttonBinders.Count + _toggleBinders.Count}");

            // 统计按钮绑定器
            LogButtonBinderStats();

            // 统计开关绑定器
            LogToggleBinderStats();

            Logger.LogInfo($"=== End Collection Summary ===");
        }

        /// <summary>
        /// 统计并输出按钮绑定器信息
        /// </summary>
        private void LogButtonBinderStats()
        {
            Logger.LogInfo($"--- ButtonActionBinder Details ---");

            int activeCount = 0;
            int inactiveCount = 0;

            foreach (var binder in _buttonBinders)
            {
                if (binder.gameObject.activeInHierarchy)
                {
                    activeCount++;
                }
                else
                {
                    inactiveCount++;
                }
            }

            Logger.LogInfo($"Active: {activeCount}, Inactive: {inactiveCount}");

            // 输出每个绑定器的详细信息
            foreach (var binder in _buttonBinders)
            {
                var isActive = binder.gameObject.activeInHierarchy ? "Active" : "Inactive";
                Logger.LogInfo($"[{isActive}] ButtonActionBinder on: {binder.gameObject.name}");

                // 获取并检查关联的按钮组件
                var button = binder.Button;
                if (button != null)
                {
                    Logger.LogInfo($"  - Associated with button: {button.gameObject.name}");
                }

                // 获取并检查输入动作引用
                var actionRef = binder.Action;
                if (actionRef != null)
                {
                    Logger.LogInfo($"  - Bound to action: {actionRef.name}");
                }
            }
        }

        /// <summary>
        /// 统计并输出开关绑定器信息
        /// </summary>
        private void LogToggleBinderStats()
        {
            Logger.LogInfo($"--- ToggleActionBinder Details ---");

            int activeCount = 0;
            int inactiveCount = 0;

            foreach (var binder in _toggleBinders)
            {
                if (binder.gameObject.activeInHierarchy)
                {
                    activeCount++;
                }
                else
                {
                    inactiveCount++;
                }
            }

            Logger.LogInfo($"Active: {activeCount}, Inactive: {inactiveCount}");

            // 输出每个绑定器的详细信息
            foreach (var binder in _toggleBinders)
            {
                var isActive = binder.gameObject.activeInHierarchy ? "Active" : "Inactive";
                Logger.LogInfo($"[{isActive}] ToggleActionBinder on: {binder.gameObject.name}");

                // 获取并检查关联的开关组件（假设ToggleActionBinder有Toggle属性）
                try
                {
                    var toggle = binder.GetComponent<UnityEngine.UI.Toggle>();
                    if (toggle != null)
                    {
                        Logger.LogInfo($"  - Associated with toggle: {toggle.gameObject.name}");
                    }
                }
                catch
                {
                    // 如果无法获取Toggle组件，跳过
                }

                // 获取并检查输入动作引用
                try
                {
                    var actionRef = binder.Action;
                    if (actionRef != null)
                    {
                        Logger.LogInfo($"  - Bound to action: {actionRef.name}");
                    }
                }
                catch
                {
                    // 如果无法获取Action引用，跳过
                }
            }
        }

        /// <summary>
        /// 根据按钮名称查找对应的输入动作引用
        /// 这是一个便捷的查询方法，用于快速定位特定按钮的绑定信息
        /// </summary>
        /// <param name="name">要查找的按钮名称</param>
        /// <returns>找到的InputActionReference，如果未找到则返回null</returns>
        public InputActionReference FindSpecifiedActionRef(string name)
        {
            // 首先在按钮绑定器中查找
            var buttonResult = _buttonBinders
                .FirstOrDefault(binder => binder.Button != null && binder.Button.name == name)
                ?.Action;

            if (buttonResult != null)
                return buttonResult;

            // 然后在开关绑定器中查找
            var toggleResult = _toggleBinders
                .FirstOrDefault(binder =>
                {
                    try
                    {
                        var toggle = binder.GetComponent<UnityEngine.UI.Toggle>();
                        return toggle != null && toggle.name == name;
                    }
                    catch
                    {
                        return false;
                    }
                })
                ?.Action;

            return toggleResult;
        }

        /// <summary>
        /// 根据按钮名称查找对应的ButtonActionBinder
        /// </summary>
        /// <param name="name">要查找的按钮名称</param>
        /// <returns>找到的ButtonActionBinder，如果未找到则返回null</returns>
        public ButtonActionBinder FindButtonActionBinder(string name)
        {
            return _buttonBinders
                .FirstOrDefault(binder => binder.Button != null && binder.Button.name == name);
        }

        /// <summary>
        /// 根据开关名称查找对应的ToggleActionBinder
        /// </summary>
        /// <param name="name">要查找的开关名称</param>
        /// <returns>找到的ToggleActionBinder，如果未找到则返回null</returns>
        public ToggleActionBinder FindToggleActionBinder(string name)
        {
            return _toggleBinders
                .FirstOrDefault(binder =>
                {
                    try
                    {
                        var toggle = binder.GetComponent<UnityEngine.UI.Toggle>();
                        return toggle != null && toggle.name == name;
                    }
                    catch
                    {
                        return false;
                    }
                });
        }
    }
}
