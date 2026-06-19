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
            if (points.Length == 0)
            {
                throw new ArgumentException("KDTree requires at least one point.", nameof(points));
            }

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

        /// <summary>
        /// 待访问树节点，记录查询点到节点包围盒的最近点和平方距离。
        /// </summary>
        private struct QueryNode : INativeHeapItem<QueryNode>, IEquatable<QueryNode>
        {
            public int NodeIndex;
            public float3 ClosestPoint;
            public float DistanceSq;
            public int HeapIndex { get; set; }

            public int CompareTo(QueryNode other)
            {
                return other.DistanceSq.CompareTo(DistanceSq);
            }

            public bool Equals(QueryNode other)
            {
                return NodeIndex == other.NodeIndex;
            }
        }

        /// <summary>
        /// KNN 候选点。CompareTo 让距离更大的候选排在堆顶，便于替换当前最远候选。
        /// </summary>
        private struct CandidateNode : INativeHeapItem<CandidateNode>, IEquatable<CandidateNode>
        {
            public int PointIndex;
            public float DistanceSq;
            public int HeapIndex { get; set; }

            public int CompareTo(CandidateNode other)
            {
                return DistanceSq.CompareTo(other.DistanceSq);
            }

            public bool Equals(CandidateNode other)
            {
                return PointIndex == other.PointIndex;
            }
        }

        public NativeArray<int> QueryKNearest(float3 queryPosition, int k)
        {
            ValidateQueryState();

            if (k < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(k));
            }

            if (k == 0)
            {
                return new NativeArray<int>(0, Allocator.Temp);
            }

            int candidateCapacity = math.min(k, _points.Length);

            // candidates 维护当前 K 个最近候选，堆顶是候选中距离最远的点。
            NativeMinHeap<CandidateNode> candidates = new NativeMinHeap<CandidateNode>(candidateCapacity, Allocator.Temp);

            // pendingNodes 按节点包围盒到查询点的最近距离优先遍历。
            NativeMinHeap<QueryNode> pendingNodes = new NativeMinHeap<QueryNode>(_nodes.Length, Allocator.Temp);

            try
            {
                float bestSqrRadius = float.PositiveInfinity;
                PushQueryNode(ROOT_NODE_INDEX, RootNode.Bound.ClosestPoint(queryPosition), queryPosition, ref pendingNodes);

                // 先访问最近可能距离更小的包围盒，超过当前最远候选距离的节点可剪枝。
                while (pendingNodes.Length > 0)
                {
                    QueryNode queryNode = pendingNodes.PopFirstItem();
                    if (queryNode.DistanceSq > bestSqrRadius)
                    {
                        continue;
                    }

                    KDTreeNode node = _nodes[queryNode.NodeIndex];
                    if (node.IsLeaf)
                    {
                        SearchKNearestLeaf(node, queryPosition, candidateCapacity, ref bestSqrRadius, ref candidates);
                    }
                    else
                    {
                        PushChildNodes(node, queryNode, queryPosition, ref pendingNodes);
                    }
                }

                NativeArray<int> result = new NativeArray<int>(candidates.Length, Allocator.Temp);
                for (int i = 0; i < result.Length; i++)
                {
                    // 这里弹出顺序是由远到近；调用方只依赖集合时无需额外排序。
                    result[i] = candidates.PopFirstItem().PointIndex;
                }

                return result;
            }
            finally
            {
                candidates.Dispose();
                pendingNodes.Dispose();
            }
        }

        public NativeList<int> QueryRange(float3 queryPosition, float radius)
        {
            ValidateQueryState();

            if (radius < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(radius));
            }

            NativeList<int> result = new NativeList<int>(Allocator.Temp);

            // Range 查询只需要待访问节点堆，命中的点直接写入 result。
            NativeMinHeap<QueryNode> pendingNodes = new NativeMinHeap<QueryNode>(_nodes.Length, Allocator.Temp);
            float sqrRadius = radius * radius;

            try
            {
                PushQueryNode(ROOT_NODE_INDEX, RootNode.Bound.ClosestPoint(queryPosition), queryPosition, ref pendingNodes);

                // Range 查询用固定半径剪枝，叶子节点内再做精确距离判断。
                while (pendingNodes.Length > 0)
                {
                    QueryNode queryNode = pendingNodes.PopFirstItem();
                    if (queryNode.DistanceSq > sqrRadius)
                    {
                        continue;
                    }

                    KDTreeNode node = _nodes[queryNode.NodeIndex];
                    if (node.IsLeaf)
                    {
                        SearchRangeLeaf(node, queryPosition, sqrRadius, result);
                    }
                    else
                    {
                        PushChildNodes(node, queryNode, queryPosition, ref pendingNodes);
                    }
                }

                return result;
            }
            finally
            {
                pendingNodes.Dispose();
            }
        }

        private void ValidateQueryState()
        {
            if (_nodes.Length == 0)
            {
                throw new InvalidOperationException("KDTree must be rebuilt before querying.");
            }
        }

        private void PushQueryNode(int nodeIndex, float3 closestPoint, float3 queryPosition, ref NativeMinHeap<QueryNode> pendingNodes)
        {
            float distanceSq = math.lengthsq(closestPoint - queryPosition);
            pendingNodes.Add(new QueryNode
            {
                NodeIndex = nodeIndex,
                ClosestPoint = closestPoint,
                DistanceSq = distanceSq
            });
        }

        private void PushChildNodes(KDTreeNode node, QueryNode queryNode, float3 queryPosition, ref NativeMinHeap<QueryNode> pendingNodes)
        {
            int partitionAxis = (int)node.PartitionAxis;
            float partitionCoordinate = node.PartitionCoordinate;
            float3 closestPoint = queryNode.ClosestPoint;

            // 先访问查询点所在侧，另一侧用分割面修正后的最近点参与排序。
            bool isNegativeSide = queryPosition[partitionAxis] < partitionCoordinate;
            int firstChildIndex = isNegativeSide ? node.NegativeChildIndex : node.PositiveChildIndex;
            int secondChildIndex = isNegativeSide ? node.PositiveChildIndex : node.NegativeChildIndex;

            PushQueryNode(firstChildIndex, closestPoint, queryPosition, ref pendingNodes);

            // KD-Tree 子节点按单轴划分，因此另一侧包围盒最近点只需修正分割轴坐标。
            closestPoint[partitionAxis] = partitionCoordinate;
            PushQueryNode(secondChildIndex, closestPoint, queryPosition, ref pendingNodes);
        }

        /// <summary>
        /// 扫描叶子节点，维护最多 K 个最近候选。
        /// </summary>
        private void SearchKNearestLeaf(KDTreeNode node, float3 queryPosition, int k, ref float bestSqrRadius, ref NativeMinHeap<CandidateNode> candidates)
        {
            for (int i = node.Boundary.x; i < node.Boundary.y; i++)
            {
                int index = _permutation[i];
                float sqrDistance = math.lengthsq(_points[index] - queryPosition);
                if (sqrDistance > bestSqrRadius)
                {
                    continue;
                }

                if (candidates.Length < k)
                {
                    candidates.Add(new CandidateNode { PointIndex = index, DistanceSq = sqrDistance });
                }
                else if (sqrDistance < candidates.PeekFirstItem().DistanceSq)
                {
                    candidates.PopFirstItem();
                    candidates.Add(new CandidateNode { PointIndex = index, DistanceSq = sqrDistance });
                }

                if (candidates.Length == k)
                {
                    // 候选堆顶是当前最远候选，可作为后续节点和点的剪枝半径。
                    bestSqrRadius = candidates.PeekFirstItem().DistanceSq;
                }
            }
        }

        /// <summary>
        /// 扫描叶子节点，收集半径内的所有候选。
        /// </summary>
        private void SearchRangeLeaf(KDTreeNode node, float3 queryPosition, float sqrRadius, NativeList<int> result)
        {
            for (int i = node.Boundary.x; i < node.Boundary.y; i++)
            {
                int index = _permutation[i];
                float sqrDistance = math.lengthsq(_points[index] - queryPosition);
                if (sqrDistance <= sqrRadius)
                {
                    result.Add(index);
                }
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

            // 初始化排列数组：逻辑顺序等于原始索引，后续划分只重排索引。
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

            // 更新父节点的分割信息。
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
