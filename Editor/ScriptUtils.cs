using UnityEngine;

namespace UnityUtils
{
    public static class ScriptUtils
    {
        public static string GetTransformPath(Transform transform)
        {
            string path = "/" + transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = "/" + transform.name + path;
            }
            return path;
        }
    }
}
