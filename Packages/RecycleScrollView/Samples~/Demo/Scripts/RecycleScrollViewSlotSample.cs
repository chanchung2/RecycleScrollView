using TMPro;
using UnityEngine;

namespace RecycleScrollView.Samples
{
    public sealed class RecycleScrollViewSlotSample : ScrollSlot
    {
        private static readonly Color s_highlightColor = new Color(1, .75f, .25f);

        [SerializeField]
        private TextMeshProUGUI _text;

        private UnityEngine.UI.Image _background;
        private CanvasGroup _canvasGroup;
        private Color _normalColor;
        private bool _visualsInitialized;

        public void UpdateUI(int index)
        {
            ResetVisuals();
            _text.SetText("{0}", index);
        }

        public void SetHighlighted(bool selected)
        {
            InitializeVisuals();
            if (_background != null)
            {
                if (selected)
                {
                    _background.color = s_highlightColor;
                }
                else
                {
                    _background.color = _normalColor;
                }
            }
        }

        public void ApplyEffect(HighlightSlotState state, Vector2 scaleRange)
        {
            if (!state.IsActive)
            {
                ResetVisuals();
                return;
            }

            SetHighlighted(state.IsSelected);

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                {
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            _canvasGroup.alpha = state.Opacity;

            float scale = Mathf.Lerp(scaleRange.x, scaleRange.y, state.ScaleBlend);
            CacheTransform.localScale = Vector3.one * scale;
        }

        public void ResetVisuals()
        {
            InitializeVisuals();
            CacheTransform.localScale = Vector3.one;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1;
            }

            if (_background != null)
            {
                _background.color = _normalColor;
            }
        }

        public override void OnReleased()
        {
            ResetVisuals();
        }

        private void InitializeVisuals()
        {
            if (_visualsInitialized)
            {
                return;
            }

            _background = GetComponent<UnityEngine.UI.Image>();
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_background != null)
            {
                _normalColor = _background.color;
            }

            _visualsInitialized = true;
        }
    }
}
