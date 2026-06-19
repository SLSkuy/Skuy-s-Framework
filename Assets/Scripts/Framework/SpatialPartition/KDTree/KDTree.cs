using System;
using Unity.Collections;
using Unity.Mathematics;

namespace Framework
{
    /// <summary>
    /// KD-Tree 定义。
    /// </summary>
    public struct KDTree : IDisposable
    {
        /// <summary>
        /// 每个叶子节点最多包含的点数量，经验值为 64。
        /// </summary>
        private const int MAX_POINTS_PER_LEAF_NODE = 64;
        private const int ROOT_NODE_INDEX = 0;

        /// <summary>
        /// 原始点数据。
        /// </summary>
        private NativeArray<float3> _points;

        /// <summary>
        /// 节点缓存。
        /// </summary>
        private NativeList<KDTreeNode> _nodes;

        /// <summary>
        /// 点索引排列数组。节点只记录区间，划分时重排索引而不移动点数据。
        /// </summary>
        private NativeArray<int> _permutation;

        /// <summary>
        /// 构建队列。
        /// </summary>
        private NativeQueue<int> _buildQueue;

        public KDTree(NativeArray<float3> points, Allocator allocator, bool buildNow = true)
        {
            if(points.Length == 0) throw new ArgumentNullException(nameof(points));
            
            int nodeCount = 4 * (int)math.ceil(points.Length / (float)MAX_POINTS_PER_LEAF_NODE + 1) + 1;
            
            _points = new NativeArray<float3>(points.Length, allocator);
            _nodes = new NativeList<KDTreeNode>(nodeCount, allocator);
            _permutation = new NativeArray<int>(points.Length, allocator);
            _buildQueue = new NativeQueue<int>(allocator);
            NativeArray<float3>.Copy(points, _points);

            if (buildNow)
            {
                Rebuild();
            }
        }

        public void Dispose()
        {
            _points.Dispose();
            _nodes.Dispose();
            _permutation.Dispose();
            _buildQueue.Dispose();
        }

        #region 查询方法

        private KDTreeNode RootNode => _nodes[ROOT_NODE_INDEX];

        private struct QueryNode
        {
            public int NodeIndex;
            public float3 ClosestPoint;
            public float DistanceSq;
        }

        /// <summary>
        /// 查询操作，用于缓存节点
        /// </summary>
        private struct QueryOperation : IDisposable
        {
            /// <summary>
            /// 查询结果
            /// </summary>
            public NativeMaxPriorityHeap<int> Candidates;
            
            /// <summary>
            /// 查询缓存，根据优先级排列
            /// </summary>
            public NativeMinPriorityHeap<QueryNode> PendingNodes;

            public QueryOperation(int candidateCapacity, Allocator allocator)
            {
                Candidates = new NativeMaxPriorityHeap<int>(candidateCapacity, allocator);
                PendingNodes = new NativeMinPriorityHeap<QueryNode>(64, allocator);
            }

            public void PushNode(int index, float3 closestPoint, float3 queryPosition)
            {
                float distance = math.lengthsq(closestPoint - queryPosition);
                PendingNodes.Push(new QueryNode
                {
                    NodeIndex = index,
                    ClosestPoint = closestPoint,
                    DistanceSq = distance
                }, distance);
            }

            public void Dispose()
            {
                Candidates.Dispose();
                PendingNodes.Dispose();
            }
        }

        public NativeArray<int> QueryKNearest(float3 queryPosition, int k)
        {
            if (k < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(k));
            }
            
            QueryOperation operation = new QueryOperation(k, Allocator.Temp);

            try
            {
                float bestSqrRadius = float.PositiveInfinity;
                operation.PushNode(ROOT_NODE_INDEX, RootNode.Bound.ClosestPoint(queryPosition), queryPosition);

                // 优先访问距离查询点更近的包围盒，超过当前最远候选距离的节点可剪枝。
                while (operation.PendingNodes.Count > 0)
                {
                    QueryNode queryNode = operation.PendingNodes.Pop();
                    if (queryNode.DistanceSq > bestSqrRadius)
                    {
                        continue;
                    }

                    KDTreeNode node = _nodes[queryNode.NodeIndex];
                    if (node.IsLeaf)
                    {
                        SearchKNearestLeaf(node, queryPosition, k, ref bestSqrRadius, ref operation);
                    }
                    else
                    {
                        PushChildNodes(node, queryNode, queryPosition, ref operation);
                    }
                }
                
                int count = math.min(k, operation.Candidates.Count);
                NativeArray<int> result = new NativeArray<int>(count, Allocator.Temp);
                for (int i = 0; i < count; i++)
                {
                    result[i] = operation.Candidates.Pop();
                }
                
                return result;
            }
            finally
            {
                operation.Dispose();
            }
        }

