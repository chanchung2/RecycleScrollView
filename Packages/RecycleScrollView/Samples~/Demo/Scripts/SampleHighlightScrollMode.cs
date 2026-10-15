using UnityEngine;

namespace RecycleScrollView.Samples
{
    public class SampleHighlightScrollMode : HighlightScrollMode
    {
        private readonly Vector2 _scaleRange;

        public SampleHighlightScrollMode(float alignment, float effectDistance, float fadeDistance, Vector2 scaleRange)
            : base(alignment: alignment, effectDistance: effectDistance, fadeDistance: fadeDistance)
        {
            _scaleRange = scaleRange;
        }

        protected override void UpdateSlotEffect(ScrollSlot slot, HighlightSlotState state)
        {
            ((RecycleScrollViewSlotSample)slot).ApplyEffect(state, _scaleRange);
        }
    }

    public class SampleAlphaHighlightScrollMode : HighlightScrollMode
    {
        private const float CenterAlignment = .5f;
        private const float DefaultEffectDistance = 200;

        public SampleAlphaHighlightScrollMode(float alignment = CenterAlignment, float effectDistance = DefaultEffectDistance)
            : base(alignment: alignment, effectDistance: effectDistance)
        {
        }

        protected override void UpdateSlotEffect(ScrollSlot slot, HighlightSlotState state)
        {
            var sampleSlot = (RecycleScrollViewSlotSample)slot;
            var group = slot.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = slot.gameObject.AddComponent<CanvasGroup>();
            }

            if (!state.IsActive)
            {
                sampleSlot.ResetVisuals();
                group.alpha = 1;
                return;
            }

            sampleSlot.SetHighlighted(state.IsSelected);
            float alpha = 1;
            if (!state.IsSelected)
            {
                alpha = state.ScaleBlend;
            }

            slot.CacheTransform.localScale = Vector3.one;
            group.alpha = alpha;
        }
    }
}
