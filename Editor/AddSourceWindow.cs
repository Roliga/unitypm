using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using SimpleJSON;

namespace UnityUtils.UnityPM
{
    class AddSourceWindow : EditSourceWindow
    {
        private List<ISource> sourceList;

        protected override void DrawConfirmButton()
        {
            if (GUILayout.Button("Add"))
            {
                editUI.Apply();
                sourceList.Add(source);
                editorWindow.Close();
            }
        }

        public AddSourceWindow(ISourceWithEditUI source, List<ISource> sourceList) : base(source)
        {
            this.sourceList = sourceList;
        }
    }

    class EditSourceWindow : PopupWindowContent
    {
        protected ISourceEditUI editUI;
        protected ISourceWithEditUI source;

        private float windowHeight;
        private static readonly GUIStyle containerStyle = new GUIStyle()
        {
            padding = new RectOffset(10, 10, 10, 10)
        };

        public override Vector2 GetWindowSize()
        {
            return new Vector2(400, windowHeight);
        }

        protected virtual void DrawConfirmButton()
        {
            if (GUILayout.Button("Apply"))
            {
                editUI.Apply();
                editorWindow.Close();
            }
        }

        public override void OnGUI(Rect rect)
        {
            Rect contentRect = EditorGUILayout.BeginVertical(containerStyle);

            EditorGUI.BeginDisabledGroup(source.Presets == null || source.Presets.Count == 0 || !(source is ISourceWithSettings));
            if(EditorGUILayout.DropdownButton(new GUIContent("Load Preset"), FocusType.Passive))
            {
                GenericMenu menu = new GenericMenu();

                foreach(var presetKV in source.Presets)
                {
                    menu.AddItem(new GUIContent(presetKV.Key), false, (presetJSON) =>
                    {
                        ((ISourceWithSettings)source).LoadSettings((JSONObject)presetJSON);
                        editUI = source.GetEditUI();
                    }, presetKV.Value);
                }

                menu.DropDown(GUILayoutUtility.GetLastRect());
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(20);

            bool valid = editUI.Draw();

            EditorGUILayout.Space(20);

            GUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(!valid);
            DrawConfirmButton();
            EditorGUI.EndDisabledGroup();
            if (GUILayout.Button("Cancel"))
            {
                editorWindow.Close();
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            if(contentRect.height > 10)
                windowHeight = contentRect.height;
        }

        public EditSourceWindow(ISourceWithEditUI source)
        {
            this.source = source;
            this.editUI = source.GetEditUI();
        }
    }
}
