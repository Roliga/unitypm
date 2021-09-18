#if (UNITY_EDITOR)
using UnityEngine;
using UnityEditor;

public class KeepSceneAlive : MonoBehaviour
{
    public bool KeepSceneViewActive = true ;

    void Start()
    {
        if (KeepSceneViewActive && Application.isEditor)
        {
            EditorWindow.FocusWindowIfItsOpen(typeof(SceneView));
        }
    }
	
	[MenuItem("Tools/Unity Utils/Enable Keep Scene Alive")]
    public static void EnableAv3Testing() {
		GameObject go = new GameObject("Keep Scene Alive Control");
		go.AddComponent<KeepSceneAlive>();
    }
}
#endif