using UnityEditor;
using UnityEngine;

namespace RecycleScrollView.Editor
{
    public static class RecycleScrollViewEditor
    {
        private const int CreateMenuPriority = 2027;

        [MenuItem("GameObject/UI (Canvas)/RecycleScrollView", false, CreateMenuPriority)]
        public static void CreateRecycleScrollView(MenuCommand command)
        {
            var prefab = Resources.Load<GameObject>("Prefabs/UI/RecycleScrollView/RecycleScrollView");
            if (prefab == null)
            {
                Debug.LogError("The RecycleScrollView prefab could not be loaded.");
                return;
            }

            var parent = command.context as GameObject ?? Selection.activeGameObject;
            Canvas canvas = null;
            if (parent != null)
            {
                canvas = parent.GetComponentInParent<Canvas>();
            }

            if (canvas == null)
            {
                var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
                canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                Undo.RegisterCreatedObjectUndo(canvasObject, "Create Canvas");
                parent = canvasObject;
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            Undo.RegisterCreatedObjectUndo(instance, "Create RecycleScrollView");
            GameObjectUtility.SetParentAndAlign(instance, parent);
            instance.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            Selection.activeGameObject = instance;

            if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var events = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
                Undo.RegisterCreatedObjectUndo(events, "Create EventSystem");
            }
        }
    }
}
