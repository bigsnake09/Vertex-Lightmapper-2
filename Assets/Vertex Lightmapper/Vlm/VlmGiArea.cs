using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Vlm
{
    [AddComponentMenu("Vlm/[Vlm] Gi Area")]
    public class VlmGiArea : MonoBehaviour
    {
        public Light LightSource;
        public Vector3Int Resolution = new Vector3Int(100, 100, 100);

#if UNITY_EDITOR
        [MenuItem("GameObject/Light/[Vlm] GI Area")]
        public static void CreateNewGiArea()
        {
            SceneView view = SceneView.currentDrawingSceneView;
            if (!view) view = SceneView.lastActiveSceneView;
            if (view)
            {
                Transform cameraT = view.camera.transform;

                GameObject newGi = new GameObject("GI Area");
                newGi.transform.position = cameraT.position + cameraT.forward * 10.0f;

                newGi.AddComponent<VlmGiArea>();

                Selection.activeObject = newGi;
            }
        }
#endif

        private void OnDrawGizmos()
        {
            Vector3 center = transform.position;

            Gizmos.color = new Color(0.0f, 1.0f, 0.5f, 0.3f);
            Gizmos.DrawCube(center, new Vector3(Resolution.x / 2, Resolution.y / 2, Resolution.z / 2));
            Gizmos.color = new Color(0.0f, 1.0f, 0.5f, 1.0f);
            Gizmos.DrawWireCube(center, new Vector3(Resolution.x / 2, Resolution.y / 2, Resolution.z / 2));
            Gizmos.color = Color.white;
        }
    }
}
