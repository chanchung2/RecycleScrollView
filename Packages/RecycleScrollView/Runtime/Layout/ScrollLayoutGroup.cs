using System;
using System.Collections.Generic;
using UnityEngine;

namespace RecycleScrollView
{
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class ScrollLayoutGroup : MonoBehaviour
    {
        private const float DefaultSpacing = 50;
        private const int MinimumConstraintCount = 1;
        private const int PivotColumnCount = 3;
        private const float CenterPivot = .5f;

        public enum ContentPivot
        {
            TopLeft,
            Top,
            TopRight,
            Left,
            Center,
            Right,
            BottomLeft,
            Bottom,
            BottomRight
        }

        public enum ScrollDirection
        {
            Horizontal,
            Vertical
        }

        [SerializeField]
        private LayoutPadding _padding;
        [SerializeField]
        private Vector2 _spacing = new Vector2(DefaultSpacing, DefaultSpacing);
        [SerializeField]
        [Min(MinimumConstraintCount)]
        private int _constraintCount = MinimumConstraintCount;
        [SerializeField]
        private ContentPivot _contentPivot = ContentPivot.Top;
        [SerializeField]
        private ScrollDirection _direction = ScrollDirection.Vertical;

        private RectTransform _rectTransform;
        private readonly LayoutCache _cache = new LayoutCache();
        private int _version;

        public RectTransform RectTransform
        {
            get
            {
                if (_rectTransform == null)
                {
                    _rectTransform = GetComponent<RectTransform>();
                }

                return _rectTransform;
            }
        }

        public int Version => _version;
        public bool IsVertical => _direction == ScrollDirection.Vertical;
        public Vector2 ContentSize => _cache.ContentSize;

        public int ConstraintCount
        {
            get
            {
                return _constraintCount;
            }

            set
            {
                value = Math.Max(MinimumConstraintCount, value);
                if (_constraintCount == value)
                {
                    return;
                }

                _constraintCount = value;
                _version++;
            }
        }

        public LayoutPadding Padding
        {
            get
            {
                return _padding;
            }

            set
            {
                _padding = value;
                _version++;
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
                if (_spacing == value)
                {
                    return;
                }

                _spacing = value;
                _version++;
            }
        }

        public ContentPivot Pivot
        {
            get
            {
                return _contentPivot;
            }

            set
            {
                if (_contentPivot == value)
                {
                    return;
                }

                _contentPivot = value;
                _version++;
            }
        }

        public ScrollDirection Direction
        {
            get
            {
                return _direction;
            }

            set
            {
                if (_direction == value)
                {
                    return;
                }

                _direction = value;
                _version++;
            }
        }

        public Vector2 PivotPosition => new Vector2((int)_contentPivot % PivotColumnCount * CenterPivot, 1 - (int)_contentPivot / PivotColumnCount * CenterPivot);

        public void Rebuild(IReadOnlyList<Vector2> sizes)
        {
            var pivot = PivotPosition;
            _cache.Rebuild(sizes, IsVertical, _constraintCount, _spacing, _padding, pivot);

            RectTransform.anchorMin = pivot;
            RectTransform.anchorMax = pivot;
            RectTransform.pivot = pivot;
            RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, ContentSize.x);
            RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, ContentSize.y);
        }

        public Vector2 GetSlotPos(int index)
        {
            return _cache.GetPosition(index);
        }

        public void GetVisibleRange(float start, float end, int extraLines, out int first, out int endExclusive)
        {
            _cache.GetVisibleRange(start, end, extraLines, out first, out endExclusive);
        }

        public void Arrange(IReadOnlyDictionary<int, ScrollSlot> slots, IReadOnlyList<Vector2> sizes)
        {
            var pivot = PivotPosition;
            foreach (var pair in slots)
            {
                var rect = pair.Value.CacheRectTransform;
                rect.anchorMin = pivot;
                rect.anchorMax = pivot;
                rect.pivot = pivot;

                var size = sizes[pair.Key];
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
                rect.anchoredPosition = _cache.GetPosition(pair.Key);
            }
        }

        public bool IsContentPivotBottomType()
        {
            return PivotPosition.y < CenterPivot;
        }

        public bool IsContentPivotLeftType()
        {
            return PivotPosition.x <= CenterPivot;
        }

        private void OnValidate()
        {
            _constraintCount = Math.Max(MinimumConstraintCount, _constraintCount);
            _version++;
        }
    }
}
