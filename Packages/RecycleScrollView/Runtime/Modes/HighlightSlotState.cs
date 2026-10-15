namespace RecycleScrollView
{

    public readonly struct HighlightSlotState
    {
        #region Property

        public int ItemIndex { get; }
        public bool IsActive { get; }
        public bool IsSelected { get; }
        public float ScaleBlend { get; }
        public float Opacity { get; }
        public float DistanceToAlignment { get; }

        #endregion

        internal HighlightSlotState(int index, bool active, bool selected, float scaleBlend, float opacity, float distance)
        {
            ItemIndex = index;
            IsActive = active;
            IsSelected = selected;
            ScaleBlend = scaleBlend;
            Opacity = opacity;
            DistanceToAlignment = distance;
        }

        internal static HighlightSlotState Reset(int index)
        {
            return new HighlightSlotState(index, false, false, 1, 1, 0);
        }
    }
}
