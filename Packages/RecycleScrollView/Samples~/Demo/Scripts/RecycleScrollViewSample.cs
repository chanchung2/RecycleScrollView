using System.Collections.Generic;
using UnityEngine;

namespace RecycleScrollView.Samples
{
    public class RecycleScrollViewSample : MonoBehaviour
    {
        private const int VariableSizePatternLength = 3;
        private const float SmallSlotSizeFactor = .75f;
        private const float LargeSlotSizeFactor = 1.5f;

        [SerializeField]
        private global::RecycleScrollView.RecycleScrollView _scrollView;
        [SerializeField]
        private GameObject _slotPrefab;
        [SerializeField]
        private SampleSettings _settings = new SampleSettings();

        private SamplePerformanceHud _performanceHud;

        public SampleSettings Settings => _settings;
        public global::RecycleScrollView.RecycleScrollView ScrollView => _scrollView;
        public GameObject SlotPrefab => _slotPrefab;

        public int SelectedIndex { get; private set; } = -1;

        private void Start()
        {
            ApplyRuntime();
        }

        private void OnValidate()
        {
            NormalizeSettings();
        }

        private void NormalizeSettings()
        {
            if (_settings == null)
            {
                _settings = new SampleSettings();
            }

            _settings.Normalize();
        }

        public void Configure(SampleSettings settings)
        {
            _settings = settings.Clone();
            _settings.Normalize();
            ApplyLayout();
        }

        public void ApplyLayout()
        {
            NormalizeSettings();

            _scrollView.RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _settings.ViewportSize.x);
            _scrollView.RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _settings.ViewportSize.y);
            _scrollView.RectTransform.anchoredPosition = Vector2.zero;

            _scrollView.viewport.anchorMin = Vector2.zero;
            _scrollView.viewport.anchorMax = Vector2.one;
            _scrollView.viewport.offsetMin = Vector2.zero;
            _scrollView.viewport.offsetMax = Vector2.zero;

            var layout = _scrollView.LayoutGroup;
            if (_settings.Layout == SampleLayout.Horizontal)
            {
                layout.Direction = ScrollLayoutGroup.ScrollDirection.Horizontal;
            }
            else
            {
                layout.Direction = ScrollLayoutGroup.ScrollDirection.Vertical;
            }

            switch (_settings.Layout)
            {
                case SampleLayout.Vertical:
                    layout.Pivot = ScrollLayoutGroup.ContentPivot.Top;
                    break;
                case SampleLayout.Horizontal:
                    layout.Pivot = ScrollLayoutGroup.ContentPivot.Left;
                    break;
                default:
                    layout.Pivot = ScrollLayoutGroup.ContentPivot.TopLeft;
                    break;
            }

            layout.ConstraintCount = 1;
            if (_settings.Layout == SampleLayout.Grid)
            {
                layout.ConstraintCount = _settings.GridColumns;
            }

            layout.Padding = new LayoutPadding();
            if (_settings.Effect != SampleEffect.None)
            {
                float viewportLength;
                float slotLength;
                if (layout.IsVertical)
                {
                    viewportLength = _settings.ViewportSize.y;
                    slotLength = _settings.SlotSize.y;
                }
                else
                {
                    viewportLength = _settings.ViewportSize.x;
                    slotLength = _settings.SlotSize.x;
                }

                float freeSpace = Mathf.Max(0, viewportLength - slotLength);
                int leadingPadding = Mathf.RoundToInt(freeSpace * _settings.Alignment);
                int trailingPadding = Mathf.RoundToInt(freeSpace * (1 - _settings.Alignment));
                if (layout.IsVertical)
                {
                    layout.Padding = new LayoutPadding(0, 0, leadingPadding, trailingPadding);
                }
                else
                {
                    layout.Padding = new LayoutPadding(leadingPadding, trailingPadding, 0, 0);
                }
            }

            layout.Spacing = _settings.Spacing;

            _scrollView.horizontal = !layout.IsVertical;
            _scrollView.vertical = layout.IsVertical;
            if (_scrollView.isActiveAndEnabled)
            {
                _scrollView.horizontalScrollbar = _scrollView.horizontalScrollbar;
                _scrollView.verticalScrollbar = _scrollView.verticalScrollbar;
            }

            if (layout.IsVertical)
            {
                _scrollView.horizontalScrollbarVisibility = UnityEngine.UI.ScrollRect.ScrollbarVisibility.Permanent;
                _scrollView.verticalScrollbarVisibility = UnityEngine.UI.ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            }
            else
            {
                _scrollView.horizontalScrollbarVisibility = UnityEngine.UI.ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
                _scrollView.verticalScrollbarVisibility = UnityEngine.UI.ScrollRect.ScrollbarVisibility.Permanent;
            }

