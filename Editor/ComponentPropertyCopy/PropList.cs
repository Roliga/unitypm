using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditorInternal;

namespace ComponentPropertyCopy
{
	class PropList
	{
		private ReorderableList list;

		public List<PropertyEntry> Props { get; set; } = new List<PropertyEntry>();
		public string Title { get; set; }
		public bool Visible
		{
			get => list != null;
			set
			{
				if (value)
				{
					if (list == null)
						CreateList();
				}
				else
				{
					DestroyList();
				}
			}
		}
		 
		private void CreateList()
		{
			list = new ReorderableList(Props, typeof(UnityEngine.Object), false, true, false, false)
			{
				drawHeaderCallback = (rect) =>
				{
					Rect labelRect = rect, buttonRectAll = rect, buttonRectNone = rect;
					float buttonWidth = rect.width * 0.2f;

					labelRect.width = rect.width - buttonWidth * 2;

					buttonRectAll.x = rect.xMax - buttonWidth * 2;
					buttonRectAll.width = buttonWidth;

					buttonRectNone.x = rect.xMax - buttonWidth;
					buttonRectNone.width = buttonWidth;

					EditorGUI.LabelField(labelRect, Title);
					if (GUI.Button(buttonRectAll, "All"))
						Props.ForEach(p => p.copy = true);
					if (GUI.Button(buttonRectNone, "None"))
						Props.ForEach(p => p.copy = false);
				},
				drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
				{
					rect.y += 2f;
					rect.height = EditorGUIUtility.singleLineHeight;

					PropertyEntry prop = Props[index];

					float originalValue = EditorGUIUtility.labelWidth;
					EditorGUIUtility.labelWidth = rect.width - EditorGUIUtility.singleLineHeight;
					prop.copy = EditorGUI.Toggle(rect, prop.name, prop.copy);
					EditorGUIUtility.labelWidth = originalValue;
				}
			};
		}

		private void DestroyList() => list = null;

		public void Draw()
		{
			if (list != null)
				list.DoLayoutList();
		}

		public PropList(string title)
		{
			Title = title;
		}
	}

	public class PropertyEntry
	{
		public string name;
		public bool copy;

		public PropertyEntry(string name)
		{
			this.name = name;
			copy = false;
		}
	}
}