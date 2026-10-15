using UnityEditor;

namespace RecycleScrollView.Editor
{
    [CustomEditor(typeof(ScrollLayoutGroup))]
    public sealed class ScrollLayoutGroupEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
