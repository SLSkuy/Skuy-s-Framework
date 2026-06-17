using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Framework
{
    /// <summary>
    /// A* 寻路算法实现
    /// </summary>
    public struct ASParallelSearch
    {
        public Entity OperationEntity;
        
        public ComponentLookup<ASOperation> Operations;
        public ComponentLookup<ASResult> Results;
        public BufferLookup<ASPathBuffer> PathPoints;
        
        public NativeHashSet<ASCell> ClosedCells;
        public NativeMinHeap<ASCell> OpenCells;

        public ASGrid Grid;
        public LocalTransform GridTransform;
        
        // A*缓存数组，A*搜索过程会更改G/H/PreviousNodexIndex值
        public NativeArray<ASCell> Cells;
        public NativeArray<ASCell> NewCells;

        public void Execute()
        {
            NewCells = new NativeArray<ASCell>(Cells.Length, Allocator.Temp);
            NativeArray<ASCell>.Copy(Cells, NewCells);

            ASOperation operation = Operations[OperationEntity];
            ASResult operationResult = Results[OperationEntity];
            DynamicBuffer<ASPathBuffer> pathBuffer = PathPoints[OperationEntity];

            operationResult.PathFounded = false;
            operationResult.FinishedSearch = false;

            // 将起点/终点世界坐标转换为网格节点
            ASCell startCell = GridUtils.WorldPosToCell(operation.StartPoint, Grid, GridTransform, NewCells);
            ASCell targetCell = GridUtils.WorldPosToCell(operation.TargetPoint, Grid, GridTransform, NewCells);

            ASCell closestCell = new ASCell();
            int closetCellDistFromGoal = int.MaxValue;
            bool targetFound = false;

            // 终点不可通行时，寻找最近可通行节点
            if (!targetCell.IsAccessible)
            {
                ASCell newTargetCell = new ASCell();
                if (ExpandInaccessibleBFS(ref newTargetCell, targetCell, startCell, Grid, NewCells))
                {
                    targetCell = newTargetCell;
                }
                else
                {
                    operationResult.PathFounded = false;
                    operationResult.FinishedSearch = true;
                    Results[OperationEntity] = operationResult;
                    NewCells.Dispose();
                    return;
                }
            }

            OpenCells = new NativeMinHeap<ASCell>(Grid.XCount * Grid.YCount, Allocator.Temp);
            ClosedCells = new NativeHashSet<ASCell>(0, Allocator.Temp);
            OpenCells.Add(startCell);

            // 已访问集合（用于 O(1) 判断节点是否在 ClosedSet）
            NativeHashSet<int> closedIndexSet = new NativeHashSet<int>(0, Allocator.Temp);

            // 记录节点是否已在 OpenSet 中（用于 O(1) 判断）
            NativeHashMap<int, int> openIndexHeapPos = new NativeHashMap<int, int>(0, Allocator.Temp);
            int startIndex = startCell.X + startCell.Y * Grid.XCount;
            openIndexHeapPos.TryAdd(startIndex, 0);

            while (OpenCells.Length > 0)
            {
                ASCell current = OpenCells.PopFirstItem();
                int currentIndex = current.X + current.Y * Grid.XCount;
                closedIndexSet.Add(currentIndex);
                ClosedCells.Add(current);
                openIndexHeapPos.Remove(currentIndex);

                // 到达终点：回溯路径并返回
                if (current.Equals(targetCell))
                {
                    operationResult.PathFounded = true;
                    operationResult.FinishedSearch = true;
                    RetracePath(startCell, current, Grid, GridTransform, NewCells, pathBuffer, OperationEntity);
                    targetFound = true;
                    break;
                }

                // 记录距目标最近的节点（用于无法直达时的备选路径）
                int currentToGoal = GridUtils.CellDistance(current, targetCell);
                if (currentToGoal < closetCellDistFromGoal)
                {
                    closetCellDistFromGoal = currentToGoal;
                    closestCell = current;
                }

                NativeArray<int2> neighbours = GridUtils.CellNeighbours(Allocator.Temp);
                foreach (int2 neighbourOffset in neighbours)
                {
                    int nx = current.X + neighbourOffset.x;
                    int ny = current.Y + neighbourOffset.y;

                    // 边界检查
                    if (nx < 0 || nx >= Grid.XCount || ny < 0 || ny >= Grid.YCount) continue;

                    int neighbourIndex = nx + ny * Grid.XCount;

                    // 注意：从 NewCells（工作副本）读取，以便获得最新的 GCost/PreviousNodeIndex 等状态
                    ASCell neighbourCell = NewCells[neighbourIndex];

                    // 不可通行：跳过
                    if (!neighbourCell.IsAccessible) continue;

                    // 已在 Closed 中：跳过
                    if (closedIndexSet.Contains(neighbourIndex)) continue;

                    int movementCost = current.GCost + GridUtils.CellDistance(current, neighbourCell) + (int)neighbourCell.Penalty;

                    bool inOpen = openIndexHeapPos.ContainsKey(neighbourIndex);

                    // 如果新代价更优，或节点尚未加入 Open Set
                    if (!inOpen || movementCost < neighbourCell.GCost || neighbourCell.GCost == 0)
                    {
                        neighbourCell.GCost = movementCost;
                        neighbourCell.HCost = GridUtils.CellDistance(neighbourCell, targetCell);
                        neighbourCell.PreviousNodeIndex = currentIndex;

                        // 写回工作数组
                        NewCells[neighbourIndex] = neighbourCell;

                        if (inOpen)
                        {
                            // 找到在堆中的位置并更新
                            for (int i = 0; i < OpenCells.Length; i++)
                            {
                                if (OpenCells.Items[i].X == neighbourCell.X && OpenCells.Items[i].Y == neighbourCell.Y)
                                {
                                    neighbourCell.HeapIndex = i;
                                    OpenCells.SetItemAt(neighbourCell, i);
                                    OpenCells.UpdateItem(neighbourCell);
                                    openIndexHeapPos[neighbourIndex] = neighbourCell.HeapIndex;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            OpenCells.Add(neighbourCell);
                            openIndexHeapPos.TryAdd(neighbourIndex, neighbourCell.HeapIndex);
                        }
                    }
                }

                neighbours.Dispose();
            }

            // 未找到完整路径：返回距离目标最近的节点的路径
            if (!targetFound)
            {
                operationResult.PathFounded = true;
                operationResult.FinishedSearch = true;
                RetracePath(startCell, closestCell, Grid, GridTransform, NewCells, pathBuffer, OperationEntity);
            }

            Results[OperationEntity] = operationResult;

            // 释放所有 Native 容器
            NewCells.Dispose();
            OpenCells.Dispose();
            ClosedCells.Dispose();
            closedIndexSet.Dispose();
            openIndexHeapPos.Dispose();
        }
        
        private void RetracePath(ASCell _startNode, ASCell _targetNode, ASGrid _grid, LocalTransform _gridLocalTransform,
            NativeArray<ASCell> _nodes, DynamicBuffer<ASPathBuffer> _pathPoints, Entity _agentEntity)
        {
            _pathPoints.Clear();

            ASCell pathCursor = _targetNode;
            NativeList<ASCell> pathNodes = new NativeList<ASCell>(Allocator.Temp);

            // 从终点向起点回溯（通过父节点链），防止 PreviousNodeIndex 越界导致死循环
            int safetyCount = 0;
            int maxSteps = _nodes.Length;
            while (!(pathCursor.X == _startNode.X && pathCursor.Y == _startNode.Y) && safetyCount < maxSteps)
            {
                pathNodes.Add(pathCursor);
                int prevIdx = pathCursor.PreviousNodeIndex;
                if (prevIdx < 0 || prevIdx >= _nodes.Length) break;
                pathCursor = _nodes[prevIdx];
                safetyCount++;
            }

            // 反转路径（从起点到终点的正向顺序），转换为世界坐标
            for (int i = pathNodes.Length - 1; i >= 0; i--)
            {
                _pathPoints.Add(new ASPathBuffer()
                {
                    Point = GridUtils.GridCoordsToWorld(pathNodes[i].X, pathNodes[i].Y, _grid, _gridLocalTransform)
                });
            }

            // 最后追加精确的目标世界坐标（替代最近节点中心，减少最终误差）
            _pathPoints.Add(new ASPathBuffer() { Point = Operations[_agentEntity].TargetPoint });

            pathNodes.Dispose();
        }
        
        bool ExpandInaccessibleBFS(ref ASCell _closestNode, ASCell _targetNode, ASCell _startingNode,
            ASGrid _grid, NativeArray<ASCell> _nodes)
        {
            NativeArray<bool> visited = new NativeArray<bool>(_grid.XCount * _grid.YCount, Allocator.Temp);
            NativeQueue<(int, int)> nodeQueue = new NativeQueue<(int, int)>(Allocator.Temp);
            nodeQueue.Enqueue((_targetNode.X, _targetNode.Y));

            bool foundClosestTarget = false;
            int closestDistance = int.MaxValue;

            while (nodeQueue.Count > 0)
            {
                (int, int) currCoords = nodeQueue.Dequeue();

                // 边界检查
                if (currCoords.Item1 < 0 || currCoords.Item1 >= _grid.XCount ||
                    currCoords.Item2 < 0 || currCoords.Item2 >= _grid.YCount) continue;

                // 索引方式必须与 GridAuthoring / WorldPosToCell 保持一致：index = x + y * CellXCount
                int currIndex = currCoords.Item1 + currCoords.Item2 * _grid.XCount;

                // 搜索范围限制
                ASCell currentCell = _nodes[currIndex];
                if (GridUtils.CellDistance(_targetNode, currentCell) > _grid.TargetInaccessibleSearchTolerance * 10) continue;

                if (visited[currIndex]) continue;

                // 找到可通行节点：记录距起点最近的一个
                if (currentCell.IsAccessible)
                {
                    foundClosestTarget = true;
                    int dist = GridUtils.CellDistance(_startingNode, currentCell);
                    if (dist < closestDistance)
                    {
                        _closestNode = currentCell;
                        closestDistance = dist;
                    }
                    continue; // 不继续扩展（可通行节点是终点）
                }

                visited[currIndex] = true;

                // 向4方向扩展（只扩展不可通行节点内部，寻找边界上的可通行节点）
                nodeQueue.Enqueue((currCoords.Item1 + 1, currCoords.Item2));
                nodeQueue.Enqueue((currCoords.Item1, currCoords.Item2 + 1));
                nodeQueue.Enqueue((currCoords.Item1 - 1, currCoords.Item2));
                nodeQueue.Enqueue((currCoords.Item1, currCoords.Item2 - 1));
            }

            visited.Dispose();
            nodeQueue.Dispose();
            return foundClosestTarget;
        }
    }
}