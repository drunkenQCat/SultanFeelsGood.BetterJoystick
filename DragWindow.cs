using System;
using UnityEngine;

namespace SultanFeelsGood.BetterJoystick.Window;

/// <summary>
/// Draggable version of <see cref="CustomWindow"/>.
/// </summary>
public class DragWindow : CustomWindow
{
    /// <inheritdoc />
    public DragWindow(Rect rect, string title, Action<int> func) : base(rect, title, func)
    {
        Func = id =>
        {
            func(id);

            GUI.DragWindow(new Rect(0, 0, 10000, 20));

            // 使用温和的屏幕边界限制，避免窗口消失
            var clampedRect = Rect;
            clampedRect.x = Mathf.Clamp(clampedRect.x, 0, Screen.width - clampedRect.width);
            clampedRect.y = Mathf.Clamp(clampedRect.y, 0, Screen.height - clampedRect.height);
            Rect = clampedRect;
        };
    }

    /// <inheritdoc />
    public DragWindow(Rect rect, string title, Action func) : this(rect, title, _ => func())
    {
    }
}
