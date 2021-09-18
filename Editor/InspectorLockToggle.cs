// Source: https://forum.unity.com/threads/shortcut-key-for-lock-inspector.95815/#post-3613249

using UnityEditor;
using System;
using System.Reflection;
 
static class InspectorLockToggle
{
	[MenuItem("Tools/Unity Utils/Toggle Inspector Lock &e")]
	static void SelectLockableInspector()
	{
		EditorWindow inspectorToBeLocked = EditorWindow.mouseOverWindow; // "EditorWindow.focusedWindow" can be used instead
		if (inspectorToBeLocked != null  && inspectorToBeLocked.GetType().Name == "InspectorWindow")
		{
			Type type = Assembly.GetAssembly(typeof(UnityEditor.Editor)).GetType("UnityEditor.InspectorWindow");
			PropertyInfo propertyInfo = type.GetProperty("isLocked");
			bool value = (bool)propertyInfo.GetValue(inspectorToBeLocked, null);
			propertyInfo.SetValue(inspectorToBeLocked, !value, null);
		 
			inspectorToBeLocked.Repaint();
		}
	}
}