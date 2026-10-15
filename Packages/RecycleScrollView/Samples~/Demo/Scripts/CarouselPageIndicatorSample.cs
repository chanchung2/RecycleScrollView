using UnityEngine;
using UnityEngine.UI;

namespace RecycleScrollView.Samples
{
    public class CarouselPageIndicatorSample : MonoBehaviour
    {
        [SerializeField]
        private Image[] _fills;

        private CarouselScrollMode _mode;

        public void Initialize(CarouselScrollMode mode)
        {
            Unsubscribe();
            _mode = mode;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_mode == null)
            {
                return;
            }

            _mode.SelectionChanged += UpdateIndicator;
            UpdateIndicator(_mode.SelectedIndex);
        }

        private void Unsubscribe()
        {
            if (_mode != null)
            {
                _mode.SelectionChanged -= UpdateIndicator;
            }
        }

        private void UpdateIndicator(int index)
        {
            for (int i = 0; i < _fills.Length; i++)
            {
                _fills[i].enabled = i == index;
            }
        }
    }
}
