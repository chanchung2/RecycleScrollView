using System.Collections;
using UnityEngine;
using ScrollView = RecycleScrollView.RecycleScrollView;

namespace RecycleScrollView.Samples
{
    public class CarouselAutoScrollSample : MonoBehaviour
    {
        private const float DefaultInterval = 2;
        private const int MinimumItemCount = 2;

        [SerializeField]
        private ScrollView _scrollView;
        [SerializeField]
        private float _interval = DefaultInterval;

        private Coroutine _autoScrollCoroutine;
        private int _index;

        private void OnEnable()
        {
            _autoScrollCoroutine = StartCoroutine(AutoScroll());
        }

        private void OnDisable()
        {
            if (_autoScrollCoroutine != null)
            {
                StopCoroutine(_autoScrollCoroutine);
                _autoScrollCoroutine = null;
            }
        }

        private IEnumerator AutoScroll()
        {
            var wait = new WaitForSeconds(_interval);
            while (true)
            {
                yield return wait;

                int itemCount = _scrollView.DataCount;
                if (itemCount < MinimumItemCount)
                {
                    _index = 0;
                    continue;
                }

                _index = (_index + 1) % itemCount;
                if (_index == 0)
                {
                    _scrollView.ImmediateToSlotIndex(_index);
                }
                else
                {
                    _scrollView.ScrollToSlotIndex(_index);
                }
            }
        }
    }
}
