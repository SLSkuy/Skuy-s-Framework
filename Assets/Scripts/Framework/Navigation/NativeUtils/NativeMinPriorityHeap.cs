using System;
using Unity.Collections;

namespace Framework
{
    public struct NativeMinPriorityHeap<T> : IDisposable where T : unmanaged
    {
        private NativeArray<T> _keys;
        private NativeArray<float> _values;
        private readonly Allocator _allocator;
        private int _capacity;

        public int Count;
        public bool IsCreated => _keys.IsCreated && _values.IsCreated;
        public float PeekValue => Count > 0 ? _values[1] : throw new InvalidOperationException("Heap is empty.");
        private T PeekKey => Count > 0 ? _keys[1] : throw new InvalidOperationException("Heap is empty.");

        public NativeMinPriorityHeap(int startCapacity, Allocator allocator)
        {
            if (startCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startCapacity));
            }

            Count = 0;
            _capacity = startCapacity;
            _allocator = allocator;
            _keys = new NativeArray<T>(startCapacity + 1, allocator);
            _values = new NativeArray<float>(startCapacity + 1, allocator);
        }

        public void Push(T key, float value)
        {
            EnsureCapacityForPush();

            Count++;
            _keys[Count] = key;
            _values[Count] = value;
            BubbleUp(Count);
        }

        public T Pop()
        {
            T result = PeekKey;
            _keys[1] = _keys[Count];
            _values[1] = _values[Count];
            Count--;

            if (Count > 0)
            {
                BubbleDown(1);
            }

            return result;
        }

        public void Dispose()
        {
            if (_values.IsCreated)
            {
                _values.Dispose();
            }

            if (_keys.IsCreated)
            {
                _keys.Dispose();
            }

            Count = 0;
            _capacity = 0;
        }

        private void EnsureCapacityForPush()
        {
            if (Count < _capacity)
            {
                return;
            }

            Resize(_capacity > 0 ? _capacity * 2 : 1);
        }

        private void Resize(int newSize)
        {
            if (newSize < Count)
            {
                throw new ArgumentOutOfRangeException(nameof(newSize), "New capacity cannot be smaller than Count.");
            }

            int oldCount = Count;
            NativeArray<T> newKeys = new NativeArray<T>(newSize + 1, _allocator);
            NativeArray<float> newValues = new NativeArray<float>(newSize + 1, _allocator);

            if (oldCount > 0)
            {
                NativeArray<T>.Copy(_keys, 1, newKeys, 1, oldCount);
                NativeArray<float>.Copy(_values, 1, newValues, 1, oldCount);
            }

            Dispose();

            _keys = newKeys;
            _values = newValues;
            Count = oldCount;
            _capacity = newSize;
        }

        private void BubbleUp(int index)
        {
            int parent = HeapUtils.Parent(index);
            while (parent > 0 && _values[parent] > _values[index])
            {
                Swap(parent, index);
                index = parent;
                parent = HeapUtils.Parent(index);
            }
        }

        private void BubbleDown(int index)
        {
            while (true)
            {
                int left = HeapUtils.Left(index);
                int right = HeapUtils.Right(index);
                int smallest = index;

                if (left <= Count && _values[left] < _values[smallest])
                {
                    smallest = left;
                }

                if (right <= Count && _values[right] < _values[smallest])
                {
                    smallest = right;
                }

                if (smallest == index)
                {
                    return;
                }

                Swap(index, smallest);
                index = smallest;
            }
        }

        private void Swap(int indexA, int indexB)
        {
            (_keys[indexA], _keys[indexB]) = (_keys[indexB], _keys[indexA]);
            (_values[indexA], _values[indexB]) = (_values[indexB], _values[indexA]);
        }
    }
}
