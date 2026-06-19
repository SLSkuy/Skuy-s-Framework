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
        /// 通过该数组，Node中的Start,End长度不变，而指向的point改变
        /// 改变代价变小，且无需变动Node的Start,End即可得到划分后Node管理的节点数据
        /// </summary>
        private NativeArray<int> _permutation;
        
        private NativeQueue<int> _buildQueue;

        /// <summary>
        /// 每个叶子节点最多包含的点数量，经验值为64
        /// </summary>
        private const int MAX_POINTS_PER_LEAF_NODE = 64;

        public KDTree(NativeArray<float3> points, Allocator allocator, bool buildNow = true)
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
        
        public void Dispose()
        {
            _points.Dispose();
            _nodes.Dispose();
            _indices.Dispose();
            _permutation.Dispose();
            _buildQueue.Dispose();
        }
        
        #region 查询方法

        private KDTreeNode RootNode => _nodes[_indices[0]];
        
        /// <summary>
        /// 临时查询节点封装，避免函数过长参数声明
        /// </summary>
        private struct QueryNode
        {
            public int NodeIndex;
            public float3 ClosestPoint;
            public float Distance;
        }
        
        private struct KnnQueryTemp : IDisposable {
	        /// <summary>
	        /// 最大堆：维护当前已找到的 K 个最近邻候选。
	        /// 堆顶是候选中距离最大的点，用于快速判断新点是否应替换当前最远候选。
	        /// 容量固定为 K（查询的近邻数量）。
	        /// </summary>
	        public MinMaxHeap<int> MaxHeap;

	        /// <summary>
	        /// 最小堆：KD 树遍历的优先队列，按"最近可能距离"排序待访问节点。
	        /// 堆顶是最可能包含近邻的节点（距离最小），优先访问它。
	        ///
	        /// 容量固定为 64，对应树的最大深度约为 log_64(n)，
	        /// 在任意时刻堆中最多有左右子节点各一个路径的节点，即树深 * 2。
	        /// 假设树最深 32 层（可处理约 2^39 个节点），则最多 64 个节点在堆中。
	        /// </summary>
	        public MinMaxHeap<QueryNode> MinHeap;

	        /// <summary>
	        /// 创建查询临时内存。
	        /// </summary>
	        /// <param name="kCapacity">要查找的近邻数量 K（MaxHeap 的容量）</param>
	        /// <returns>初始化好的 KnnQueryTemp</returns>
	        public static KnnQueryTemp Create(int kCapacity) {
		        KnnQueryTemp temp;
		        temp.MaxHeap = new MinMaxHeap<int>(kCapacity, Allocator.Temp);
		        temp.MinHeap = new MinMaxHeap<QueryNode>(64, Allocator.Temp);
		        return temp;
	        }

	        /// <summary>
	        /// 将一个 KD 树节点推入最小堆（待访问节点优先队列）。
	        ///
	        /// 计算查询点到该节点包围盒最近点的平方距离，作为优先级。
	        /// 距离越小，优先级越高（越早被访问）。
	        /// </summary>
	        /// <param name="index">节点在 m_nodes 中的索引</param>
	        /// <param name="closestPoint">查询点到该节点包围盒的最近点（已由调用方计算好）</param>
	        /// <param name="queryPosition">查询点的位置</param>
	        public void PushQueryNode(int index, float3 closestPoint, float3 queryPosition) {
		        float lengthsq = math.lengthsq(closestPoint - queryPosition);

		        MinHeap.PushObjMin(new QueryNode {
			        NodeIndex = index, 
			        ClosestPoint = closestPoint,
			        Distance = lengthsq
		        }, lengthsq);
	        }

	        /// <summary>
	        /// 释放两个堆的非托管内存。
	        /// </summary>
	        public void Dispose() {
		        MaxHeap.Dispose();
		        MinHeap.Dispose();
	        }
        }

        public void QueryKNearest(float3 queryPosition, NativeSlice<int> result)
        {
			var temp = KnnQueryTemp.Create(result.Length);
			int k = result.Length;
			
			float bssr = float.PositiveInfinity;
			float3 rootClosestPoint = RootNode.Bound.ClosestPoint(queryPosition);

			temp.PushQueryNode(_indices[0], rootClosestPoint, queryPosition);

			while (temp.MinHeap.Count > 0) {
				QueryNode queryNode = temp.MinHeap.PopObjMin();

				// 若节点到查询点的最短距离 > 当前已知最远近邻，剪枝
				if (queryNode.Distance > bssr) {
					continue;
				}

				KDTreeNode node = _nodes[queryNode.NodeIndex];

				if (!node.IsLeaf) {
					int partitionAxis = (int)node.PartitionAxis;
					float partitionCoord = node.PartitionCoordinate;
					float3 tempClosestPoint = queryNode.ClosestPoint;

					if (tempClosestPoint[partitionAxis] - partitionCoord < 0) {
						temp.PushQueryNode(node.NegativeChildIndex, tempClosestPoint, queryPosition);
						tempClosestPoint[partitionAxis] = partitionCoord;

						if (node.Count != 0) {
							temp.PushQueryNode(node.PositiveChildIndex, tempClosestPoint, queryPosition);
						}
					} else {
						temp.PushQueryNode(node.PositiveChildIndex, tempClosestPoint, queryPosition);
						tempClosestPoint[partitionAxis] = partitionCoord;

						if (node.Count != 0) {
							temp.PushQueryNode(node.NegativeChildIndex, tempClosestPoint, queryPosition);
						}
					}
				} else {
					for (int i = node.Boundary.x; i < node.Boundary.y; i++) {
						int index = _permutation[i];
						float sqrDist = math.lengthsq(_points[index] - queryPosition);
						if (sqrDist <= bssr) {
							temp.MaxHeap.PushObjMax(index, sqrDist);
							if (temp.MaxHeap.Count == k) {
								bssr = temp.MaxHeap.HeadValue;
							}
						}
					}
				}
			}
			
			for (int i = 0; i < k; i++) {
				result[i] = temp.MaxHeap.PopObjMax();
			}

			temp.Dispose();
        }
        
        public void QueryRange(float3 queryPosition, float radius, NativeList<int> result) 
        {
			var temp = KnnQueryTemp.Create(32);
			float bssr = radius * radius;
			float3 rootClosestPoint = RootNode.Bound.ClosestPoint(queryPosition);
			temp.PushQueryNode(_indices[0], rootClosestPoint, queryPosition);

			while (temp.MinHeap.Count > 0) {
				QueryNode queryNode = temp.MinHeap.PopObjMin();
				if (queryNode.Distance > bssr) {
					continue;
				}

				KDTreeNode node = _nodes[queryNode.NodeIndex];

				if (!node.IsLeaf) {
					int partitionAxis = (int)node.PartitionAxis;
					float partitionCoord = node.PartitionCoordinate;
					float3 tempClosestPoint = queryNode.ClosestPoint;

					if (tempClosestPoint[partitionAxis] - partitionCoord < 0) {
						temp.PushQueryNode(node.NegativeChildIndex, tempClosestPoint, queryPosition);
						tempClosestPoint[partitionAxis] = partitionCoord;
						if (node.Count != 0) {
							temp.PushQueryNode(node.PositiveChildIndex, tempClosestPoint, queryPosition);
						}
					}
					else {
						temp.PushQueryNode(node.PositiveChildIndex, tempClosestPoint, queryPosition);
						tempClosestPoint[partitionAxis] = partitionCoord;

						if (node.Count != 0) {
							temp.PushQueryNode(node.NegativeChildIndex, tempClosestPoint, queryPosition);
						}
					}
				} else {
					for (int i = node.Boundary.x; i < node.Boundary.y; i++) {
						int index = _permutation[i];
						float sqrDist = math.lengthsq(_points[index] - queryPosition);

						if (sqrDist <= bssr) {
							if (temp.MaxHeap.IsFull) {
								temp.MaxHeap.Resize(temp.MaxHeap.Count * 2);
							}
							temp.MaxHeap.PushObjMax(index, sqrDist);
						}
					}
				}
			}
			
			while (temp.MaxHeap.Count > 0) {
				result.Add(temp.MaxHeap.PopObjMax());
			}

			temp.Dispose();
		}
        
        #endregion

        #region 构造方法
        
        /// <summary>
        /// 重建KD-Tree
        /// </summary>
        public void Rebuild()
        {
            _nodes.Clear();

            // 初始化排列数组：每个节点初始化时的“逻辑顺序”等于其原始索引
            // 后续不断划分更新逻辑位置对应的点索引
            // 规定每一个Node管理哪一个区间的点
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
        
        /// <summary>
        /// 分割信息定义，避免函数长串参数声明
        /// </summary>
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
            
            // 更新父节点的分割信息
            parent.PartitionAxis = split.Axis;
            parent.PartitionCoordinate = split.Coordinate;
            parent.NegativeChildIndex = negIndex;
            parent.PositiveChildIndex = posIndex;
            _nodes[parentIndex] = parent;

            return true;
        }

        /// <summary>
        /// 计算节点分割方案：选最长轴，算分割面，然后把点重新排列为负/正两段。
        /// </summary>
        private SplitPlan CreateSplitPlan(KDTreeNode parent)
        {
            KDTreePartitionAxis axis = parent.GetPartitionAxis();
            float splitCoordinate = SelectSplitCoordinate(parent.Boundary.x, parent.Boundary.y, parent.Bound, axis);
            int partitionIndex = PartitionByCoordinate(parent.Boundary.x, parent.Boundary.y, splitCoordinate, axis);
            
            // 负子节点
            KDTreeBound negativeBound = parent.Bound;
            float3 negativeMax = negativeBound.Max;
            negativeMax[(int)axis] = splitCoordinate;
            negativeBound.Max = negativeMax;

            // 正子节点
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
        /// 选择切割的坐标
        /// 优先使用包围盒中点；如果所有点都挤在中点一侧，则改用点集自身范围的中点。
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
        /// 将[start, end)重排为两段：
        /// [start, partitionIndex) 坐标小于splitCoordinate，归入负子节点；
        /// [partitionIndex, end) 坐标大于等于splitCoordinate，归入正子节点。
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

        /// <summary>
        /// 获取点的坐标
        /// </summary>
        private float GetPointCoordinate(int permutationIndex, KDTreePartitionAxis axis)
        {
            return _points[_permutation[permutationIndex]][(int)axis];
        }

        #endregion
    }
}
