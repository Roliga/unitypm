using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UnityUtils.UnityPM
{
    class AddSourceWindow : PopupWindowContent
    {
        private ISource source;
        private ISourceEditUI editUI;
        private List<ISource> sourceList;

        public override Vector2 GetWindowSize()
        {
            return new Vector2(400, 200);
        }

        public override void OnGUI(Rect rect)
        {
            bool valid = editUI.Draw();

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUI.enabled = valid;
            if (GUILayout.Button("Add"))
            {
                editUI.Apply();
                sourceList.Add(source);
                editorWindow.Close();
            }
            GUI.enabled = true;
            if (GUILayout.Button("Cancel"))
            {
                editorWindow.Close();
            }
            GUILayout.EndHorizontal();
        }

        public AddSourceWindow(ISourceWithEditUI source, List<ISource> sourceList)
        {
            this.source = source;
            this.editUI = source.GetEditUI();
            this.sourceList = sourceList;
        }
    }
}
