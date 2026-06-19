using System;
using Unity.Collections;
using Unity.Mathematics;

namespace Framework
{
    /// <summary>
    /// KD-Tree定义
    /// </summary>
    public struct KDTree : IDisposable
    {
        /// <summary>
        /// 原始点数据
        /// </summary>
        private NativeArray<float3> _points;
        
        /// <summary>
        /// 节点缓存
        /// </summary>
        private NativeList<KDTreeNode> _nodes;
        
        /// <summary>
        /// 每个节点在_nodes中的索引
        /// </summary>
        private NativeArray<int> _indices;

        /// <summary>
        /// 排列数组，长度与_points相同
        /// 标识当前第i个点在_points中的索引
        /// </summary>
        private NativeArray<int> _permutation;
        
        private NativeQueue<int> _buildQueue;

        /// <summary>
        /// 每个叶子节点最多包含的点数量
        /// </summary>
        private const int MAX_POINTS_PER_LEAF_NODE = 64;

        public KDTree(NativeArray<float3> points, bool buildNow, Allocator allocator)
        {
            // 预估节点数量
            int nodeCount = 4 * (int)math.ceil(points.Length / (float)MAX_POINTS_PER_LEAF_NODE + 1) + 1;
            _points = points;
            
            _nodes = new NativeList<KDTreeNode>(nodeCount, allocator);
            _permutation = new NativeArray<int>(points.Length, allocator);
            _indices = new NativeArray<int>(1, allocator);
            _indices[0] = -1;
            _buildQueue = new NativeQueue<int>(allocator);
            
            if(buildNow) Rebuild();
        }
        
        /// <summary>
        /// 重建KD-Tree
        /// </summary>
        public void Rebuild()
        {
            _nodes.Clear();

            // 初始化排列数组：每个节点初始化时的“逻辑顺序”等于其原始索引
            for (int i = 0; i < _permutation.Length; i++)
            {
                _permutation[i] = i;
            }

            int rootNode = GetTreeNodeIndex(MakeBound(), 0, _points.Length);
            _indices[0] = rootNode;
            _buildQueue.Enqueue(rootNode);

            while (_buildQueue.Count > 0)
            {
                int index =  _buildQueue.Dequeue();
            }
        }

        /// <summary>
        /// 创建一个新的树节点并返回其索引
        /// </summary>
        /// <param name="bound">节点包围盒</param>
        /// <param name="start">分割区间起点</param>
        /// <param name="end">分割区间终点</param>
        private int GetTreeNodeIndex(KDTreeBound bound, int start, int end)
        {
            _nodes.Add(new KDTreeNode()
            {
                Bound = bound,
                Boundary = new int2(start, end),
                PartitionAxis = KDTreePartitionAxis.Leaf,
                PartitionCoordinate = 0,
                NegativeChildIndex = -1,
                PositiveChildIndex = -1
            });

            return _nodes.Length - 1;
        }

        private KDTreeBound MakeBound()
        {
            float3 min = new float3(float.MaxValue);
            float3 max = new float3(float.MinValue);

            foreach (var p in _points)
            {
                min = math.min(min, p);
                max = math.max(max, p);
            }
 
            return new KDTreeBound
            {
                Min = min,
                Max = max
            };
        }

        private void SplitNode(int parentIndex, out int posIndex, out int negIndex)
        {
            KDTreeNode parent = _nodes[parentIndex];
            KDTreeBound bound = parent.Bound;
            
            // 计算分割轴
            KDTreePartitionAxis splitAxis = parent.GetPartitionAxis();

            // 计算坐标边界值
            float boundStart = bound.Min[(int)splitAxis];
            float boundEnd = bound.Max[(int)splitAxis];

            // 计算分割坐标轴的位置
            float splitPivot = CalculatePivot(parent.Boundary.x, parent.Boundary.y, boundStart, boundEnd, splitAxis);
            // 分割坐标轴对应的节点索引
            // [splitAxisIndex, parent.End) 中的点坐标 >= splitPivot
            // [parent.Start, splitAxisIndex) 中的点坐标 < splitPivot
            int splitAxisIndex = GetPartition(parent.Boundary.x, parent.Boundary.y,splitPivot, splitAxis);

            // 负子节点，包围盒的最大值为分割坐标
            float3 negMax = bound.Max;
            negMax[(int)splitAxis] = splitPivot;
            KDTreeBound negBound = bound;
            negBound.Max = negMax;
            negIndex = GetTreeNodeIndex(negBound, parent.Boundary.x, splitAxisIndex);
            
            // 正子节点，包围盒的最小值为分割坐标
            float3 posMin = bound.Min;
            posMin[(int)splitAxis] = splitPivot;
            KDTreeBound posBound = bound;
            posBound.Min = posMin;
            posIndex = GetTreeNodeIndex(posBound, splitAxisIndex, parent.Boundary.y);
            
            // 更新父节点的分割信息
            parent.PartitionAxis = splitAxis;
            parent.PartitionCoordinate = splitPivot;
            parent.NegativeChildIndex = negIndex;
            parent.PositiveChildIndex = posIndex;
            _nodes[parentIndex] = parent;
        }

        /// <summary>
        /// 计算分割轴应该选择的轴心位置
        /// </summary>
        private float CalculatePivot(int startIndex, int endIndex, float boundStart, float boundEnd, KDTreePartitionAxis axis)
        {
            float mid = (boundStart + boundEnd) / 2;

            bool negtive = false;
            bool postive = false;
            
            float negMax = float.MaxValue;
            float posMin = float.MinValue;

            for (int i = startIndex; i < endIndex; i++)
            {
                float val = _points[_permutation[i]][(int)axis];

                if (val < mid)
                {
                    negtive = true;
                }
                else
                {
                    postive = true;
                }

                // 若两侧都有点，则中点最优，提前返回
                if (negtive && postive)
                {
                    return mid;
                }
            }

            if (negtive)
            {
                for (int i = startIndex; i < endIndex; i++)
                {
                    float val = _points[_permutation[i]][(int)axis];

                    if (negMax < val)
                    {
                        negMax = val;
                    }
                }
                return negMax;
            }

            for (int i = startIndex; i < endIndex; i++)
            {
                float val = _points[_permutation[i]][(int)axis];
                if (posMin > val)
                {
                    posMin = val;
                }
            }

            return posMin;
        }

        private int GetPartition(int start, int end, float partitionPivot, KDTreePartitionAxis axis)
        {
            int lp = start - 1;
            int rp = end;

            while (true)
            {
                do
                {
                    lp++;
                }while (lp < rp && _points[_permutation[lp]][(int)axis] < partitionPivot);

                do
                {
                    rp--;
                }while(lp < rp && _points[_permutation[lp]][(int)axis] >= partitionPivot);

                if (lp < rp)
                {
                    (_permutation[lp], _permutation[rp]) = (_permutation[rp], _permutation[lp]);
                }
                else
                {
                    return lp;
                }
            }
        }
        
        public void Dispose()
        {
            _points.Dispose();
            _nodes.Dispose();
            _indices.Dispose();
            _permutation.Dispose();
            _buildQueue.Dispose();
        }
    }
}