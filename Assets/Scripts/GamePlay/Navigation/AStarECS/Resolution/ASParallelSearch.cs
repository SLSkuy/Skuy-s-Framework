using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Utils;
using Utils.DataStruct;

namespace GamePlay.Navigation
{
    /// <summary>
    /// A* 寻路算法实现
    /// </summary>
    public struct ASParallelSearch
    {
        /// <summary>本次寻路请求所属实体</summary>
        public Entity OperationEntity;

        /// <summary>寻路输入：起点和终点</summary>
        public ComponentLookup<ASOperation> Operations;

        /// <summary>寻路输出：是否完成、是否找到路径</summary>
        public ComponentLookup<ASResult> Results;

        /// <summary>寻路输出：路径点缓存</summary>
        public BufferLookup<ASPathBuffer> PathPoints;

        public ASGrid Grid;
        public LocalTransform GridTransform;

        /// <summary>只读网格快照。Execute 内部会复制一份工作数组，避免污染网格原始数据。</summary>
        public NativeArray<ASCell> Cells;

        public void Execute()
        {
            ASOperation operation = Operations[OperationEntity];
            DynamicBuffer<ASPathBuffer> pathBuffer = PathPoints[OperationEntity];
            NativeArray<ASCell> searchCells = new NativeArray<ASCell>(Cells.Length, Allocator.Temp);
            NativeArray<ASCell>.Copy(Cells, searchCells);

            ASCell startCell = GridUtils.WorldPosToCell(operation.StartPoint, Grid, GridTransform, searchCells);
            ASCell targetCell = GridUtils.WorldPosToCell(operation.TargetPoint, Grid, GridTransform, searchCells);
            if (!TryResolveTargetCell(ref targetCell, startCell, searchCells))
            {
                WriteResult(false);
                pathBuffer.Clear();
                searchCells.Dispose();
                return;
            }

            NativeMinHeap<ASCell> openList = new NativeMinHeap<ASCell>(Grid.XCount * Grid.YCount, Allocator.Temp);
            NativeHashSet<int> closedIndexSet = new NativeHashSet<int>(0, Allocator.Temp);
            NativeHashSet<int> openIndexSet = new NativeHashSet<int>(0, Allocator.Temp);
            NativeArray<int2> neighbourOffsets = GridUtils.CellNeighbours(Allocator.Temp);
            
            int startIndex = startCell.X + startCell.Y * Grid.XCount;
            openList.Add(startCell);
            openIndexSet.Add(startIndex);

            ASCell closestCell = startCell;
            int closestCellDistFromGoal = int.MaxValue;
            bool pathFounded = false;

            while (openList.Length > 0)
            {
                // 取出F代价最小节点，其为当前最优选择
                ASCell current = openList.Pop();
                int currentIndex = GridUtils.CellIndex(Grid, current);

                closedIndexSet.Add(currentIndex);
                openIndexSet.Remove(currentIndex);

                // 命中目标后立刻回溯路径；路径点缓存只在这里或兜底路径中写入。
                if (current.Equals(targetCell))
                {
                    RetracePath(startCell, current, searchCells, pathBuffer, operation.TargetPoint);
                    pathFounded = true;
                    break;
                }

                int currentToGoal = GridUtils.CellDistance(current, targetCell);
                if (currentToGoal < closestCellDistFromGoal)
                {
                    closestCellDistFromGoal = currentToGoal;
                    closestCell = current;
                }

                EvaluateNeighbours(current, targetCell, currentIndex,
                    searchCells, neighbourOffsets, closedIndexSet, 
                    openIndexSet, ref openList);
            }

            // 搜索失败时仍返回一条尽量靠近目标的路径，调用方可继续按 PathFounded=true 移动到最近点。
            if (!pathFounded && closestCellDistFromGoal < int.MaxValue)
            {
                RetracePath(startCell, closestCell, searchCells, pathBuffer, operation.TargetPoint);
                pathFounded = true;
            }

            // 写回结果
            WriteResult(pathFounded);

            neighbourOffsets.Dispose();
            openList.Dispose();
            closedIndexSet.Dispose();
            openIndexSet.Dispose();
            searchCells.Dispose();
        }

