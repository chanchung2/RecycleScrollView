using System;
using System.Collections.Generic;
using UnityEngine;

namespace RecycleScrollView
{
    public sealed class PoolData
    {
        private const int DefaultRetainedCount = 20;

        private readonly Transform _parent;
        private readonly GameObject _prefab;
        private Vector2 _defaultSize;

        private readonly Stack<ScrollSlot> _available = new Stack<ScrollSlot>();
        private readonly HashSet<ScrollSlot> _pooled = new HashSet<ScrollSlot>();
        private readonly HashSet<ScrollSlot> _created = new HashSet<ScrollSlot>();

#region Property
        public int AvailableCount => _available.Count;
        public Vector2 DefaultSize => _defaultSize;
#endregion

        public PoolData(Transform parent, GameObject prefab, int count)
        {
            if (prefab == null || prefab.GetComponent<ScrollSlot>() == null)
            {
                throw new ArgumentException("The prefab must contain a ScrollSlot.", nameof(prefab));
            }

            _parent = parent;
            _prefab = prefab;
            _defaultSize = prefab.GetComponent<ScrollSlot>().RectSize;

            for (int i = 0; i < count; i++)
            {
                Push(CreateSlot());
            }
        }

        public PoolData(Transform parent, string path, int count) : this(parent, Resources.Load<GameObject>(path), count)
        {
        }

        public ScrollSlot Pop(Transform parent)
        {
            ScrollSlot slot = null;
            while (_available.Count > 0 && slot == null)
            {
                slot = _available.Pop();
                _pooled.Remove(slot);
            }

            if (slot == null)
            {
                slot = CreateSlot();
            }

            slot.CacheTransform.SetParent(parent, false);
            slot.CacheTransform.localPosition = Vector3.zero;
            slot.CacheTransform.localRotation = Quaternion.identity;
            slot.CacheTransform.localScale = Vector3.one;
            slot.RectSize = _defaultSize;

            slot.OnAcquired();
            return slot;
        }

        public T Pop<T>(Transform parent) where T : ScrollSlot
        {
            return Pop(parent) as T;
        }

        public void Push(ScrollSlot slot)
        {
            if (slot == null || !_created.Contains(slot) || !_pooled.Add(slot))
            {
                return;
            }

            slot.OnReleased();
            slot.Hide();
            slot.CacheTransform.SetParent(_parent, false);

            _available.Push(slot);
        }

        public void InitRectSize(Vector2 size)
        {
            if (size.x < 0 || size.y < 0 ||
                float.IsNaN(size.x) || float.IsNaN(size.y) ||
                float.IsInfinity(size.x) || float.IsInfinity(size.y))
            {
                throw new ArgumentException("Slot sizes must be finite and non-negative.", nameof(size));
            }

            _defaultSize = size;
            foreach (var slot in _available)
            {
                if (slot != null)
                {
                    slot.RectSize = size;
                }
            }
        }

        public void TrimTo(int count = DefaultRetainedCount)
        {
            count = Math.Max(0, count);
            while (_available.Count > count)
            {
                DestroySlot(_available.Pop());
            }
        }

        public void Clear()
        {
            foreach (var slot in _created)
            {
                if (slot == null)
                {
                    continue;
                }

                slot.ReturnToPool = null;
                DestroyObject(slot.gameObject);
            }

            _created.Clear();
            _available.Clear();
            _pooled.Clear();
        }

        private ScrollSlot CreateSlot()
        {
            var instance = UnityEngine.Object.Instantiate(_prefab, _parent);

            var slot = instance.GetComponent<ScrollSlot>();
            slot.RectSize = _defaultSize;
            slot.ReturnToPool = Push;

            _created.Add(slot);
            return slot;
        }

        private void DestroySlot(ScrollSlot slot)
        {
            _pooled.Remove(slot);
            _created.Remove(slot);
            if (slot == null)
            {
                return;
            }

            slot.ReturnToPool = null;
            DestroyObject(slot.gameObject);
        }

        private static void DestroyObject(GameObject target)
        {
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(target);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }
}
