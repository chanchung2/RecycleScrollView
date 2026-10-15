using System;
using UnityEngine;

namespace RecycleScrollView.Samples
{
    public enum SampleLayout
    {
        Vertical,
        Horizontal,
        Grid
    }

    public enum SampleEffect
    {
        None = 0,
        Carousel = 1,
        Highlight = 2
    }

    [Serializable]
    public sealed class SampleSettings
    {
        private const int DefaultDataCount = 50;
        private const float DefaultViewportWidth = 600;
        private const float DefaultViewportHeight = 500;
        private const float DefaultSlotWidth = 200;
        private const float DefaultSlotHeight = 50;
        private const float DefaultSpacing = 10;
        private const int DefaultGridColumns = 2;
        private const float DefaultAlignment = .5f;
        private const float DefaultEffectDistance = 200;
        private const float DefaultMinimumScale = .7f;
        private const float DefaultMaximumScale = 1.15f;
        private const float MinimumScale = .01f;

        public const int MaxDataCount = 1000000;

        [SerializeField]
        private SampleLayout _layout;
        [SerializeField]
        private int _dataCount = DefaultDataCount;
        [SerializeField]
        private Vector2 _viewportSize = new Vector2(DefaultViewportWidth, DefaultViewportHeight);
        [SerializeField]
        private Vector2 _slotSize = new Vector2(DefaultSlotWidth, DefaultSlotHeight);
        [Tooltip("Vary slot length along the scroll axis when Effect is None.")]
        [SerializeField]
        private bool _variableSlotSize;
        [SerializeField]
        private Vector2 _spacing = new Vector2(DefaultSpacing, DefaultSpacing);
        [SerializeField]
        private int _gridColumns = DefaultGridColumns;
        [SerializeField]
        private bool _showPerformanceHud = true;

        [SerializeField]
        private SampleEffect _effect;
        [SerializeField]
        private float _alignment = DefaultAlignment;
        [SerializeField]
        private float _effectDistance = DefaultEffectDistance;
        [SerializeField]
        private Vector2 _highlightScaleRange = new Vector2(DefaultMinimumScale, DefaultMaximumScale);

        public SampleLayout Layout
        {
            get
            {
                return _layout;
            }

            set
            {
                _layout = value;
            }
        }

        public int DataCount
        {
            get
            {
                return _dataCount;
            }

            set
            {
                _dataCount = value;
            }
        }

        public Vector2 ViewportSize
        {
            get
            {
                return _viewportSize;
            }

            set
            {
                _viewportSize = value;
            }
        }

        public Vector2 SlotSize
        {
            get
            {
                return _slotSize;
            }

            set
            {
                _slotSize = value;
            }
        }

        public Vector2 Spacing
        {
            get
            {
                return _spacing;
            }

            set
            {
                _spacing = value;
            }
        }

        public bool VariableSlotSize
        {
            get
            {
                return _variableSlotSize;
            }

            set
            {
                _variableSlotSize = value;
            }
        }

        public int GridColumns
        {
            get
            {
                return _gridColumns;
            }

            set
            {
                _gridColumns = value;
            }
        }

        public bool ShowPerformanceHud
        {
            get
            {
                return _showPerformanceHud;
            }

            set
            {
                _showPerformanceHud = value;
            }
        }

        public SampleEffect Effect
        {
            get
            {
                return _effect;
            }

            set
            {
                _effect = value;
            }
        }

        public float Alignment
        {
            get
            {
                return _alignment;
            }

            set
            {
                _alignment = value;
            }
        }

        public float EffectDistance
        {
            get
            {
                return _effectDistance;
            }

            set
            {
                _effectDistance = value;
            }
        }

        public Vector2 HighlightScaleRange
        {
            get
            {
                return _highlightScaleRange;
            }

            set
            {
                _highlightScaleRange = value;
            }
        }

        public SampleSettings Clone()
        {
            return (SampleSettings)MemberwiseClone();
        }

        public void Normalize()
        {
            _dataCount = Math.Clamp(_dataCount, 0, MaxDataCount);
            _gridColumns = Math.Max(1, _gridColumns);

            if (_layout < SampleLayout.Vertical || _layout > SampleLayout.Grid)
            {
                _layout = SampleLayout.Vertical;
            }

            if (_effect < SampleEffect.None || _effect > SampleEffect.Highlight)
            {
                _effect = SampleEffect.None;
            }

            if (Finite(_alignment))
            {
                _alignment = Mathf.Clamp01(_alignment);
            }
            else
            {
                _alignment = DefaultAlignment;
            }

            _effectDistance = Dimension(_effectDistance, DefaultEffectDistance);

            if (Finite(_highlightScaleRange.x))
            {
                _highlightScaleRange.x = Mathf.Max(MinimumScale, _highlightScaleRange.x);
            }
            else
            {
                _highlightScaleRange.x = DefaultMinimumScale;
            }

            if (Finite(_highlightScaleRange.y))
            {
                _highlightScaleRange.y = Mathf.Max(_highlightScaleRange.x, _highlightScaleRange.y);
            }
            else
            {
                _highlightScaleRange.y = Mathf.Max(_highlightScaleRange.x, DefaultMaximumScale);
            }

            _viewportSize = new Vector2(Dimension(_viewportSize.x, DefaultViewportWidth), Dimension(_viewportSize.y, DefaultViewportHeight));
            _slotSize = new Vector2(Dimension(_slotSize.x, DefaultSlotWidth), Dimension(_slotSize.y, DefaultSlotHeight));

            if (!Finite(_spacing.x))
            {
                _spacing.x = 0;
            }

            if (!Finite(_spacing.y))
            {
                _spacing.y = 0;
            }
        }

        private static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static float Dimension(float value, float fallback)
        {
            if (Finite(value))
            {
                return Mathf.Max(1, value);
            }

            return fallback;
        }
    }
}
