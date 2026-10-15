using UnityEngine;

namespace RecycleScrollView
{
    internal readonly struct ScrollModeGeometry
    {
        private const int ViewportCornerCount = 4;
        private const int LeftCornerCount = 2;
        private const int BottomLeftCorner = 0;
        private const int BottomRightCorner = 3;
        private const float CenterPivot = .5f;

        private readonly ScrollLayoutGroup _layout;
        private readonly RectTransform _content;
        private readonly Rect _contentBounds;
        private readonly Vector2 _pivot;
        private readonly bool _isVertical;
        private readonly bool _fromHighEdge;

        public float ViewportStart { get; }
        public float ViewportLength { get; }

        public float ContentLength
        {
            get
            {
                if (_isVertical)
                {
                    return _contentBounds.height;
                }

                return _contentBounds.width;
            }
        }

        public ScrollModeGeometry(RecycleScrollView view)
        {
            _layout = view.LayoutGroup;
            _content = view.content;
            _contentBounds = _content.rect;
            _pivot = _layout.PivotPosition;
            _isVertical = _layout.IsVertical;

            if (_isVertical)
            {
                _fromHighEdge = _pivot.y >= CenterPivot;
            }
            else
            {
                _fromHighEdge = _pivot.x > CenterPivot;
            }

            var viewport = view.viewport;
            if (viewport == null)
            {
                viewport = view.RectTransform;
            }

            Rect bounds = viewport.rect;
            float start = float.PositiveInfinity;
            float end = float.NegativeInfinity;

            for (int corner = 0; corner < ViewportCornerCount; corner++)
            {
                float x = bounds.xMax;
                if (corner < LeftCornerCount)
                {
                    x = bounds.xMin;
                }

                float y = bounds.yMax;
                if (corner == BottomLeftCorner || corner == BottomRightCorner)
                {
                    y = bounds.yMin;
                }

                var point = new Vector3(x, y);
                Vector3 local = _content.InverseTransformPoint(viewport.TransformPoint(point));
                float distance;
                if (_isVertical)
                {
                    if (_fromHighEdge)
                    {
                        distance = _contentBounds.yMax - local.y;
                    }
                    else
                    {
                        distance = local.y - _contentBounds.yMin;
                    }
                }
                else
                {
                    if (_fromHighEdge)
                    {
                        distance = _contentBounds.xMax - local.x;
                    }
                    else
                    {
                        distance = local.x - _contentBounds.xMin;
                    }
                }

                start = Mathf.Min(start, distance);
                end = Mathf.Max(end, distance);
            }

            ViewportStart = start;
            ViewportLength = end - start;
        }

        public float GetLeadingOffset(int index, Vector2 size)
        {
            Vector2 position = _layout.GetSlotPos(index);
            if (_isVertical)
            {
                if (_fromHighEdge)
                {
                    return _contentBounds.yMax - position.y - size.y * (1 - _pivot.y);
                }

                return position.y - _contentBounds.yMin - size.y * _pivot.y;
            }

            if (_fromHighEdge)
            {
                return _contentBounds.xMax - position.x - size.x * (1 - _pivot.x);
            }

            return position.x - _contentBounds.xMin - size.x * _pivot.x;
        }

        public float GetAlignmentOffset(int index, Vector2 size, float alignment)
        {
            float leadingOffset = GetLeadingOffset(index, size);

            float slotLength = size.x;
            if (_isVertical)
            {
                slotLength = size.y;
            }

            float viewportAlignment = ViewportStart + ViewportLength * alignment;
            return leadingOffset + slotLength * alignment - viewportAlignment;
        }

        public void MoveToLeadingOffset(float target)
        {
            int direction = -1;
            if (_fromHighEdge)
            {
                direction = 1;
            }

            Vector3 displacement;
            if (_isVertical)
            {
                displacement = new Vector3(0, direction * (target - ViewportStart), 0);
            }
            else
            {
                displacement = new Vector3(direction * (target - ViewportStart), 0, 0);
            }

            Vector3 parentDisplacement = _content.localRotation * Vector3.Scale(displacement, _content.localScale);
            _content.anchoredPosition += new Vector2(parentDisplacement.x, parentDisplacement.y);
        }
    }
}
