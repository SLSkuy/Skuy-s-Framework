using System;
using Unity.Collections;

namespace Framework
{
	public struct MinMaxHeap<T> : IDisposable where T : unmanaged
	{
		private NativeArray<T> _keys;
		private NativeArray<float> _values;
		private readonly Allocator _allocator;
		private int _capacity;

		public int Count;
		public bool IsFull => Count == _capacity;
		public bool IsCreated => _keys.IsCreated && _values.IsCreated;
		public float HeadValue => Count > 0 ? _values[1] : throw new InvalidOperationException("Heap is empty.");
		T HeadKey => Count > 0 ? _keys[1] : throw new InvalidOperationException("Heap is empty.");

		public MinMaxHeap(int startCapacity, Allocator allocator)
		{
			if (startCapacity < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(startCapacity));
			}

			Count = 0;
			_capacity = startCapacity;
			_allocator = allocator;

			// Index 0 is intentionally unused, so parent/child math can stay simple.
			_keys = new NativeArray<T>(startCapacity + 1, allocator);
			_values = new NativeArray<float>(startCapacity + 1, allocator);
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

		public void Resize(int newSize)
		{
			if (newSize < Count)
			{
				throw new ArgumentOutOfRangeException(nameof(newSize), "New capacity cannot be smaller than Count.");
			}

			int oldCount = Count;
			var newKeys = new NativeArray<T>(newSize + 1, _allocator);
			var newValues = new NativeArray<float>(newSize + 1, _allocator);

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

		void Swap(int indexA, int indexB)
		{
			(_values[indexA], _values[indexB]) = (_values[indexB], _values[indexA]);
			(_keys[indexA], _keys[indexB]) = (_keys[indexB], _keys[indexA]);
		}

		void BubbleDownMax(int index)
		{
			while (true)
			{
				int left = HeapUtils.Left(index);
				int right = HeapUtils.Right(index);
				int largest = index;

				if (left <= Count && _values[left] > _values[largest])
				{
					largest = left;
				}

				if (right <= Count && _values[right] > _values[largest])
				{
					largest = right;
				}

				if (largest == index)
				{
					return;
				}

				Swap(index, largest);
				index = largest;
			}
		}

		void BubbleDownMin(int index)
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

		void BubbleUpMax(int index)
		{
			int parent = HeapUtils.Parent(index);

			while (parent > 0 && _values[parent] < _values[index])
			{
				Swap(parent, index);
				index = parent;
				parent = HeapUtils.Parent(index);
			}
		}

		void BubbleUpMin(int index)
		{
			int parent = HeapUtils.Parent(index);

			while (parent > 0 && _values[parent] > _values[index])
			{
				Swap(parent, index);
				index = parent;
				parent = HeapUtils.Parent(index);
			}
		}

		/// <summary>
		/// Maintains the smallest values when capacity is reached.
		/// </summary>
		public void PushObjMax(T key, float val)
		{
			if (Count == _capacity)
			{
				if (Count > 0 && HeadValue > val)
				{
					_values[1] = val;
					_keys[1] = key;
					BubbleDownMax(1);
				}

				return;
			}

			Count++;
			_values[Count] = val;
			_keys[Count] = key;
			BubbleUpMax(Count);
		}

		/// <summary>
		/// Maintains the largest values when capacity is reached.
		/// </summary>
		public void PushObjMin(T key, float val)
		{
			if (Count == _capacity)
			{
				if (Count > 0 && HeadValue < val)
				{
					_values[1] = val;
					_keys[1] = key;
					BubbleDownMin(1);
				}

				return;
			}

			Count++;
			_values[Count] = val;
			_keys[Count] = key;
			BubbleUpMin(Count);
		}

		T PopHeadObj()
		{
			if (Count == 0)
			{
				throw new InvalidOperationException("Heap is empty.");
			}

			T result = HeadKey;
			_keys[1] = _keys[Count];
			_values[1] = _values[Count];
			Count--;
			return result;
		}

		public T PopObjMax()
		{
			T result = PopHeadObj();
			if (Count > 0)
			{
				BubbleDownMax(1);
			}

			return result;
		}

		public T PopObjMin()
		{
			T result = PopHeadObj();
			if (Count > 0)
			{
				BubbleDownMin(1);
			}

			return result;
		}
	}
}