        public NativeList<int> QueryRange(float3 queryPosition, float radius)
        {
            if (radius < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(radius));
            }
            
            NativeList<int> result = new NativeList<int>(Allocator.Temp);
            QueryOperation operation = new QueryOperation(32, Allocator.Temp);
            float sqrRadius = radius * radius;

            try
            {
                operation.PushNode(ROOT_NODE_INDEX, RootNode.Bound.ClosestPoint(queryPosition), queryPosition);

                // Range 查询使用固定半径剪枝，叶子节点内再做精确距离判断。
                while (operation.PendingNodes.Count > 0)
                {
                    QueryNode queryNode = operation.PendingNodes.Pop();
                    if (queryNode.DistanceSq > sqrRadius)
                    {
                        continue;
                    }

                    KDTreeNode node = _nodes[queryNode.NodeIndex];
                    if (node.IsLeaf)
                    {
                        SearchRangeLeaf(node, queryPosition, sqrRadius, ref operation);
                    }
                    else
                    {
                        PushChildNodes(node, queryNode, queryPosition, ref operation);
                    }
                }

                while (operation.Candidates.Count > 0)
                {
                    result.Add(operation.Candidates.Pop());
                }
                return result;
            }
            finally
            {
                operation.Dispose();
            }
        }

        private void PushChildNodes(KDTreeNode node, QueryNode queryNode, float3 queryPosition, ref QueryOperation operation)
        {
            int partitionAxis = (int)node.PartitionAxis;
            float partitionCoordinate = node.PartitionCoordinate;
            float3 closestPoint = queryNode.ClosestPoint;

            // 先访问查询点所在侧，另一侧用分割面修正后的最近点参与排序
            bool isNegativeSide = queryPosition[partitionAxis] < partitionCoordinate;
            int firstChildIndex = isNegativeSide ? node.NegativeChildIndex : node.PositiveChildIndex;
            int secondChildIndex = isNegativeSide ? node.PositiveChildIndex : node.NegativeChildIndex;

            // 因为是根据分割轴划分，因此只需修改一个坐标即可，即查询点在包围盒上的投影
            operation.PushNode(firstChildIndex, closestPoint, queryPosition);
            closestPoint[partitionAxis] = partitionCoordinate;
            operation.PushNode(secondChildIndex, closestPoint, queryPosition);
        }

        private void SearchKNearestLeaf(KDTreeNode node, float3 queryPosition, int k, ref float bestSqrRadius, ref QueryOperation operation)
        {
            for (int i = node.Boundary.x; i < node.Boundary.y; i++)
            {
                int index = _permutation[i];
                float sqrDistance = math.lengthsq(_points[index] - queryPosition);
                if (sqrDistance > bestSqrRadius)
                {
                    continue;
                }

                operation.Candidates.TryPushSmallest(index, sqrDistance);
                if (operation.Candidates.Count == k)
                {
                    bestSqrRadius = operation.Candidates.PeekValue;
                }
            }
        }

        private void SearchRangeLeaf(KDTreeNode node, float3 queryPosition, float sqrRadius, ref QueryOperation operation)
        {
            for (int i = node.Boundary.x; i < node.Boundary.y; i++)
            {
                int index = _permutation[i];
                float sqrDistance = math.lengthsq(_points[index] - queryPosition);
                if (sqrDistance > sqrRadius)
                {
                    continue;
                }

                operation.Candidates.Push(index, sqrDistance);
            }
        }

        #endregion

        #region 构造方法

        /// <summary>
        /// 重建 KD-Tree。
        /// </summary>
        public void Rebuild()
        {
            _nodes.Clear();
            _buildQueue.Clear();

            for (int i = 0; i < _permutation.Length; i++)
            {
                _permutation[i] = i;
            }

            GetTreeNodeIndex(MakeBound(), 0, _points.Length);
            _buildQueue.Enqueue(ROOT_NODE_INDEX);

            while (_buildQueue.Count > 0)
            {
                int index = _buildQueue.Dequeue();
                if (_nodes[index].Count <= MAX_POINTS_PER_LEAF_NODE)
                {
                    continue;
                }

                if (TrySplitNode(index, out int negIndex, out int posIndex))
                {
                    _buildQueue.Enqueue(negIndex);
                    _buildQueue.Enqueue(posIndex);
                }
            }
        }

        private int GetTreeNodeIndex(KDTreeBound bound, int start, int end)
        {
            _nodes.Add(new KDTreeNode
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

            foreach (float3 p in _points)
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

        private struct SplitPlan
        {
            public KDTreePartitionAxis Axis;
            public float Coordinate;
            public int PartitionIndex;
            public KDTreeBound NegativeBound;
            public KDTreeBound PositiveBound;
        }

        private bool TrySplitNode(int parentIndex, out int negIndex, out int posIndex)
        {
            KDTreeNode parent = _nodes[parentIndex];
            SplitPlan split = CreateSplitPlan(parent);

            negIndex = -1;
            posIndex = -1;

            if (split.PartitionIndex <= parent.Boundary.x || split.PartitionIndex >= parent.Boundary.y)
            {
                return false;
            }

            negIndex = GetTreeNodeIndex(split.NegativeBound, parent.Boundary.x, split.PartitionIndex);
            posIndex = GetTreeNodeIndex(split.PositiveBound, split.PartitionIndex, parent.Boundary.y);

            parent.PartitionAxis = split.Axis;
            parent.PartitionCoordinate = split.Coordinate;
            parent.NegativeChildIndex = negIndex;
            parent.PositiveChildIndex = posIndex;
            _nodes[parentIndex] = parent;

            return true;
        }

        /// <summary>
        /// 选最长轴切分，并把点索引重排为负/正两段。
        /// </summary>
        private SplitPlan CreateSplitPlan(KDTreeNode parent)
        {
            KDTreePartitionAxis axis = parent.GetPartitionAxis();
            float splitCoordinate = SelectSplitCoordinate(parent.Boundary.x, parent.Boundary.y, parent.Bound, axis);
            int partitionIndex = PartitionByCoordinate(parent.Boundary.x, parent.Boundary.y, splitCoordinate, axis);

            KDTreeBound negativeBound = parent.Bound;
            float3 negativeMax = negativeBound.Max;
            negativeMax[(int)axis] = splitCoordinate;
            negativeBound.Max = negativeMax;

            KDTreeBound positiveBound = parent.Bound;
            float3 positiveMin = positiveBound.Min;
            positiveMin[(int)axis] = splitCoordinate;
            positiveBound.Min = positiveMin;

            return new SplitPlan
            {
                Axis = axis,
                Coordinate = splitCoordinate,
                PartitionIndex = partitionIndex,
                NegativeBound = negativeBound,
                PositiveBound = positiveBound
            };
        }

        /// <summary>
        /// 优先使用包围盒中点；如果所有点都挤在一侧，则改用点集范围中点。
        /// </summary>
        private float SelectSplitCoordinate(int startIndex, int endIndex, KDTreeBound bound, KDTreePartitionAxis axis)
        {
            float boundMid = (bound.Min[(int)axis] + bound.Max[(int)axis]) * 0.5f;
            float pointMin = float.MaxValue;
            float pointMax = float.MinValue;

            for (int i = startIndex; i < endIndex; i++)
            {
                float coordinate = GetPointCoordinate(i, axis);
                pointMin = math.min(pointMin, coordinate);
                pointMax = math.max(pointMax, coordinate);
            }

            if (pointMin < boundMid && pointMax >= boundMid)
            {
                return boundMid;
            }

            return (pointMin + pointMax) * 0.5f;
        }

        /// <summary>
        /// 将 [start, end) 重排为负子节点区间和正子节点区间。
        /// </summary>
        private int PartitionByCoordinate(int start, int end, float splitCoordinate, KDTreePartitionAxis axis)
        {
            int partitionIndex = start;

            for (int i = start; i < end; i++)
            {
                if (GetPointCoordinate(i, axis) < splitCoordinate)
                {
                    (_permutation[partitionIndex], _permutation[i]) = (_permutation[i], _permutation[partitionIndex]);
                    partitionIndex++;
                }
            }

            return partitionIndex;
        }

        private float GetPointCoordinate(int permutationIndex, KDTreePartitionAxis axis)
        {
            return _points[_permutation[permutationIndex]][(int)axis];
        }

        #endregion
    }
}
