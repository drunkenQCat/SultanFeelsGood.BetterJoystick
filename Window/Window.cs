using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SultanFeelsGood.BetterJoystick.Window;

/// <summary>
/// Utility wrapper over <see cref="GUI"/>.<see cref="GUI.Window(int,UnityEngine.Rect,UnityEngine.GUI.WindowFunction,UnityEngine.GUIContent,UnityEngine.GUIStyle)"/>.
/// </summary>
public class CustomWindow
{
    private static int _lastWindowId = 2135184938;
    private bool _sizeInitialized = false;

    /// <summary>
    /// Gets the next window id.
    /// </summary>
    /// <returns>A window id.</returns>
    public static int NextWindowId()
    {
        return _lastWindowId++;
    }

    /// <summary>
    /// Gets or sets the id of the window.
    /// </summary>
    public int Id { get; set; } = NextWindowId();

    /// <summary>
    /// Gets or sets a value indicating whether the window is enabled and shown.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the rect of the window.
    /// </summary>
    public Rect Rect { get; set; }

    /// <summary>
    /// Gets or sets the render function of the window.
    /// </summary>
    public Action<int> Func { get; set; }

    /// <summary>
    /// Gets or sets the title of the window.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomWindow"/> class.
    /// </summary>
    /// <param name="rect">The rect.</param>
    /// <param name="title">The title.</param>
    /// <param name="func">The render function.</param>
    public CustomWindow(Rect rect, string title, Action<int> func)
    {
        Rect = rect;
        Title = title;
        Func = func;
    }

    /// <inheritdoc />
    public CustomWindow(Rect rect, string title, Action func) : this(rect, title, _ => func())
    {
    }

    /// <summary>
    /// Draws the window gui.
    /// </summary>
    public virtual void OnGUI()
    {
        if (Enabled)
        {
            GUI.skin.label.wordWrap = false;

            // Initialize window size once to avoid GUILayoutOption garbage collection
            if (!_sizeInitialized)
            {
                var titleSize = GUI.skin.label.CalcSize(new GUIContent(Title));
                var newRect = Rect;
                newRect.width = Mathf.Max(450, titleSize.x * 2);
                newRect.height = 280;
                Rect = newRect;
                _sizeInitialized = true;
            }

            Rect = GUI.Window(Id, Rect, Func, Title);

            // Check if Input System is available before using input methods
            if (Mouse.current != null)
            {
                var mousePos = Mouse.current.position.ReadValue();
                var adjustedMousePos = new Vector2(mousePos.x, Screen.height - mousePos.y);
                if ((Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame) && Rect.Contains(adjustedMousePos))
                {
                    // Note: Input.ResetInputAxes() is old Input System, not needed with new Input System
                }
            }
        }
    }
}
