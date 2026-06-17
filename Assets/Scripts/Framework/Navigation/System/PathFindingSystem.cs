using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Framework
{
    public partial struct PathFindingSystem : ISystem
    {
        private ComponentLookup<ASOperation> _operationLookup;
        private ComponentLookup<ASResult> _resultLookup;
        private ComponentLookup<ASFollower> _followerLookup;
        private BufferLookup<ASPathBuffer> _pathBufferLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _operationLookup = state.GetComponentLookup<ASOperation>();
            _resultLookup = state.GetComponentLookup<ASResult>();
            _followerLookup = state.GetComponentLookup<ASFollower>();
            _pathBufferLookup = state.GetBufferLookup<ASPathBuffer>();
            
            state.RequireForUpdate<ASGrid>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _operationLookup.Update(ref state);
            _resultLookup.Update(ref state);
            _followerLookup.Update(ref state);
            _pathBufferLookup.Update(ref state);
            
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            
            bool hasGrid = false;
            ASGrid grid = default;
            LocalTransform gridTransform = default;
            NativeArray<ASCell> cells = default;
            int maxPerFrame = 10;

            foreach (var (gridValue, transform, cellsBuf, config) in SystemAPI.Query<RefRO<ASGrid>, 
                         RefRO<LocalTransform>, DynamicBuffer<ASCell>, RefRO<ASPathFindingConfig>>())
            {
                grid = gridValue.ValueRO;
                gridTransform = transform.ValueRO;
                maxPerFrame = config.ValueRO.MaxComputePerFrame > 0 ? config.ValueRO.MaxComputePerFrame : 10;
                
                cells = new NativeArray<ASCell>(cellsBuf.Length, Allocator.Temp);
                for (int i = 0; i < cellsBuf.Length; i++)
                {
                    cells[i] = cellsBuf[i];
                }
                
                hasGrid = true;
                break;
            }

            if (!hasGrid || cells.Length == 0)
            {
                if (cells.IsCreated)
                    cells.Dispose();
                ecb.Dispose();
                return;
            }
            
            // ---------- 对每个正在寻路的 Agent 执行 A* ----------
            int count = 0;
            // 注意：这里用 foreach 线性遍历。
            // 仅处理拥有 ASOperation 的 Agent（即 PFRequestSystem 新派发的请求）
            foreach (var (resultRW, entity) in SystemAPI.Query<RefRW<ASResult>>()
                         .WithAll<ASOperation, ASRequester, ASAgent>().WithEntityAccess())
            {
                if (count >= maxPerFrame) break;
                if (resultRW.ValueRO.FinishedSearch) continue; // 已完成的跳过
                if (!_operationLookup.HasComponent(entity)) continue;
                if (!_pathBufferLookup.HasBuffer(entity)) continue;

                // 执行 A* 搜索（直接在主线程执行，以保证可在 Unity Editor 中稳定运行）
                ASParallelSearch search = new ASParallelSearch
                {
                    OperationEntity = entity,
                    Operations = _operationLookup,
                    Results = _resultLookup,
                    PathPoints = _pathBufferLookup,
                    Grid = grid,
                    GridTransform = gridTransform,
                    Cells = cells,
                    NewCells = default,
                };
                search.Execute();

                ASResult result = _resultLookup[entity];
                if (result.FinishedSearch)
                {
                    if (_followerLookup.HasComponent(entity))
                    {
                        ASFollower follower = _followerLookup[entity];
                        follower.PathAvailable = result.PathFounded;
                        follower.TargetIndex = 0;
                        follower.DestinationReached = false;
                        ecb.SetComponent(entity, follower);
                    }

                    ecb.RemoveComponent<ASOperation>(entity);
                    ecb.RemoveComponent<ASRequester>(entity);
                }

                count++;
            }

            cells.Dispose();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
