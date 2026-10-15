using System;
using System.Collections.Generic;
using UnityEngine;

namespace RecycleScrollView
{
    public class HighlightScrollMode : ScrollMode
    {
        private const float CenterAlignment = .5f;
        private const float LegacyScaleOffset = .2f;
        private const float LeadingFadeMultiplier = 2;

        private int _lastNotifiedIndex = -1;
        public float Alignment { get; }
        public float EffectDistance { get; }
        public float FadeDistance { get; }
        public int SelectedIndex { get; private set; } = -1;
        public float MinimumScrollOffset { get; }
        public float EndOffsetFromContentEnd { get; }

        public event Action<int> SelectionChanged;

        private const float DefaultEffectDistance = 200;
        private readonly Action<ScrollSlot, HighlightSlotState> _updateSlotEffect;
        private readonly List<KeyValuePair<int, ScrollSlot>> _slotsInLogicalOrder = new List<KeyValuePair<int, ScrollSlot>>();
        private readonly bool _useLegacyScrollLimits;

        public HighlightScrollMode(Action<ScrollSlot, HighlightSlotState> updateSlotEffect = null, float alignment = CenterAlignment, float effectDistance = DefaultEffectDistance, float? fadeDistance = null)
        {
            if (float.IsNaN(alignment) || alignment < 0 || alignment > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(alignment));
            }

            if (float.IsNaN(effectDistance) || float.IsInfinity(effectDistance) || effectDistance <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(effectDistance));
            }

