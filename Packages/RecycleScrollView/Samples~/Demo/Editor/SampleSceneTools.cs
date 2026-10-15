using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RecycleScrollView.Samples.Editor
{
    [InitializeOnLoad]
    public static class SampleSceneTools
    {
        private const int UiLayer = 5;
        private const int ViewportCornerCount = 4;
        private const int PreviewBufferLines = 1;
        private const float CanvasScaleMatch = .5f;
        private static readonly Vector3 s_cameraPosition = new Vector3(0, 0, -10);
        private static readonly Color s_backgroundColor = new Color(.13f, .15f, .19f);
        private static readonly Vector2 s_referenceResolution = new Vector2(1280, 720);
        private static readonly Vector2 s_centerPivot = new Vector2(.5f, .5f);

        private const string SceneGuid = "2c69f8326a53b5e4f9d122608560c446";
        private const string PreviewName = "Sample Preview";
        private const int PreviewLimit = 200;

        private static readonly Dictionary<int, ScrollSlot> s_previewSlots = new Dictionary<int, ScrollSlot>();

        public static event Action SettingsChanged;

        public static bool IsSampleSceneActive
        {
            get
            {
                var scene = SceneManager.GetActiveScene();
                string path = AssetDatabase.GUIDToAssetPath(SceneGuid);
                return scene.path == path;
            }
        }

        public static void RebuildSampleScene()
        {
            var path = AssetDatabase.GUIDToAssetPath(SceneGuid);
            ClearPreview();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = s_cameraPosition;

            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = s_backgroundColor;
            camera.orthographic = true;

            var canvasObject = new GameObject("Sample Canvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvasObject.layer = UiLayer;
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = s_referenceResolution;
            scaler.matchWidthOrHeight = CanvasScaleMatch;

            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));

            var prefab = Resources.Load<GameObject>("Prefabs/UI/RecycleScrollView/RecycleScrollView");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "Sample Scroll View";

            var rect = instance.GetComponent<RectTransform>();
            rect.SetParent(canvasObject.transform, false);
            rect.anchorMin = s_centerPivot;
            rect.anchorMax = s_centerPivot;
            rect.pivot = s_centerPivot;
            rect.anchoredPosition = Vector2.zero;

            var sampleObject = new GameObject("Sample Controller");
            var sample = sampleObject.AddComponent<RecycleScrollViewSample>();

            var serialized = new SerializedObject(sample);
            serialized.FindProperty("_scrollView").objectReferenceValue = instance.GetComponent<global::RecycleScrollView.RecycleScrollView>();

            var slotPrefab = Resources.Load<GameObject>("Prefabs/UI/RecycleScrollView/RecycleScrollViewSlotSample");
            serialized.FindProperty("_slotPrefab").objectReferenceValue = slotPrefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            sample.Configure(new SampleSettings());

            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), path);
            RefreshPreview();
        }

        static SampleSceneTools()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            AssemblyReloadEvents.beforeAssemblyReload += ClearPreview;
            Undo.undoRedoPerformed += RefreshPreview;
            EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.delayCall += RefreshPreview;
        }

        private static void OnActiveSceneChanged(Scene previous, Scene current)
        {
            RefreshPreview();
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            RefreshPreview();
        }

        public static void QueueSettingsRefresh()
        {
            EditorApplication.delayCall -= RefreshSettings;
            EditorApplication.delayCall += RefreshSettings;
        }

        private static void RefreshSettings()
        {
            if (EditorApplication.isPlaying)
            {
                FindSample()?.ApplyRuntime();
                SettingsChanged?.Invoke();
            }
            else
            {
                RefreshPreview();
            }
        }

        public static RecycleScrollViewSample FindSample()
        {
            if (!IsSampleSceneActive)
            {
                return null;
            }

            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var sample = root.GetComponentInChildren<RecycleScrollViewSample>(true);
                if (sample != null)
                {
                    return sample;
                }
            }

            return null;
        }

        public static bool OpenSampleScene()
        {
            if (IsSampleSceneActive)
            {
                RefreshPreview();
                return true;
            }

            if (EditorApplication.isPlaying)
            {
                return false;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return false;
            }

            EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath(SceneGuid));
            RefreshPreview();
            return true;
        }

        public static RecycleScrollViewSample Apply(SampleSettings settings)
        {
            var sample = FindSample();
            if (sample == null || sample.ScrollView == null || sample.SlotPrefab == null)
            {
                return null;
            }

            var view = sample.ScrollView;
            if (!EditorApplication.isPlaying)
            {
                var objects = new UnityEngine.Object[]
                {
                    sample,
                    view,
                    view.RectTransform,
                    view.LayoutGroup,
                    view.content
                };
                Undo.RecordObjects(objects, "Change sample settings");
            }

            sample.Configure(settings);
            if (EditorApplication.isPlaying)
            {
                sample.ApplyRuntime();
            }
            else
            {
                RefreshPreview();
                EditorUtility.SetDirty(sample);
                EditorSceneManager.MarkSceneDirty(sample.gameObject.scene);
            }

            return sample;
        }

        public static void RefreshPreview()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            ClearPreview();
            var sample = FindSample();
            if (sample == null || sample.ScrollView == null || sample.SlotPrefab == null)
            {
                return;
            }

            sample.ApplyLayout();

            var view = sample.ScrollView;
            var sizes = sample.BuildSizes();
            view.LayoutGroup.Rebuild(sizes);

            Canvas.ForceUpdateCanvases();

            var corners = new Vector3[ViewportCornerCount];
            view.viewport.GetWorldCorners(corners);

            var start = float.PositiveInfinity;
            var end = float.NegativeInfinity;
            foreach (var corner in corners)
            {
                var local = view.content.InverseTransformPoint(corner);
                float distance;
                if (view.LayoutGroup.IsVertical)
                {
                    distance = view.content.rect.yMax - local.y;
                }
                else
                {
                    distance = local.x - view.content.rect.xMin;
                }

                start = Mathf.Min(start, distance);
                end = Mathf.Max(end, distance);
            }

            view.LayoutGroup.GetVisibleRange(start, end, PreviewBufferLines, out var first, out var endExclusive);
            endExclusive = Math.Min(endExclusive, first + PreviewLimit);

            var preview = new GameObject(PreviewName, typeof(RectTransform));
            preview.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

            var rect = preview.GetComponent<RectTransform>();
            rect.SetParent(view.content, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = view.content.pivot;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            for (int index = first; index < endExclusive; index++)
            {
                var slot = UnityEngine.Object.Instantiate(sample.SlotPrefab, rect).GetComponent<ScrollSlot>();
                slot.gameObject.hideFlags = preview.hideFlags;
                slot.RectSize = sizes[index];
                sample.BindSlot(slot, index);
                s_previewSlots.Add(index, slot);
            }

            view.LayoutGroup.Arrange(s_previewSlots, sizes);
            sample.ApplyPreviewEffects(s_previewSlots);

            SceneView.RepaintAll();
            EditorApplication.QueuePlayerLoopUpdate();
            SettingsChanged?.Invoke();
        }

        private static void ClearPreview()
        {
            s_previewSlots.Clear();
            foreach (var sample in Resources.FindObjectsOfTypeAll<RecycleScrollViewSample>())
            {
                if (!sample.gameObject.scene.IsValid() || !sample.gameObject.scene.isLoaded || sample.ScrollView == null)
                {
                    continue;
                }

                var content = sample.ScrollView.content;
                for (int index = content.childCount - 1; index >= 0; index--)
                {
                    var child = content.GetChild(index);
                    if (child.name == PreviewName)
                    {
                        UnityEngine.Object.DestroyImmediate(child.gameObject);
                    }
                }
            }
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                ClearPreview();
            }

            if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.delayCall += RefreshPreview;
            }
        }
    }
}
