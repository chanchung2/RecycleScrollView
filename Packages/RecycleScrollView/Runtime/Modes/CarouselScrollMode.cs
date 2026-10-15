using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RecycleScrollView
{

    public class CarouselScrollMode : ScrollMode
    {
        private const float CenterAlignment = .5f;

        private readonly Action<int> _onSnapSelected;
        private int _lastNotifiedIndex = -1;

        #region Property

        public int SelectedIndex { get; private set; } = -1;


        public float? Alignment { get; }

        #endregion

        public event Action<int> SelectionChanged;

        public CarouselScrollMode(Action<int> onSnapSelected = null, float? alignment = null)
        {
            if (alignment.HasValue && (float.IsNaN(alignment.Value) || alignment.Value < 0 || alignment.Value > 1))
            {
                throw new ArgumentOutOfRangeException(nameof(alignment));
            }

            _onSnapSelected = onSnapSelected;
            Alignment = alignment;
        }

        public override void OnDataChanged(RecycleScrollView scrollView)
        {
            if (scrollView.DataCount == 0)
            {
                SetSelection(-1);
            }
            else
            {
                int selectedIndex = Mathf.Clamp(SelectedIndex, 0, scrollView.DataCount - 1);
                SetSelection(selectedIndex);
            }
        }

        public override void OnDetach(RecycleScrollView scrollView)
        {
            try
            {
                foreach (var pair in scrollView.ActiveSlots)
                {
                    UpdateSlotEffect(pair.Value, CarouselSlotState.Reset(pair.Key));
                }
            }
            finally
            {
                SelectedIndex = -1;
                _lastNotifiedIndex = -1;
            }
        }

        public override void OnSlotReleased(RecycleScrollView scrollView, ScrollSlot slot, int index)
        {
            UpdateSlotEffect(slot, CarouselSlotState.Reset(index));
        }

        public override void UpdateMode(RecycleScrollView scrollView)
        {
            EvaluateSlots(scrollView, scrollView.ActiveSlots);
        }

        public void EvaluateSlots(RecycleScrollView scrollView, System.Collections.Generic.IReadOnlyDictionary<int, ScrollSlot> slots)
        {
            var geometry = new ScrollModeGeometry(scrollView);
            float alignment = GetAlignment(scrollView);

            int selectedIndex = -1;
            float closestDistance = float.PositiveInfinity;
            foreach (var pair in slots)
            {
                float distance = Mathf.Abs(geometry.GetAlignmentOffset(pair.Key, pair.Value.RectSize, alignment));
                if (distance < closestDistance || Mathf.Approximately(distance, closestDistance) && pair.Key < selectedIndex)
                {
                    closestDistance = distance;
                    selectedIndex = pair.Key;
                }
            }

            SetSelection(selectedIndex);

            foreach (var pair in slots)
            {
                float distance = geometry.GetAlignmentOffset(pair.Key, pair.Value.RectSize, alignment);
                UpdateSlotEffect(pair.Value, new CarouselSlotState(pair.Key, true, pair.Key == SelectedIndex, distance));
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

        public override void OnEndDrag(PointerEventData eventData, RecycleScrollView scrollView)
        {
            SnapToNearest(scrollView);
        }

        public virtual void SnapToNearest(RecycleScrollView scrollView)
        {
            scrollView.UpdateSlot();
            if (scrollView.CurrentMode != this || scrollView.DataCount == 0 || SelectedIndex < 0)
            {
                return;
            }

            int selectedIndex = SelectedIndex;
            scrollView.ScrollToSlotIndex(selectedIndex);
            _onSnapSelected?.Invoke(selectedIndex);
        }


        protected virtual void UpdateSlotEffect(ScrollSlot slot, CarouselSlotState state)
        {
        }

        public override float GetAlignment(RecycleScrollView scrollView)
        {
            if (Alignment.HasValue)
            {
                return Alignment.Value;
            }

            var pivot = scrollView.LayoutGroup.PivotPosition;
            float pivotValue = pivot.x;
            if (scrollView.LayoutGroup.IsVertical)
            {
                pivotValue = pivot.y;
            }

            if (Mathf.Approximately(pivotValue, CenterAlignment))
            {
                return CenterAlignment;
            }

            return 0;
        }

        private void SetSelection(int index)
        {
            SelectedIndex = index;
        }
    }
}
