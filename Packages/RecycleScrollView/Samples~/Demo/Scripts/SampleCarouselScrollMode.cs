namespace RecycleScrollView.Samples
{
    public class SampleCarouselScrollMode : CarouselScrollMode
    {
        private const float CenterAlignment = .5f;

        public SampleCarouselScrollMode(float alignment = CenterAlignment) : base(alignment: alignment)
        {
        }

        protected override void UpdateSlotEffect(ScrollSlot slot, CarouselSlotState state)
        {
            if (slot is RecycleScrollViewSlotSample sampleSlot)
            {
                if (!state.IsActive)
                {
                    sampleSlot.SetHighlighted(false);
                    return;
                }

                sampleSlot.SetHighlighted(state.IsSelected);
            }
        }
    }
}
