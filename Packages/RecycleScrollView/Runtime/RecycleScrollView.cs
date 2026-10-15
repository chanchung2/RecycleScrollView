using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RecycleScrollView
{
    [AddComponentMenu("UI (Canvas)/Recycle Scroll View")]
    public class RecycleScrollView : UnityEngine.UI.ScrollRect
    {
        private const int ViewportCornerCount = 4;
        private const int NoExtraLines = 0;
        private const int InitialPoolSize = 0;
        private const float LeadingAlignment = 0;
        private const float CenterPivot = .5f;
        private const float PositionToleranceSquared = .0001f;

        private ScrollLayoutGroup _layoutGroup;
        private RectTransform _rectTransform;

        private PoolData _pool;
        private PoolData _ownedPool;
        private GameObject _ownedPrefab;
        private Transform _poolRoot;

        private ScrollMode _scrollMode;
        private Coroutine _scrollCoroutine;

        private int _animationVersion;
        private bool _scrollable = true;
        private bool _layoutDirty = true;
        private bool _updating;
        private int _layoutVersion = -1;
        private int _layoutRevision;
        private int _arrangedRevision = -1;
        private int _firstIndex = -1;
        private int _endIndex = -1;
        private Vector2 _lastViewportSize;

        private Action<ScrollSlot, int> _bindSlot;
        private readonly List<Vector2> _sizes = new List<Vector2>();
        private readonly Dictionary<int, ScrollSlot> _slots = new Dictionary<int, ScrollSlot>();
        private readonly List<int> _removeIndices = new List<int>();
        private readonly Vector3[] _viewportCorners = new Vector3[ViewportCornerCount];
        private ReadOnlyDictionary<int, ScrollSlot> _readOnlySlots;

        private const int BufferLines = 1;

#region Property
        public int DataCount => _sizes.Count;
        public ScrollMode CurrentMode => _scrollMode;
        public bool IsAnimating => _scrollCoroutine != null;
        public bool IsScrollState => IsAnimating || velocity.sqrMagnitude > 0;

        public ScrollLayoutGroup LayoutGroup
        {
            get
            {
                if (_layoutGroup == null)
                {
                    _layoutGroup = content.GetComponent<ScrollLayoutGroup>();
                }

                return _layoutGroup;
            }
        }

        public RectTransform RectTransform
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

        public IReadOnlyDictionary<int, ScrollSlot> ActiveSlots
        {
            get
            {
                if (_readOnlySlots == null)
                {
                    _readOnlySlots = new ReadOnlyDictionary<int, ScrollSlot>(_slots);
                }

                return _readOnlySlots;
            }
        }

        public int FrontIndex
        {
            get
            {
                EnsureLayout();

                GetViewportInterval(out float start, out float end);
                LayoutGroup.GetVisibleRange(start, end, NoExtraLines, out int first, out _);

                return first;
            }
        }
#endregion

        protected override void Start()
        {
            base.Start();
            onValueChanged.AddListener(OnValueChanged);
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();

            if (_pool == null)
            {
                return;
            }

            var view = viewport;
            if (view == null)
            {
                view = RectTransform;
            }

            Vector2 size = view.rect.size;
            if (_layoutDirty || LayoutGroup.Version != _layoutVersion || _arrangedRevision != _layoutRevision || size != _lastViewportSize)
            {
                UpdateSlot();
            }

            _lastViewportSize = size;
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            if (!_scrollable)
            {
                return;
            }

            CancelAnimation();
            base.OnBeginDrag(eventData);
        }

        public override void OnDrag(PointerEventData eventData)
        {
            if (_scrollable)
            {
                base.OnDrag(eventData);
            }
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            base.OnEndDrag(eventData);
            _scrollMode?.OnEndDrag(eventData, this);
        }

        public override void OnScroll(PointerEventData eventData)
        {
            if (_scrollable)
            {
                CancelAnimation();
                base.OnScroll(eventData);
            }
        }

        public void SetScrollMode(ScrollMode mode)
        {
            ThrowIfUpdating();
            mode?.ValidateOwner(this);

            CancelAnimation();
            StopMovement();
            if (ReferenceEquals(_scrollMode, mode))
            {
                return;
            }

            var previousMode = _scrollMode;
            _scrollMode = null;
            _updating = true;

            try
            {
                previousMode?.Detach(this);
            }
            finally
            {
                _updating = false;
            }

            _scrollMode = mode;
            mode?.Attach(this);
            mode?.OnDataChanged(this);

            UpdateSlot();
        }

        public void UpdateScrollViewWithSizes(GameObject slotPrefab, IReadOnlyList<Vector2> sizes, Action<ScrollSlot, int> bindSlot)
        {
            ThrowIfUpdating();

            var pool = _ownedPool;
            if (pool == null || _ownedPrefab != slotPrefab)
            {
                if (_poolRoot == null)
                {
                    _poolRoot = new GameObject("Recycled Slots").transform;
                    _poolRoot.SetParent(transform, false);
                }

                pool = new PoolData(_poolRoot, slotPrefab, InitialPoolSize);
            }

            DisposeObject();
            if (pool != _ownedPool)
            {
                _ownedPool?.Clear();
                _ownedPool = pool;
                _ownedPrefab = slotPrefab;
            }

            _pool = _ownedPool;
            Reload(sizes, bindSlot);
        }

        public void UpdateScrollView(GameObject slotPrefab, int dataCount, Action<ScrollSlot, int> bindSlot)
        {
            ThrowIfUpdating();
            if (slotPrefab == null || slotPrefab.GetComponent<ScrollSlot>() == null)
            {
                throw new ArgumentException("The prefab must contain a ScrollSlot.", nameof(slotPrefab));
            }

            Vector2 size = slotPrefab.GetComponent<ScrollSlot>().RectSize;
            if (_ownedPool != null && _ownedPrefab == slotPrefab)
            {
                size = _ownedPool.DefaultSize;
            }

            var sizes = new Vector2[dataCount];
            for (int i = 0; i < sizes.Length; i++)
            {
                sizes[i] = size;
            }

            UpdateScrollViewWithSizes(slotPrefab, sizes, bindSlot);
        }

        public void UpdateScrollView(GameObject slotPrefab, Action<ScrollSlot, int> bindSlot)
        {
            UpdateScrollViewWithSizes(slotPrefab, _sizes.ToArray(), bindSlot);
        }

        public void SetSlotSize(int index, Vector2 size)
        {
            ValidateSize(size);
            if (_sizes[index] == size)
            {
                return;
            }

            _sizes[index] = size;
            _layoutDirty = true;

            UpdateSlot();
        }

        public Vector2 GetSlotSize(int index)
        {
            return _sizes[index];
        }

        public Vector2 GetSlotPos(int index)
        {
            EnsureLayout();
            return LayoutGroup.GetSlotPos(index);
        }

        public void AddSlot(Vector2 size)
        {
            if (_pool == null)
            {
                throw new InvalidOperationException("_pool is null");
            }

            ValidateSize(size);
            _sizes.Add(size);
            _layoutDirty = true;

            UpdateSlot();
        }

        public void ResizeObject(Vector2 size)
        {
            if (_pool == null)
            {
                throw new InvalidOperationException("_pool is null");
            }

            _pool.InitRectSize(size);
            for (int i = 0; i < DataCount; i++)
            {
                _sizes[i] = size;
            }

            _layoutDirty = true;
            UpdateSlot();
        }

        public void RefreshVisibleSlots()
        {
            foreach (var pair in _slots)
            {
                _bindSlot?.Invoke(pair.Value, pair.Key);
            }
        }

        public void UpdateSlot()
        {
            if (_pool == null || _updating)
            {
                return;
            }

            var mode = _scrollMode;
            _updating = true;

            try
            {
                mode?.BeforeUpdate(this);

                EnsureLayout();
                int revision = _layoutRevision;
                bool changed = _arrangedRevision != revision;

                GetViewportInterval(out float start, out float end);
                LayoutGroup.GetVisibleRange(start, end, BufferLines, out int first, out int endExclusive);

                if (changed || first != _firstIndex || endExclusive != _endIndex)
                {
                    _firstIndex = -1;
                    _endIndex = -1;
                    _arrangedRevision = -1;

                    _removeIndices.Clear();
                    foreach (var pair in _slots)
                    {
                        if (pair.Key < first || pair.Key >= endExclusive)
                        {
                            _removeIndices.Add(pair.Key);
                        }
                    }

                    foreach (int index in _removeIndices)
                    {
                        mode?.OnSlotReleased(this, _slots[index], index);
                        _slots[index].DisposeObject();
                        _slots.Remove(index);
                    }

                    for (int index = first; index < endExclusive; index++)
                    {
                        if (_slots.ContainsKey(index))
                        {
                            continue;
                        }

                        var slot = _pool.Pop(content);
                        slot.RectSize = _sizes[index];
                        try
                        {
                            _bindSlot?.Invoke(slot, index);
                            _slots.Add(index, slot);
                            slot.Show();
                        }
                        catch
                        {
                            _slots.Remove(index);
                            slot.DisposeObject();
                            throw;
                        }
                    }

                    LayoutGroup.Arrange(ActiveSlots, _sizes);
                    _firstIndex = first;
                    _endIndex = endExclusive;
                    _arrangedRevision = revision;
                }

                mode?.UpdateMode(this);
            }
            finally
            {
                _updating = false;
            }

            if (ReferenceEquals(_scrollMode, mode))
            {
                mode?.AfterUpdate(this);
            }
        }

        public void DisposeObject()
        {
            CancelAnimation();

            foreach (var pair in _slots)
            {
                if (pair.Value != null)
                {
                    _scrollMode?.OnSlotReleased(this, pair.Value, pair.Key);
                    pair.Value.DisposeObject();
                }
            }

            _slots.Clear();
            _firstIndex = -1;
            _endIndex = -1;
        }

        public void ImmediateToSlotIndex(int index)
        {
            CancelAnimation();
            StopMovement();
            content.anchoredPosition = GetTargetPosition(index);

            OnValueChanged(Vector2.zero);
        }

        public void ScrollToSlotIndex(int index)
        {
            Vector2 target = GetTargetPosition(index);

            CancelAnimation();
            StopMovement();
            _scrollCoroutine = StartCoroutine(AnimateTo(target, _animationVersion));
        }

        public void StopDrag()
        {
            _scrollable = false;
            CancelAnimation();
            StopMovement();
            base.OnEndDrag(new PointerEventData(EventSystem.current));
        }

        public void EnableDrag()
        {
            _scrollable = true;
        }

        public void SetNormalizedPositionY(float value)
        {
            EnsureLayout();
            verticalNormalizedPosition = 1 - Mathf.Clamp01(value);
            OnValueChanged(Vector2.zero);
        }

        private void Reload(IReadOnlyList<Vector2> sizes, Action<ScrollSlot, int> bindSlot)
        {
            int count = sizes.Count;
            EnsureLayout(sizes);

            _sizes.Clear();
            for (int i = 0; i < count; i++)
            {
                _sizes.Add(sizes[i]);
            }

            _bindSlot = bindSlot;
            _firstIndex = -1;
            _endIndex = -1;
            _scrollMode?.OnDataChanged(this);

            Canvas.ForceUpdateCanvases();
            OnValueChanged(Vector2.zero);
        }

        private bool EnsureLayout(IReadOnlyList<Vector2> sizes = null)
        {
            if (sizes == null && !_layoutDirty && _layoutVersion == LayoutGroup.Version)
            {
                return false;
            }

            LayoutGroup.Rebuild(sizes ?? _sizes);

            bool isVertical = LayoutGroup.IsVertical;
            bool directionChanged = horizontal != !isVertical || vertical != isVertical;
            horizontal = !isVertical;
            vertical = isVertical;
            if (directionChanged && isActiveAndEnabled)
            {
                horizontalScrollbar = horizontalScrollbar;
                verticalScrollbar = verticalScrollbar;
            }

            _layoutVersion = LayoutGroup.Version;
            _layoutDirty = false;
            _layoutRevision++;

            return true;
        }

        private void GetViewportInterval(out float start, out float end)
        {
            var view = viewport;
            if (view == null)
            {
                view = RectTransform;
            }

            view.GetWorldCorners(_viewportCorners);

            start = float.PositiveInfinity;
            end = float.NegativeInfinity;
            Rect bounds = content.rect;
            Vector2 pivot = LayoutGroup.PivotPosition;
            foreach (var corner in _viewportCorners)
            {
                Vector3 local = content.InverseTransformPoint(corner);
                float coordinate;
                if (LayoutGroup.IsVertical)
                {
                    if (pivot.y >= CenterPivot)
                    {
                        coordinate = bounds.yMax - local.y;
                    }
                    else
                    {
                        coordinate = local.y - bounds.yMin;
                    }
                }
                else
                {
                    if (pivot.x <= CenterPivot)
                    {
                        coordinate = local.x - bounds.xMin;
                    }
                    else
                    {
                        coordinate = bounds.xMax - local.x;
                    }
                }

                start = Mathf.Min(start, coordinate);
                end = Mathf.Max(end, coordinate);
            }
        }


        public float GetSlotAlignmentOffset(int index, float alignment)
        {
            float leading = GetSlotLeadingOffset(index);
            GetViewportInterval(out float start, out float end);

            float size;
            if (LayoutGroup.IsVertical)
            {
                size = _sizes[index].y;
            }
            else
            {
                size = _sizes[index].x;
            }

            return leading + size * alignment - (start + (end - start) * alignment);
        }

        private float GetSlotLeadingOffset(int index)
        {
            EnsureLayout();

            Vector2 position = LayoutGroup.GetSlotPos(index);
            Vector2 pivot = LayoutGroup.PivotPosition;
            Vector2 size = _sizes[index];
            Rect bounds = content.rect;
            if (LayoutGroup.IsVertical)
            {
                if (pivot.y >= CenterPivot)
                {
                    return bounds.yMax - position.y - size.y * (1 - pivot.y);
                }

                return position.y - bounds.yMin - size.y * pivot.y;
            }

            if (pivot.x <= CenterPivot)
            {
                return position.x - bounds.xMin - size.x * pivot.x;
            }

            return bounds.xMax - position.x - size.x * (1 - pivot.x);
        }

        private Vector2 GetTargetPosition(int index)
        {
            float alignment = _scrollMode?.GetAlignment(this) ?? LeadingAlignment;
            ValidateAlignment(alignment);

            float leading = GetSlotLeadingOffset(index);
            GetViewportInterval(out float start, out float end);

            float slotSize;
            if (LayoutGroup.IsVertical)
            {
                slotSize = _sizes[index].y;
            }
            else
            {
                slotSize = _sizes[index].x;
            }

            leading -= (end - start - slotSize) * alignment;

            float length;
            if (LayoutGroup.IsVertical)
            {
                length = LayoutGroup.ContentSize.y;
            }
            else
            {
                length = LayoutGroup.ContentSize.x;
            }

            float targetOffset = Mathf.Clamp(leading, 0, Mathf.Max(0, length - (end - start)));
            Vector2 pivot = LayoutGroup.PivotPosition;
            Vector3 displacement;
            if (LayoutGroup.IsVertical)
            {
                int direction = -1;
                if (pivot.y >= CenterPivot)
                {
                    direction = 1;
                }

                displacement = new Vector3(0, direction * (targetOffset - start), 0);
            }
            else
            {
                int direction = 1;
                if (pivot.x <= CenterPivot)
                {
                    direction = -1;
                }

                displacement = new Vector3(direction * (targetOffset - start), 0, 0);
            }

            Vector3 parentDisplacement = content.localRotation * Vector3.Scale(displacement, content.localScale);
            return content.anchoredPosition + new Vector2(parentDisplacement.x, parentDisplacement.y);
        }

        private static void ValidateAlignment(float alignment)
        {
            if (float.IsNaN(alignment) || float.IsInfinity(alignment))
            {
                throw new ArgumentException("Alignment must be finite.", nameof(alignment));
            }
        }

        private void ThrowIfUpdating()
        {
            if (_updating)
            {
                throw new InvalidOperationException("Do not replace the data source from a slot-binding callback. Use SetSlotSize for measured size changes.");
            }
        }

        private void OnValueChanged(Vector2 value)
        {
            if (_pool == null)
            {
                return;
            }

            UpdateSlot();
        }

        private IEnumerator AnimateTo(Vector2 target, int version)
        {
            Vector2 origin = content.anchoredPosition;
            float elapsed = 0f;
            const float Duration = .3f;
            Vector2 expectedPosition = origin;

            try
            {
                yield return null;
                while (elapsed < Duration)
                {
                    if ((content.anchoredPosition - expectedPosition).sqrMagnitude > PositionToleranceSquared)
                    {
                        yield break;
                    }

                    elapsed += Time.unscaledDeltaTime;
                    content.anchoredPosition = Vector2.Lerp(origin, target, Mathf.Clamp01(elapsed / Duration));
                    expectedPosition = content.anchoredPosition;

                    OnValueChanged(Vector2.zero);
                    if (_animationVersion != version)
                    {
                        yield break;
                    }

                    if (elapsed < Duration)
                    {
                        yield return null;
                    }
                }
            }
            finally
            {
                if (_animationVersion == version)
                {
                    _scrollCoroutine = null;
                }
            }
        }

        private void CancelAnimation()
        {
            _animationVersion++;
            if (_scrollCoroutine != null)
            {
                StopCoroutine(_scrollCoroutine);
            }

            _scrollCoroutine = null;
        }

        private static void ValidateSize(Vector2 size)
        {
            if (size.x < 0 || size.y < 0 || float.IsNaN(size.x) || float.IsNaN(size.y) || float.IsInfinity(size.x) || float.IsInfinity(size.y))
            {
                throw new ArgumentException("Slot sizes must be finite and non-negative.", nameof(size));
            }
        }

        protected override void OnDisable()
        {
            CancelAnimation();
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            var mode = _scrollMode;
            _scrollMode = null;

            try
            {
                mode?.Detach(this);
            }
            finally
            {
                onValueChanged.RemoveListener(OnValueChanged);
                DisposeObject();
                _ownedPool?.Clear();
                base.OnDestroy();
            }
        }
    }
}
