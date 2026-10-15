namespace RecycleScrollView
{

    public readonly struct CarouselSlotState
    {
        #region Property

        public int ItemIndex { get; }
        public bool IsActive { get; }
        public bool IsSelected { get; }
        public float DistanceToAlignment { get; }

        #endregion

        internal CarouselSlotState(int index, bool active, bool selected, float distance)
        {
            ItemIndex = index;
            IsActive = active;
            IsSelected = selected;
            DistanceToAlignment = distance;
        }

        internal static CarouselSlotState Reset(int index)
        {
            return new CarouselSlotState(index, false, false, 0);
        }
    }
}
