using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Nomnom.RegexRename.Editor {
	/// <summary>
	/// Overrides F2 in the hierarchy to allow for regex renaming
	/// </summary>
	internal sealed class RenameWindow: EditorWindow {
		private Object[] _objects;
		private string _regex;
		private string _lastRegex;
		private string _replacement;
		private Regex _regexObject;
		private Vector2 _scrollValue;
		private bool _inProjectMode;
		private string[] _lastNames;
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

			Open(objects);
				
			e.Use();
		}

		private static void Open(Object[] objects) {
			RenameWindow window = GetWindow<RenameWindow>("Rename");
			window.OnDestroy();
			window._objects = objects;
			window._lastNames = new string[objects.Length];
			for (int i = 0; i < objects.Length; i++) {
				window._lastNames[i] = objects[i].name;
			}
			window.Show();

			window._inProjectMode = AssetDatabase.IsMainAsset(objects[0]);
		}

		private void OnDestroy() {
			_objects = null;
			_regex = string.Empty;
			_lastRegex = string.Empty;
			_regexObject = null;
			_replacement = string.Empty;
			_scrollValue = Vector2.zero;
			_lastNames = null;
		}

		private void OnGUI() {
			const string RegexName = nameof(RegexName);

			GUI.SetNextControlName(RegexName);
			_regex = EditorGUILayout.TextField("Criteria", _regex);
			_replacement = EditorGUILayout.TextField("Replacement", _replacement);

			if (!_hasFocused) {
				EditorGUI.FocusTextInControl(RegexName);
				_hasFocused = true;
			}

			if (_regex != _lastRegex) {
				_lastRegex = _regex;
				_regexObject = string.IsNullOrEmpty(_regex) ? null : new Regex(_regex);
			}

			bool validReplacement = !string.IsNullOrEmpty(_replacement);

			GUI.enabled = _regexObject != null && validReplacement;
			if (GUILayout.Button("Replace")) {
				foreach (Object obj in _objects) {
					string newName = _regexObject.Replace(obj.name, _replacement);
					
					if (_inProjectMode) {
						AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(obj), newName);
					} else {
						Undo.RecordObject(obj, "Changed name");
						obj.name = newName;
						PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
					}
				}

				if (_inProjectMode) {
					AssetDatabase.SaveAssets();
					AssetDatabase.Refresh();
				}
			}

			if (_inProjectMode && GUILayout.Button("Undo")) {
				for (int i = 0; i < _objects.Length; i++) {
					AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(_objects[i]), _lastNames[i]);
				}
				
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}
			GUI.enabled = true;
			
			EditorGUILayout.PrefixLabel("Preview", EditorStyles.boldLabel);
			
			GUI.backgroundColor = Color.white * 0.2f;
			_scrollValue = EditorGUILayout.BeginScrollView(_scrollValue, "Wizard Box");
			foreach (Object obj in _objects) {
				EditorGUILayout.BeginHorizontal();
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