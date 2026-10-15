using System;
using UnityEngine;

namespace RecycleScrollView
{
    [RequireComponent(typeof(RectTransform))]
    public class ScrollSlot : MonoBehaviour
    {
        private RectTransform _rectTransform;

#region Property
        internal Action<ScrollSlot> ReturnToPool { get; set; }
        public GameObject CacheGameObject => gameObject;
        public Transform CacheTransform => transform;

        public RectTransform CacheRectTransform
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

        public Vector2 RectSize
        {
            get
            {
                return CacheRectTransform.rect.size;
            }

            set
            {
                CacheRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, value.x);
                CacheRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, value.y);
            }
        }
#endregion

        public virtual void DisposeObject()
        {
            ReturnToPool?.Invoke(this);
        }

        public virtual void Show()
        {
            gameObject.SetActive(true);
        }

        public virtual void Hide()
        {
            gameObject.SetActive(false);
        }

        public virtual void OnAcquired()
        {
        }

        public virtual void OnReleased()
        {
        }
    }
}
