using System;
using UnityEngine.EventSystems;

namespace RecycleScrollView
{

    public abstract class ScrollMode
    {
        internal RecycleScrollView Owner { get; private set; }

        internal void ValidateOwner(RecycleScrollView scrollView)
        {
            if (Owner != null && Owner != scrollView)
            {
                throw new InvalidOperationException("Create a separate scroll mode instance for each view.");
            }
        }

        internal void Attach(RecycleScrollView scrollView)
        {
            Owner = scrollView;
            OnAttach(scrollView);
        }

        internal void Detach(RecycleScrollView scrollView)
        {
            try
            {
                OnDetach(scrollView);
            }
            finally
            {
                Owner = null;
            }
        }

        public virtual void OnAttach(RecycleScrollView scrollView)
        {
        }

        public virtual void OnDetach(RecycleScrollView scrollView)
        {
        }

        public virtual void OnSlotReleased(RecycleScrollView scrollView, ScrollSlot slot, int index)
        {
        }


        public virtual void AfterUpdate(RecycleScrollView scrollView)
        {
        }

        public virtual void OnDataChanged(RecycleScrollView scrollView)
        {
        }

        public virtual void BeforeUpdate(RecycleScrollView scrollView)
        {
        }

        public virtual float GetAlignment(RecycleScrollView scrollView)
        {
            return 0;
        }

        public abstract void UpdateMode(RecycleScrollView scrollView);

        public virtual void OnEndDrag(PointerEventData eventData, RecycleScrollView scrollView)
        {
        }
    }
}
