using System;
using System.Collections.Generic;
using UnityEngine;

namespace RecycleScrollView
{
    public sealed class LayoutCache
    {
        private const float CenterPivot = .5f;

        private Vector2[] _positions = Array.Empty<Vector2>();
        private float[] _lineStarts = Array.Empty<float>();
        private float[] _lineEnds = Array.Empty<float>();
        private float[] _crossOffsets = Array.Empty<float>();
        private int _slotCount;
        private int _lineCount;
        private int _constraintCount;

        public Vector2 ContentSize { get; private set; }

        public void Rebuild(IReadOnlyList<Vector2> sizes, bool vertical, int constraintCount, Vector2 spacing, LayoutPadding padding, Vector2 pivot)
        {
            if (constraintCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(constraintCount));
            }

            ValidateFinite(spacing, nameof(spacing));
            ValidateFinite(pivot, nameof(pivot));

            for (int i = 0; i < sizes.Count; i++)
            {
                Vector2 size = sizes[i];
                ValidateFinite(size, nameof(sizes));
            }

            _slotCount = sizes.Count;
            _constraintCount = constraintCount;
            _lineCount = 0;
            if (_slotCount != 0)
            {
                _lineCount = (_slotCount - 1) / constraintCount + 1;
            }

            if (_positions.Length < _slotCount)
            {
                Array.Resize(ref _positions, _slotCount);
            }

            if (_crossOffsets.Length < _slotCount)
            {
                Array.Resize(ref _crossOffsets, _slotCount);
            }

            if (_lineStarts.Length < _lineCount)
            {
                Array.Resize(ref _lineStarts, _lineCount);
            }

            if (_lineEnds.Length < _lineCount)
            {
                Array.Resize(ref _lineEnds, _lineCount);
            }

            if (_slotCount == 0)
            {
                ContentSize = Vector2.zero;
                return;
            }

            int forwardLeading;
            int forwardTrailing;
            int crossLeading;
            int crossPadding;
            float forwardSpacing;
            float crossSpacing;
            if (vertical)
            {
                if (pivot.y >= CenterPivot)
                {
                    forwardLeading = padding.Top;
                    forwardTrailing = padding.Bottom;
                }
                else
                {
                    forwardLeading = padding.Bottom;
                    forwardTrailing = padding.Top;
                }

                if (pivot.x > CenterPivot)
                {
                    crossLeading = padding.Right;
                }
                else
                {
                    crossLeading = padding.Left;
                }

                crossPadding = padding.Left + padding.Right;
                forwardSpacing = spacing.y;
                crossSpacing = spacing.x;
            }
            else
            {
                if (pivot.x > CenterPivot)
                {
                    forwardLeading = padding.Right;
                    forwardTrailing = padding.Left;
                }
                else
                {
                    forwardLeading = padding.Left;
                    forwardTrailing = padding.Right;
                }

                if (pivot.y >= CenterPivot)
                {
                    crossLeading = padding.Top;
                }
                else
                {
                    crossLeading = padding.Bottom;
                }

                crossPadding = padding.Top + padding.Bottom;
                forwardSpacing = spacing.x;
                crossSpacing = spacing.y;
            }

            float forwardOffset = forwardLeading;
            float farthestEnd = forwardOffset;
            float crossExtent = 0f;

            for (int line = 0; line < _lineCount; line++)
            {
                int first = line * constraintCount;
                int end = Math.Min(first + constraintCount, _slotCount);
                float lineSize = 0f;
                float crossOffset = 0f;
                _lineStarts[line] = forwardOffset;

                for (int i = first; i < end; i++)
                {
                    Vector2 size = sizes[i];
                    float forwardSize = size.x;
                    float crossSize = size.y;
                    if (vertical)
                    {
                        forwardSize = size.y;
                        crossSize = size.x;
                    }

                    lineSize = Math.Max(lineSize, forwardSize);
                    _crossOffsets[i] = crossLeading + crossOffset;
                    crossExtent = Math.Max(crossExtent, crossOffset + crossSize);

                    crossOffset += Math.Max(0, crossSize + crossSpacing);
                }

                farthestEnd = Math.Max(farthestEnd, forwardOffset + lineSize);

                _lineEnds[line] = farthestEnd;
                forwardOffset += Math.Max(0, lineSize + forwardSpacing);
            }

            float forwardLength = Math.Max(0, farthestEnd + forwardTrailing);
            float crossLength = Math.Max(0, crossExtent + crossPadding);
            if (vertical)
            {
                ContentSize = new Vector2(crossLength, forwardLength);
            }
            else
            {
                ContentSize = new Vector2(forwardLength, crossLength);
            }

            for (int i = 0; i < _slotCount; i++)
            {
                Vector2 size = sizes[i];
                float offset = _lineStarts[i / constraintCount];
                float xOffset = offset;
                float yOffset = _crossOffsets[i];
                if (vertical)
                {
                    xOffset = _crossOffsets[i];
                    yOffset = offset;
                }

                float x = Coordinate(ContentSize.x, xOffset, size.x, pivot.x, pivot.x > CenterPivot);
                float y = Coordinate(ContentSize.y, yOffset, size.y, pivot.y, pivot.y >= CenterPivot);

                if (constraintCount == 1 && vertical && pivot.x == CenterPivot)
                {
                    x = (padding.Left - padding.Right) * CenterPivot;
                }
                else if (constraintCount == 1 && !vertical && pivot.y == CenterPivot)
                {
                    y = (padding.Bottom - padding.Top) * CenterPivot;
                }

                _positions[i] = new Vector2(x, y);
            }
        }

        public Vector2 GetPosition(int index)
        {
            if (index < 0 || index >= _slotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _positions[index];
        }

        public void GetVisibleRange(float start, float end, int extraLines, out int first, out int endExclusive)
        {
            extraLines = Math.Max(0, extraLines);

            int firstLine = UpperBound(_lineEnds, _lineCount, start);
            int lastLine = LowerBound(_lineStarts, _lineCount, end);
            firstLine = Math.Max(0, firstLine - extraLines);
            lastLine = Math.Min(_lineCount, lastLine + extraLines);

            first = Math.Min(_slotCount, firstLine * _constraintCount);
            endExclusive = Math.Max(first, Math.Min(_slotCount, lastLine * _constraintCount));
        }

        private static float Coordinate(float length, float offset, float size, float pivot, bool fromHigh)
        {
            if (fromHigh)
            {
                return length * (1 - pivot) - offset - size * (1 - pivot);
            }

            return offset + size * pivot - length * pivot;
        }

        private static int UpperBound(float[] values, int count, float value)
        {
            int low = 0;
            int high = count;
            while (low < high)
            {
                int middle = low + (high - low) / 2;
                if (values[middle] <= value)
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle;
                }
            }

            return low;
        }

        private static int LowerBound(float[] values, int count, float value)
        {
            int low = 0;
            int high = count;
            while (low < high)
            {
                int middle = low + (high - low) / 2;
                if (values[middle] < value)
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle;
                }
            }

            return low;
        }

        private static void ValidateFinite(Vector2 value, string name)
        {
            if (float.IsNaN(value.x) || float.IsNaN(value.y) || float.IsInfinity(value.x) || float.IsInfinity(value.y))
            {
                throw new ArgumentException("Layout values must be finite.", name);
            }
        }
    }
}