        /// <summary>
        /// 当目标节点不可走时，在目标附近找一个可落脚节点，避免请求直接失败。
        /// </summary>
        private bool TryResolveTargetCell(ref ASCell targetCell, ASCell startCell, NativeArray<ASCell> searchCells)
        {
            if (targetCell.IsAccessible) return true;

            ASCell closestAccessibleCell = new ASCell();
            if (!ExpandInaccessibleBFS(ref closestAccessibleCell, targetCell, startCell, searchCells))
            {
                return false;
            }

            targetCell = closestAccessibleCell;
            return true;
        }

        /// <summary>
        /// 评估当前节点周围的 8 个邻居，并维护 Open/Closed 集合。
        /// </summary>
        private void EvaluateNeighbours(ASCell current, ASCell targetCell, int currentIndex,
            NativeArray<ASCell> searchCells, NativeArray<int2> neighbourOffsets,
            NativeHashSet<int> closedIndexSet, NativeHashSet<int> openIndexSet,
            ref NativeMinHeap<ASCell> openSet)
        {
            foreach (int2 neighbourOffset in neighbourOffsets)
            {
                int nx = current.X + neighbourOffset.x;
                int ny = current.Y + neighbourOffset.y;
                if (nx < 0 || nx >= Grid.XCount || ny < 0 || ny >= Grid.YCount) continue;

                // 已经被选中处理，并拓展过邻居，跳过节点
                int neighbourIndex = nx + ny * Grid.XCount;
                if (closedIndexSet.Contains(neighbourIndex)) continue;

                // 节点无法通行，跳过节点
                ASCell neighbourCell = searchCells[neighbourIndex];
                if (!neighbourCell.IsAccessible) continue;

                // 计算G代价，若计算的新的G代价大于通过其他节点计算的得到的G代价，则不更新对应节点的G代价，跳过节点
                int movementCost = current.GCost + GridUtils.CellDistance(current, neighbourCell) + (int)neighbourCell.Penalty;
                bool alreadyInOpen = openIndexSet.Contains(neighbourIndex);
                if (alreadyInOpen && movementCost >= neighbourCell.GCost && neighbourCell.GCost != 0) continue;

                // 更新节点G、F代价，设置前驱节点并加入openList以供选择
                neighbourCell.GCost = movementCost;
                neighbourCell.HCost = GridUtils.CellDistance(neighbourCell, targetCell);
                neighbourCell.PreviousNodeIndex = currentIndex;
                searchCells[neighbourIndex] = neighbourCell;    // 更新节点数据

                if (alreadyInOpen)
                {
                    // 已经加入openList，更新数据
                    UpdateOpenSetItem(neighbourCell, ref openSet);
                }
                else
                {
                    // 加入openList
                    openSet.Add(neighbourCell);
                    openIndexSet.Add(neighbourIndex);
                }
            }
        }

        /// <summary>
        /// NativeMinHeap 只能通过 HeapIndex 更新元素；节点被交换后索引可能变化，因此这里按坐标定位一次。
        /// </summary>
        private void UpdateOpenSetItem(ASCell cell, ref NativeMinHeap<ASCell> openSet)
        {
            for (int i = 0; i < openSet.Length; i++)
            {
                if (openSet.Items[i].X != cell.X || openSet.Items[i].Y != cell.Y) continue;

                cell.HeapIndex = i;
                openSet.SetItemAt(cell, i);
                openSet.UpdateItem(cell);
                return;
            }
        }