            float fadeDistanceValue = fadeDistance ?? effectDistance;
            if (float.IsNaN(fadeDistanceValue) || float.IsInfinity(fadeDistanceValue) || fadeDistanceValue <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fadeDistance));
            }

            _updateSlotEffect = updateSlotEffect;
            Alignment = alignment;
            EffectDistance = effectDistance;
            FadeDistance = fadeDistanceValue;
        }


        public HighlightScrollMode(float topLimit, float bottomLimitOffset, Action<ScrollSlot, bool, float, float> applyLegacyEffect)
        {
            if (float.IsNaN(topLimit) || float.IsInfinity(topLimit) || float.IsNaN(bottomLimitOffset) || float.IsInfinity(bottomLimitOffset))
            {
                throw new ArgumentException("Scroll offsets must be finite.");
            }

            _useLegacyScrollLimits = true;
            MinimumScrollOffset = topLimit;
            EndOffsetFromContentEnd = bottomLimitOffset;

            if (topLimit == 0)
            {
                EffectDistance = DefaultEffectDistance;
            }
            else
            {
                EffectDistance = Mathf.Abs(topLimit);
            }

            FadeDistance = EffectDistance;
            _updateSlotEffect = (slot, state) =>
            {
                if (!state.IsActive)
                {
                    slot.transform.localScale = Vector3.one;

                    var canvasGroup = slot.GetComponent<CanvasGroup>();
                    if (canvasGroup != null)
                    {
                        canvasGroup.alpha = 1;
                    }
                }

                float legacyScale = 1;
                if (state.IsActive)
                {
                    legacyScale = Mathf.Max(0, 1 - Mathf.Abs(state.DistanceToAlignment) / EffectDistance + LegacyScaleOffset);
                }

                applyLegacyEffect?.Invoke(slot, state.IsSelected, legacyScale, state.Opacity);
            };
        }

        [Obsolete("Use MinimumScrollOffset for the legacy adapter, or Alignment with layout padding.")]
        public float TopLimit => MinimumScrollOffset;

        [Obsolete("Use EndOffsetFromContentEnd for the legacy adapter, or layout padding.")]
        public float BottomLimitOffset => EndOffsetFromContentEnd;

        public override float GetAlignment(RecycleScrollView scrollView)
        {
            return Alignment;
        }

        public override void OnDataChanged(RecycleScrollView scrollView)
        {
            if (scrollView.DataCount == 0)
            {
                SetSelection(-1);
            }

            if (!_useLegacyScrollLimits || scrollView.DataCount == 0)
            {
                return;
            }

            var geometry = new ScrollModeGeometry(scrollView);
            Vector2 slotSize = scrollView.GetSlotSize(0);
            float leadingOffset = geometry.GetLeadingOffset(0, slotSize);
            geometry.MoveToLeadingOffset(MinimumScrollOffset + leadingOffset);
        }

        public override void BeforeUpdate(RecycleScrollView scrollView)
        {
            if (!_useLegacyScrollLimits)
            {
                return;
            }

            var geometry = new ScrollModeGeometry(scrollView);
            float maximumOffset = Mathf.Max(MinimumScrollOffset, geometry.ContentLength + EndOffsetFromContentEnd);
            float scrollOffset = Mathf.Clamp(geometry.ViewportStart, MinimumScrollOffset, maximumOffset);
            geometry.MoveToLeadingOffset(scrollOffset);
        }

        public override void UpdateMode(RecycleScrollView scrollView)
        {
            EvaluateSlots(scrollView, scrollView.ActiveSlots);
        }


        public void EvaluateSlots(RecycleScrollView scrollView, IReadOnlyDictionary<int, ScrollSlot> slots)
        {
            var geometry = new ScrollModeGeometry(scrollView);

            float closestDistance = float.PositiveInfinity;
            int selectedIndex = -1;
            foreach (var pair in slots)
            {
                float distance = Mathf.Abs(GetAlignmentDistance(geometry, pair.Key, pair.Value.RectSize));
                if (distance < closestDistance || Mathf.Approximately(distance, closestDistance) && pair.Key < selectedIndex)
                {
                    closestDistance = distance;
                    selectedIndex = pair.Key;
                }
            }

            SetSelection(selectedIndex);

            foreach (var pair in slots)
            {
                float distance = GetAlignmentDistance(geometry, pair.Key, pair.Value.RectSize);
                float proximity = 1 - Mathf.Abs(distance) / EffectDistance;
                float scaleBlend = Mathf.Clamp01(proximity);
                if (_useLegacyScrollLimits)
                {
                    scaleBlend = Mathf.Clamp01(proximity + LegacyScaleOffset);
                }

                float fadeProximity = 1 - Mathf.Abs(distance) / FadeDistance;
                float opacity = 1;
                if (distance < 0 && fadeProximity < 0)
                {
                    opacity = Mathf.Clamp01(1 + fadeProximity * LeadingFadeMultiplier);
                }

                UpdateSlotEffect(pair.Value, new HighlightSlotState(pair.Key, true, pair.Key == SelectedIndex, scaleBlend, opacity, distance));
            }

            if (SelectedIndex >= 0)
            {
                var selectedSlot = slots[SelectedIndex].transform;
                if (selectedSlot.GetSiblingIndex() != selectedSlot.parent.childCount - 1)
                {
                    selectedSlot.SetAsLastSibling();
                }
            }
        }

        public override void AfterUpdate(RecycleScrollView scrollView)
        {
            if (_lastNotifiedIndex == SelectedIndex)
            {
                return;
            }

            _lastNotifiedIndex = SelectedIndex;
            SelectionChanged?.Invoke(SelectedIndex);
        }

        public override void OnSlotReleased(RecycleScrollView scrollView, ScrollSlot slot, int index)
        {
            UpdateSlotEffect(slot, HighlightSlotState.Reset(index));
        }

        public override void OnDetach(RecycleScrollView scrollView)
        {
            _slotsInLogicalOrder.Clear();

            try
            {
                foreach (var pair in scrollView.ActiveSlots)
                {
                    UpdateSlotEffect(pair.Value, HighlightSlotState.Reset(pair.Key));
                    _slotsInLogicalOrder.Add(pair);
                }

                _slotsInLogicalOrder.Sort(CompareItemIndices);
                for (int order = 0; order < _slotsInLogicalOrder.Count; order++)
                {
                    _slotsInLogicalOrder[order].Value.transform.SetSiblingIndex(order);
                }
            }
            finally
            {
                _slotsInLogicalOrder.Clear();
                SelectedIndex = -1;
                _lastNotifiedIndex = -1;
            }
        }


        protected virtual void UpdateSlotEffect(ScrollSlot slot, HighlightSlotState state)
        {
            _updateSlotEffect?.Invoke(slot, state);
        }

        private float GetAlignmentDistance(ScrollModeGeometry geometry, int index, Vector2 size)
        {
            float alignmentOffset = geometry.GetAlignmentOffset(index, size, Alignment);
            float minimumOffset = 0;
            if (_useLegacyScrollLimits)
            {
                minimumOffset = MinimumScrollOffset;
            }

            return alignmentOffset + minimumOffset;
        }

        private static int CompareItemIndices(KeyValuePair<int, ScrollSlot> left, KeyValuePair<int, ScrollSlot> right)
        {
            return left.Key.CompareTo(right.Key);
        }

        private void SetSelection(int index)
        {
            SelectedIndex = index;
        }
    }
}
