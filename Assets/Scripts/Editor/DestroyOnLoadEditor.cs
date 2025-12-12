#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace VehicleUnjam
{
    [CustomEditor(typeof(DestroyOnLoad))]
    public class DestroyOnLoadEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DestroyOnLoad myComponent = (DestroyOnLoad)target; 

            if (GUILayout.Button("Mở Level Editor"))
            {
                LevelEditorWindow.Open();
            }
            
            DrawDefaultInspector(); 
        }
    }
}
#endif