        /// <summary>
        /// 从目标节点沿 PreviousNodeIndex 回到起点，再反向写入世界坐标路径点。
        /// </summary>
        private void RetracePath(ASCell startNode, ASCell targetNode, NativeArray<ASCell> searchCells,
            DynamicBuffer<ASPathBuffer> pathPoints, float3 exactTargetPoint)
        {
            pathPoints.Clear();

            ASCell pathCursor = targetNode;
            NativeList<ASCell> pathNodes = new NativeList<ASCell>(Allocator.Temp);

            // 防御性限制最大回溯次数，避免异常 PreviousNodeIndex 造成死循环。
            int safetyCount = 0;
            int maxSteps = searchCells.Length;
            while (!pathCursor.Equals(startNode) && safetyCount < maxSteps)
            {
                pathNodes.Add(pathCursor);
                int prevIdx = pathCursor.PreviousNodeIndex;
                if (prevIdx < 0 || prevIdx >= searchCells.Length) break;

                pathCursor = searchCells[prevIdx];
                safetyCount++;
            }

            // 反向放回路径缓存数组，寻路时就是正向结果
            for (int i = pathNodes.Length - 1; i >= 0; i--)
            {
                pathPoints.Add(new ASPathBuffer
                {
                    Point = GridUtils.GridCoordsToWorld(pathNodes[i].X, pathNodes[i].Y, Grid, GridTransform)
                });
            }

            // 最后一段使用请求中的精确目标点，减少停在格子中心带来的误差。
            pathPoints.Add(new ASPathBuffer { Point = exactTargetPoint });

            pathNodes.Dispose();
        }

        /// <summary>
        /// 目标格不可通行时，从目标格向外扩展，找离起点最近的可通行格。
        /// </summary>
        private bool ExpandInaccessibleBFS(ref ASCell closestNode, ASCell targetNode,
            ASCell startingNode, NativeArray<ASCell> searchCells)
        {
            NativeArray<bool> visited = new NativeArray<bool>(Grid.XCount * Grid.YCount, Allocator.Temp);
            NativeQueue<(int, int)> nodeQueue = new NativeQueue<(int, int)>(Allocator.Temp);
            nodeQueue.Enqueue((targetNode.X, targetNode.Y));

            bool foundClosestTarget = false;
            int closestDistance = int.MaxValue;

            while (nodeQueue.Count > 0)
            {
                (int, int) currCoords = nodeQueue.Dequeue();

                if (currCoords.Item1 < 0 || currCoords.Item1 >= Grid.XCount ||
                    currCoords.Item2 < 0 || currCoords.Item2 >= Grid.YCount) continue;

                int currIndex = currCoords.Item1 + currCoords.Item2 * Grid.XCount;
                ASCell currentCell = searchCells[currIndex];
                if (GridUtils.CellDistance(targetNode, currentCell) > Grid.TargetInaccessibleSearchTolerance * 10) continue;

                if (visited[currIndex]) continue;

                if (currentCell.IsAccessible)
                {
                    foundClosestTarget = true;
                    int dist = GridUtils.CellDistance(startingNode, currentCell);
                    if (dist < closestDistance)
                    {
                        closestNode = currentCell;
                        closestDistance = dist;
                    }
                    continue;
                }

                visited[currIndex] = true;

                // 只用 4 方向扩展不可通行区域，找到边界上的可通行节点即可。
                nodeQueue.Enqueue((currCoords.Item1 + 1, currCoords.Item2));
                nodeQueue.Enqueue((currCoords.Item1, currCoords.Item2 + 1));
                nodeQueue.Enqueue((currCoords.Item1 - 1, currCoords.Item2));
                nodeQueue.Enqueue((currCoords.Item1, currCoords.Item2 - 1));
            }

            visited.Dispose();
            nodeQueue.Dispose();
            return foundClosestTarget;
        }

        private void WriteResult(bool pathFounded)
        {
            Results[OperationEntity] = new ASResult
            {
                PathFounded = pathFounded,
                FinishedSearch = true,
            };
        }
    }
}
