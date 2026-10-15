using TMPro;
using UnityEngine;

namespace RecycleScrollView.Samples
{
    public sealed class SamplePerformanceHud : MonoBehaviour
    {
        private const int HudSortingOrder = 100;
        private const float CanvasScaleMatch = .5f;
        private const float TextPadding = 12;
        private const float LabelFontSize = 18;
        private const float MillisecondsPerSecond = 1000;
        private static readonly Vector2 s_referenceResolution = new Vector2(1280, 720);
        private static readonly Vector2 s_topLeftPivot = new Vector2(0, 1);
        private static readonly Vector2 s_panelOffset = new Vector2(16, -16);
        private static readonly Vector2 s_panelSize = new Vector2(300, 180);
        private static readonly Color s_backgroundColor = new Color(.06f, .08f, .11f, .92f);
        private static readonly Color s_labelColor = new Color(.9f, .93f, .97f);

        private const float RefreshInterval = .5f;

        private global::RecycleScrollView.RecycleScrollView _scrollView;
        private TextMeshProUGUI _label;
        private float _elapsed;
        private int _frames;

        public float FramesPerSecond { get; private set; }
        public float FrameMilliseconds { get; private set; }

        public static SamplePerformanceHud Create(Transform parent, global::RecycleScrollView.RecycleScrollView view, TMP_FontAsset font)
        {
            var root = new GameObject("Performance HUD", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
            root.transform.SetParent(parent, false);

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = HudSortingOrder;

            var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = s_referenceResolution;
            scaler.matchWidthOrHeight = CanvasScaleMatch;

            var panel = new GameObject("Metrics", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            var rect = panel.GetComponent<RectTransform>();
            rect.SetParent(root.transform, false);
            rect.anchorMin = s_topLeftPivot;
            rect.anchorMax = s_topLeftPivot;
            rect.pivot = s_topLeftPivot;
            rect.anchoredPosition = s_panelOffset;
            rect.sizeDelta = s_panelSize;

            var background = panel.GetComponent<UnityEngine.UI.Image>();
            background.color = s_backgroundColor;
            background.raycastTarget = false;

            var textObject = new GameObject("Values", typeof(RectTransform), typeof(TextMeshProUGUI));
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(rect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.one * TextPadding;
            textRect.offsetMax = -Vector2.one * TextPadding;

            var label = textObject.GetComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSize = LabelFontSize;
            label.enableAutoSizing = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.color = s_labelColor;
            label.raycastTarget = false;
            label.text = "Recycle Scroll View\nCollecting frame timing...";

            var hud = root.AddComponent<SamplePerformanceHud>();
            hud._scrollView = view;
            hud._label = label;
            return hud;
        }

        private void OnEnable()
        {
            _elapsed = 0;
            _frames = 0;
            FramesPerSecond = 0;
            FrameMilliseconds = 0;
        }

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;
            _frames++;
            if (_elapsed < RefreshInterval)
            {
                return;
            }

            FramesPerSecond = _frames / _elapsed;
            FrameMilliseconds = MillisecondsPerSecond * _elapsed / _frames;

            _label.SetText(
                "Recycle Scroll View\nData Items: {0:0}\nActive Slots: {1:0}\n" +
                "FPS (avg): {2:1}\nFrame Time (avg): {3:2} ms\n" +
                "<size=12><color=#AEB8C4>Timing covers the entire app.</color></size>",
                _scrollView.DataCount,
                _scrollView.ActiveSlots.Count,
                FramesPerSecond,
                FrameMilliseconds);

            _elapsed = 0;
            _frames = 0;
        }
    }
}
