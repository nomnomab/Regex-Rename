using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Nomnom.RegexRename.Editor {
  /// <summary>
  /// Overrides F2 in the hierarchy to allow for regex renaming
  /// </summary>
  internal sealed class RenameWindow : EditorWindow {
    private Object[] _objects;
    private string _regexInput;
    private string _lastRegex;
    private string _replacement;
    private Regex _regexObject;
    private Vector2 _scrollValue;
    private bool _hasProjectAsset;
    private bool _hasFocused = false;

    [InitializeOnLoadMethod]
    private static void Load() {
      EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
      EditorApplication.projectWindowItemOnGUI += OnProjectGUI;
    }

    private static void OnProjectGUI(string guid, Rect selectionRect) {
      HandleEvent();
    }

    private static void OnHierarchyGUI(int instanceID, Rect rect) {
      HandleEvent();
    }

    private static void HandleEvent() {
      Event e = Event.current;
      Object[] objects = Selection.objects;

      if (objects.Length <= 1 || e.type != EventType.KeyDown || e.keyCode != KeyCode.F2) {
        return;
      }

      bool hasAsset = false;
      foreach (Object obj in objects) {
        if (AssetDatabase.IsMainAsset(obj)) {
          hasAsset = true;
        } else {
        }
      }

#if NOM_RENAME_NO_MIX
      if (hasAsset && hasScene) {
        return;
      }
#endif

      Open(objects, hasAsset);

      e.Use();
    }

    private static void Open(Object[] objects, bool hasProjectAssets) {
      RenameWindow window = GetWindow<RenameWindow>("Rename");
      window.OnDestroy();
      window._objects = objects;
      window.Show();

      window._hasProjectAsset = hasProjectAssets;
    }

    private void OnDestroy() {
      _objects = null;
      _regexInput = string.Empty;
      _lastRegex = string.Empty;
      _regexObject = null;
      _replacement = string.Empty;
      _scrollValue = Vector2.zero;
    }

    private static bool IsKeyDown(KeyCode keyCode) {
      return Event.current != null && Event.current.isKey && Event.current.keyCode == keyCode;
    }

    private void OnGUI() {
      const string ReplacementName = nameof(ReplacementName);

      _regexInput = EditorGUILayout.TextField("Pattern (optional)", _regexInput);
      GUI.SetNextControlName(ReplacementName);
      _replacement = EditorGUILayout.TextField("Replacement", _replacement);

      string regex = string.IsNullOrEmpty(_regexInput)
        ? ".+"
        : _regexInput;

      if (!_hasFocused) {
        EditorGUI.FocusTextInControl(ReplacementName);
        _hasFocused = true;
      }

      if (regex != _lastRegex) {
        _lastRegex = regex;
        _regexObject = string.IsNullOrEmpty(regex) ? null : new Regex(regex);
      }

      bool validReplacement = !string.IsNullOrEmpty(_replacement);

      if (_hasProjectAsset) {
        EditorGUILayout.HelpBox("Changes done to assets cannot be undone", MessageType.Warning);
      }

      GUI.enabled = _regexObject != null && validReplacement;
      if (GUILayout.Button("Replace") || (GUI.enabled && IsKeyDown(KeyCode.Return))) {
        IEnumerable<Object> assets = _objects.Where(obj => AssetDatabase.IsMainAsset(obj));
        IEnumerable<Object> sceneObjects = _objects.Where(obj => !AssetDatabase.IsMainAsset(obj));

        foreach (Object obj in assets) {
          string newName = _regexObject.Replace(obj.name, _replacement);
          string msg = AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(obj), newName);
          if (!string.IsNullOrEmpty(msg)) {
            Debug.LogError(msg);
          }
        }

        foreach (Object obj in sceneObjects) {
          string newName = _regexObject.Replace(obj.name, _replacement);
          Undo.RecordObject(obj, "Changed name");
          obj.name = newName;
          PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
        }

        if (sceneObjects.Count() > 0) {
          EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        if (_hasProjectAsset) {
          AssetDatabase.SaveAssets();
          AssetDatabase.Refresh();
          // Force repaint in case rename was performed by keyboard.
          Repaint();
        }

        Close();
        return;
      }
      GUI.enabled = true;

      EditorGUILayout.PrefixLabel("Preview", EditorStyles.boldLabel);

      GUI.backgroundColor = Color.white * 0.2f;
      _scrollValue = EditorGUILayout.BeginScrollView(_scrollValue, "Wizard Box");
      foreach (Object obj in _objects) {
        _ = EditorGUILayout.BeginHorizontal();
        {
          string name = obj.name;
          EditorGUILayout.LabelField(name);
          EditorGUILayout.LabelField(_regexObject == null || !validReplacement ? name : _regexObject.Replace(name, _replacement));
        }
        EditorGUILayout.EndHorizontal();
      }
      EditorGUILayout.EndScrollView();
    }
  }
}