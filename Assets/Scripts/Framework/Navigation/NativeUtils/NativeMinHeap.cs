using System;
using Unity.Collections;

namespace Framework
{
    /// <summary>
    /// 非托管堆元素接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface INativeHeapItem<T> : IComparable<T>
    {
        int HeapIndex { get; set; }
    }
    
    public struct NativeMinHeap<T> : IDisposable where T : unmanaged, INativeHeapItem<T>, IEquatable<T>
    {
        private NativeArray<T> _minHeap;

        public void Dispose()
        {
            _minHeap.Dispose();
        }
    }
}