            if (_scrollView.horizontalScrollbar != null)
            {
                var rect = _scrollView.horizontalScrollbar.GetComponent<RectTransform>();
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _scrollView.RectTransform.rect.width);
                _scrollView.horizontalScrollbar.SetValueWithoutNotify(0);
                _scrollView.horizontalScrollbar.gameObject.SetActive(!layout.IsVertical);
            }

            if (_scrollView.verticalScrollbar != null)
            {
                var rect = _scrollView.verticalScrollbar.GetComponent<RectTransform>();
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _scrollView.RectTransform.rect.height);
                _scrollView.verticalScrollbar.SetValueWithoutNotify(1);
                _scrollView.verticalScrollbar.gameObject.SetActive(layout.IsVertical);
            }

            _scrollView.StopMovement();
            _scrollView.content.anchoredPosition = Vector2.zero;
        }

        public virtual Vector2[] BuildSizes()
        {
            NormalizeSettings();

            var sizes = new Vector2[_settings.DataCount];
            bool variableSize = _settings.VariableSlotSize && _settings.Effect == SampleEffect.None;
            bool horizontal = _settings.Layout == SampleLayout.Horizontal;

            for (int index = 0; index < sizes.Length; index++)
            {
                sizes[index] = _settings.SlotSize;
                if (!variableSize)
                {
                    continue;
                }

                float factor;
                switch (index % VariableSizePatternLength)
                {
                    case 0:
                        factor = SmallSlotSizeFactor;
                        break;
                    case 1:
                        factor = 1f;
                        break;
                    default:
                        factor = LargeSlotSizeFactor;
                        break;
                }

                if (horizontal)
                {
                    sizes[index].x *= factor;
                }
                else
                {
                    sizes[index].y *= factor;
                }
            }

            return sizes;
        }

        public void ApplyRuntime()
        {
            ApplyLayout();
            _scrollView.SetScrollMode(null);
            SelectedIndex = -1;

            _scrollView.UpdateScrollViewWithSizes(_slotPrefab, BuildSizes(), BindSlot);
            var scrollMode = CreateScrollMode();
            _scrollView.SetScrollMode(scrollMode);

            if (_settings.ShowPerformanceHud && _performanceHud == null)
            {
                var font = _slotPrefab.GetComponentInChildren<TMPro.TextMeshProUGUI>(true).font;
                _performanceHud = SamplePerformanceHud.Create(transform, _scrollView, font);
            }

            if (_performanceHud != null)
            {
                _performanceHud.gameObject.SetActive(_settings.ShowPerformanceHud);
            }
        }

        protected virtual ScrollMode CreateScrollMode()
        {
            if (_settings.Effect == SampleEffect.Carousel)
            {
                var mode = new SampleCarouselScrollMode(alignment: _settings.Alignment);
                mode.SelectionChanged += UpdateSelection;
                return mode;
            }

            if (_settings.Effect == SampleEffect.Highlight)
            {
                float viewportLength = _settings.ViewportSize.y;
                if (_settings.Layout == SampleLayout.Horizontal)
                {
                    viewportLength = _settings.ViewportSize.x;
                }

                float fadeDistance = Mathf.Max(_settings.EffectDistance, viewportLength);
                var mode = new SampleHighlightScrollMode(_settings.Alignment, _settings.EffectDistance, fadeDistance, _settings.HighlightScaleRange);
                mode.SelectionChanged += UpdateSelection;
                return mode;
            }

            return null;
        }

        public void SnapToNearest()
        {
            if (_scrollView.CurrentMode is CarouselScrollMode carousel)
            {
                carousel.SnapToNearest(_scrollView);
            }
        }

        public void ApplyPreviewEffects(IReadOnlyDictionary<int, ScrollSlot> slots)
        {
            SelectedIndex = -1;
            var mode = CreateScrollMode();
            if (mode is HighlightScrollMode highlight)
            {
                highlight.EvaluateSlots(_scrollView, slots);
                SelectedIndex = highlight.SelectedIndex;
            }
            else if (mode is CarouselScrollMode carousel)
            {
                carousel.EvaluateSlots(_scrollView, slots);
                SelectedIndex = carousel.SelectedIndex;
            }
        }

        private void UpdateSelection(int index)
        {
            SelectedIndex = index;
        }

        public virtual void BindSlot(ScrollSlot slot, int index)
        {
            ((RecycleScrollViewSlotSample)slot).UpdateUI(index);
        }
    }
}
