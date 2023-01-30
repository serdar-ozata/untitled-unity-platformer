using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using Utils;

public class Level : MonoBehaviour {
    [Serializable]
    private class LevelOptions {
        public Vector2 transformation;

        public LevelOptions() {
            transformation = Vector2.one;
        }
    }


    [Serializable]
    private class LevelDictionary : SerializableDictionary<int, LevelOptions> {
    }

    [SerializeField] public Vector3 defaultSpawnPoint;
    [SerializeField, HideInInspector] private bool foldout = false;
    [SerializeField, HideInInspector] private LevelDictionary relatedLevels;
    [SerializeField] private Collider2D mapArea;
#if UNITY_EDITOR
    [CustomEditor(typeof(Level))]
    public class LevelEditor : Editor {
        private const string AddErrorPrompt = "This level index is already defined";
        private const string RemoveErrorPrompt = "This level index is undefined";
        private bool _addError = false;
        private bool _removeError = false;
        private int _selectedIndex = 0;
        private GUIStyle _labelStyle;

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            Level level = (Level)target;
            level.relatedLevels ??= new();
            ref LevelDictionary dictionary = ref level.relatedLevels;
            level.foldout = EditorGUILayout.BeginFoldoutHeaderGroup(level.foldout, "Levels Connected");
            if (level.foldout) {
                EditorGUI.indentLevel++;
                _labelStyle ??= new GUIStyle(GUI.skin.GetStyle("label")) {
                    fontStyle = FontStyle.Bold,
                };
                foreach ((int i, LevelOptions option) in dictionary) {
                    GUIContent labelContent = new GUIContent("Index " + i);
                    _labelStyle.CalcMinMaxWidth(labelContent, out float labelMin, out float _);
                    _labelStyle.fixedWidth = labelMin;
                    EditorGUILayout.LabelField(labelContent, _labelStyle);
                    option.transformation = EditorGUILayout.Vector2Field("Transformation", option.transformation);
                }

                EditorGUILayout.BeginHorizontal();
                GUIStyle intFieldStyle = new GUIStyle(GUI.skin.GetStyle("textfield")) {
                    fixedWidth = 100f
                };
                _selectedIndex = EditorGUILayout.IntField(_selectedIndex, intFieldStyle);
                GUIStyle btnStyle = new GUIStyle(GUI.skin.GetStyle("button"));
                btnStyle.CalcMinMaxWidth(new GUIContent("+"), out float min, out float max);
                btnStyle.fixedWidth = min;
                EditorGUILayout.Space(1f);
                if (GUILayout.Button("+", btnStyle)) {
                    if (dictionary.ContainsKey(_selectedIndex)) {
                        _addError = true;
                        _removeError = false;
                    }
                    else {
                        dictionary.Add(_selectedIndex, new LevelOptions());
                        _selectedIndex = 0;
                        _addError = false;
                        _removeError = false;
                    }
                }

                if (GUILayout.Button("-", btnStyle)) {
                    if (dictionary.ContainsKey(_selectedIndex)) {
                        dictionary.Remove(_selectedIndex);
                        _addError = false;
                        _removeError = false;
                        _selectedIndex = 0;
                    }
                    else {
                        _addError = false;
                        _removeError = true;
                    }
                }

                EditorGUILayout.EndHorizontal();
                if (_addError || _removeError) {
                    Color prevColor = GUI.color;
                    GUI.color = Color.red;
                    EditorGUILayout.LabelField(_addError ? AddErrorPrompt : RemoveErrorPrompt);
                    GUI.color = prevColor;
                }

                EditorGUI.indentLevel--;
            }


            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
#endif
    private void Awake() {
        relatedLevels ??= new LevelDictionary();
    }

    public Transform StartLevel(int prevLevel, PlayerController player, Transform cameraTf, out Collider2D shape) {
        Transform tf = player.transform;
        if (relatedLevels.TryGetValue(prevLevel, out LevelOptions options)) {
            tf.Translate(options.transformation, Space.Self);
        }
        else {
            tf.position = defaultSpawnPoint;
        }

        cameraTf.position = tf.position;
        shape = mapArea;
        gameObject.SetActive(true);
        return tf;
    }

    public virtual void CloseLevel(PlayerController player) {
        gameObject.SetActive(false);
    }
}