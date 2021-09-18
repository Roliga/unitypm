using UnityEngine;
using System.Collections;
using UnityEditor;

public class KeepSceneAlive : MonoBehaviour
{
    public bool KeepSceneViewActive = true;

    void Start()
    {
        if (this.KeepSceneViewActive && Application.isEditor)
        {
            UnityEditor.SceneView.FocusWindowIfItsOpen(typeof(UnityEditor.SceneView));
        }
    }
	
	[MenuItem("Tools/Enable Keep Scene Alive")]
    public static void EnableAv3Testing() {
		GameObject go = new GameObject("Keep Scene Alive Control");
		go.AddComponent<KeepSceneAlive>();
    }
}