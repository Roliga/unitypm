using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UnityUtils.UnityPM
{
    class AddSourceWindow : EditSourceWindow
    {
        private ISource source;
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
            this.source = source;
            this.sourceList = sourceList;
        }
    }

    class EditSourceWindow : PopupWindowContent
    {
        protected ISourceEditUI editUI;

        public override Vector2 GetWindowSize()
        {
            return new Vector2(400, 200);
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
            bool valid = editUI.Draw();

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(!valid);
            DrawConfirmButton();
            EditorGUI.EndDisabledGroup();
            if (GUILayout.Button("Cancel"))
            {
                editorWindow.Close();
            }
            GUILayout.EndHorizontal();
        }

        public EditSourceWindow(ISourceWithEditUI source)
        {
            this.editUI = source.GetEditUI();
        }
    }
}
