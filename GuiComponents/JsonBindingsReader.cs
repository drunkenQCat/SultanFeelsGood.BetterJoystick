using UnityEngine;
using System.Collections.Generic;

namespace SultanFeelsGood.BetterJoystick.GuiComponents
{
    // Data Transfer Objects (DTOs) to match the structure of the input actions JSON
    [System.Serializable]
    public class BindingDto
    {
        public string name;
        public string id;
        public string path;
        public string groups;
        public string action;
        public bool isComposite;
    }

    [System.Serializable]
    public class ActionDto
    {
        public string name;
        public string type;
        public string id;
    }

    [System.Serializable]
    public class ActionMapDto
    {
        public string name;
        public List<ActionDto> actions;
        public List<BindingDto> bindings;
    }

    [System.Serializable]
    public class InputActionAssetDto
    {
        public string name;
        public List<ActionMapDto> maps;
    }

    /// <summary>
    /// Reads and displays keybindings from a JSON representation of an InputActionAsset.
    /// </summary>
    public class JsonBindingsReader
    {

        private readonly InputActionAssetDto _actionAsset;
        private Vector2 _scrollPosition;

        private const string KEYBOARD_GROUP = "Keyboard&Mouse";
        private const string GAMEPAD_GROUP = "Gamepad";

        public JsonBindingsReader(string jsonContent)
        {
            if (string.IsNullOrEmpty(jsonContent))
            {
                _actionAsset = new InputActionAssetDto(); // Create an empty asset to avoid null refs
                Debug.LogError("JSON content for bindings reader was null or empty.");
                return;
            }
            _actionAsset = JsonUtility.FromJson<InputActionAssetDto>(jsonContent);
        }

        public void Draw()
        {
            if (_actionAsset?.maps == null)
            {
                GUILayout.Label("No action maps found or JSON could not be parsed.");
                return;
            }

            // --- Header ---
            GUILayout.BeginHorizontal();
            GUILayout.Label("Action", GUILayout.Width(120));
            GUILayout.Label("Keyboard", GUILayout.ExpandWidth(true));
            GUILayout.Label("Gamepad", GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2));

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            foreach (var map in _actionAsset.maps)
            {
                GUILayout.Label($"--- {map.name} Map ---");
                foreach (var action in map.actions)
                {
                    if (action.type != "Button") continue;

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(action.name, GUILayout.Width(120));

                    // Find and display bindings
                    string keyboardBinding = FindBindingPathForAction(map, action.name, KEYBOARD_GROUP);
                    string gamepadBinding = FindBindingPathForAction(map, action.name, GAMEPAD_GROUP);

                    GUILayout.Box(keyboardBinding, GUILayout.ExpandWidth(true));
                    GUILayout.Box(gamepadBinding, GUILayout.ExpandWidth(true));

                    GUILayout.EndHorizontal();
                    GUILayout.Space(5);
                }
            }

            GUILayout.EndScrollView();
        }

        private string FindBindingPathForAction(ActionMapDto map, string actionName, string group)
        {
            foreach (var binding in map.bindings)
            {
                if (binding.action == actionName && !binding.isComposite && binding.groups.Contains(group))
                {
                    return binding.path.Replace("<Keyboard>/", "").Replace("<Gamepad>/", "");
                }
            }
            return "None";
        }
    }
}
