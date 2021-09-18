#if (UNITY_EDITOR)
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using UnityEditorInternal;
using System;
using System.Reflection;

namespace ComponentPropertyCopy
{
	public partial class ComponentPropertyCopy : EditorWindow
	{
		Component src;
		public List<Component> dsts = new List<Component>();
		ReorderableList dstList;

		ScriptableObject target;
		SerializedObject so;
		SerializedProperty dstProp;

		PropList props = new PropList("Properties");
		PropList propsScript = new PropList("Scripting Properties");
		PropList propsScriptFields = new PropList("Scripting Fields");

		Rect headerRect = new Rect();

		Vector2 scrollPos;

		[MenuItem("Tools/Component Property Copy")]
		public static void Open()
		{
			GetWindow<ComponentPropertyCopy>();
		}


		void SetupDst()
		{
			dstList = new ReorderableList(so, dstProp, false, true, true, true)
			{
				drawHeaderCallback = (rect) =>
				{
					headerRect = rect;
					Rect labelRect = rect, buttonRect = rect;
					float buttonWidth = rect.width * 0.2f;

					labelRect.width = rect.xMax - buttonWidth;

					buttonRect.x = rect.xMax - buttonWidth;
					buttonRect.width = buttonWidth;

					EditorGUI.LabelField(labelRect, "Destionations");
					if (GUI.Button(buttonRect, "Clear"))
						dsts.Clear();
				},
				drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
				{
					rect.y += 2f;
					rect.height = EditorGUIUtility.singleLineHeight;

					var sp = dstList.serializedProperty.GetArrayElementAtIndex(index);
					EditorGUI.ObjectField(rect, sp, src.GetType(), GUIContent.none);
				},
				onMouseDragCallback = (ReorderableList self) =>
				{

				}
			};
		}

		void DestroyDst()
		{
			dsts.Clear();
			dstList = null;
		}

		void SetupProps()
		{
			props.Props.Clear();
			Type type = src.GetType();
			if (Quirks.ContainsKey(type))
			{
				foreach (string key in Quirks[type].Keys)
				{
					props.Props.Add(new PropertyEntry(key));
				}
			}

			propsScript.Props.Clear();
			foreach (PropertyInfo f in src.GetType().GetProperties())
			{
				SerializedObject s = new SerializedObject(src);
				SerializedProperty p = s.FindProperty(f.Name);

				if (f.CanWrite && f.CanRead)
					propsScript.Props.Add(new PropertyEntry(f.Name));
			}

			propsScriptFields.Props.Clear();
			foreach (FieldInfo f in src.GetType().GetFields())
			{
				SerializedObject s = new SerializedObject(src);
				SerializedProperty p = s.FindProperty(f.Name);

				if (true)
					propsScriptFields.Props.Add(new PropertyEntry(f.Name));
			}

			props.Visible = true;
			propsScript.Visible = true;
			propsScriptFields.Visible = true;
		}

		public void OnEnable()
		{
			target = this;
			so = new SerializedObject(target);
			dstProp = so.FindProperty("dsts");

			if (src != null)
			{
				SetupDst();
				SetupProps();
			}
		}

		private void HideProps()
		{
			props.Visible = false;
			propsScript.Visible = false;
			propsScriptFields.Visible = false;
		}

		void EventHandling()
		{
			Event evt = Event.current;

			switch (evt.type)
			{
				case EventType.DragUpdated:
				case EventType.DragPerform:
					if (!headerRect.Contains(evt.mousePosition))
						return;

					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

					if (evt.type == EventType.DragPerform)
					{
						DragAndDrop.AcceptDrag();

						foreach (UnityEngine.Object dragged_object in DragAndDrop.objectReferences)
						{
							Debug.Log(dragged_object.name);
							if (dragged_object is GameObject)
							{
								foreach (Component c in ((GameObject)dragged_object).GetComponents(src.GetType()))
									dsts.Add(c);
							}
							else if (dragged_object.GetType() == src.GetType())
							{
								dsts.Add((Component)dragged_object);
							}
						}
					}
					break;
			}
		}

		public void OnGUI()
		{
			GUIStyle scrollStyle = new GUIStyle();
			scrollStyle.padding = new RectOffset(10, 10, 10, 10);
			scrollPos = EditorGUILayout.BeginScrollView(scrollPos, scrollStyle);

			Component prev = src;
			src = EditorGUILayout.ObjectField("Source", src, typeof(Component), true) as Component;
			EditorGUILayout.Space(10);

			if (src == null)
			{
				DestroyDst();
				HideProps();
			}
			else
			{
				if (prev != null && src.GetType() != prev.GetType())
					DestroyDst();

				if (src != prev)
				{
					SetupDst();
					SetupProps();
				}
			}

			so.Update();
			if (dstList != null)
				dstList.DoLayoutList();

			EditorGUILayout.Space(10);
			props.Draw();
			propsScript.Draw();
			propsScriptFields.Draw();

			so.ApplyModifiedProperties();

			EventHandling();

			if (GUILayout.Button("Copy") && src != null)
			{
				Type type = src.GetType();
				int count = 0;

				foreach (Component dst in dsts)
				{
					Undo.RecordObject(dst.gameObject, "Copy Properties");
					Undo.RecordObject(dst, "Copy Properties");
					PrefabUtility.RecordPrefabInstancePropertyModifications(dst.gameObject);
				}

				foreach (PropertyEntry e in props.Props)
					if (e.copy)
					{
						foreach (Component dst in dsts)
							Quirks[type][e.name](src, dst);
						count++;
					}

				foreach (PropertyEntry e in propsScript.Props)
					if (e.copy)
					{
						PropertyInfo f = type.GetProperty(e.name);
						object value = f.GetValue(src);
						foreach (Component dst in dsts)
							f.SetValue(dst, value);
						count++;
					}

				foreach (PropertyEntry e in propsScriptFields.Props)
					if (e.copy)
					{
						FieldInfo f = type.GetField(e.name);
						object value = f.GetValue(src);
						foreach (Component dst in dsts)
							f.SetValue(dst, value);
						count++;
					}

				Debug.Log("Copied " + count + " properties to " + dsts.Count + " components.");
			}

			EditorGUILayout.EndScrollView();
		}
	}
}
#endif
