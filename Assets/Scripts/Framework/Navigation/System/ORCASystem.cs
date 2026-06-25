using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Framework
{
    /// <summary>
    /// 动态避障System
    /// 接受PreferVelocitySystem计算得到的PreferVelocity
    /// 生成ORCA约束线，转换线性规划问题，最终得到接近期望速度的安全速度
    /// </summary>
    [UpdateAfter(typeof(PreferVelocitySystem))]
    public partial struct ORCASystem : ISystem
    {
        private struct NeighbourInfo
        {
            public int Index;
            public float DistanceSq;
        }
        /// <summary>
        /// math.EPSILON太小了
        /// </summary>
        private const float EPSILON = 0.00001f;
        private const float DEFAULT_TIME_HORIZON = 2.5f;
        
        private EntityQuery _query;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // 缓存查询
            _query = new EntityQueryBuilder(Allocator.Temp).WithAll<ASAgent, LocalTransform>().Build(ref state);
            
            state.RequireForUpdate<ASAgent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;

            // 获取Agent信息
            NativeArray<Entity> entities = _query.ToEntityArray(Allocator.Temp);
            NativeArray<ASAgent> agents = _query.ToComponentDataArray<ASAgent>(Allocator.Temp);
            NativeArray<LocalTransform> transforms = _query.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            
            // ORCA计算
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                ASAgent agent = agents[i];
                DynamicBuffer<ASORCALine> orcaLines = SystemAPI.GetBuffer<ASORCALine>(entity);
                orcaLines.Clear();
                
                // 转换速度坐标
                float2 preferredVelocity = ORCAUtils.ToPlane(agent.PreferVelocity);

                // 构造ORCA约束线
                BuildAgentLines(i, agents, transforms, dt, orcaLines);
                
                int failedLine = LinearProgram2(orcaLines, agent.MaxSpeed, preferredVelocity, out var currentVelocity);
                if (failedLine < orcaLines.Length)
                {
                    LinearProgram3(orcaLines, failedLine, agent.MaxSpeed, ref currentVelocity);
                }
                
                agent.CurrentVelocity = ORCAUtils.ToWorld(currentVelocity);
                state.EntityManager.SetComponentData(entity, agent);
            }
            
            entities.Dispose();
            agents.Dispose();
            transforms.Dispose();
        }

        /// <summary>
        /// 为当前的Agent和它附近的其他Agent生成ORCA约束线
        /// </summary>
        [BurstCompile]
        private void BuildAgentLines(int agentIndex, NativeArray<ASAgent> agents, NativeArray<LocalTransform> transforms,
            float dt, DynamicBuffer<ASORCALine> orcaLines)
        {
            // 获取Agent数据，只支持平面处理
            ASAgent agent = agents[agentIndex];
            float2 position = ORCAUtils.ToPlane(transforms[agentIndex].Position);
            float2 velocity = ORCAUtils.ToPlane(agent.CurrentVelocity);
            float timeHorizon = agent.TimeHorizon > 0f ? agent.TimeHorizon : DEFAULT_TIME_HORIZON;
            
            // TODO: 使用KD-Tree查找周围邻居
            // 遍历获取周围的所有邻居
            NativeList<NeighbourInfo> neighbours = new NativeList<NeighbourInfo>(Allocator.Temp);
            for (int otherIndex = 0; otherIndex < agents.Length; otherIndex++)
            {
                if(otherIndex == agentIndex) continue;
                
                float2 otherPosition = ORCAUtils.ToPlane(transforms[otherIndex].Position);
                float distanceSq = math.lengthsq(position - otherPosition);

                neighbours.Add(new NeighbourInfo
                {
                    Index = otherIndex,
                    DistanceSq = distanceSq,
                });
            }
            
            // 按距离排序
            for (int i = 1; i < neighbours.Length; i++)
            {
                NeighbourInfo value = neighbours[i];
                int j = i - 1;

                while (j >= 0 && neighbours[j].DistanceSq > value.DistanceSq)
                {
                    neighbours[j + 1] = neighbours[j];
                    j--;
                }

                neighbours[j + 1] = value;
            }
            
            // 粗略计算邻居数量，暂时采用的是全部遍历，而不是KD-Tree获取邻居节点
            // 省去部分计算量
            int neighbourCount = math.min(8, neighbours.Length);
            for (int i = 0; i < neighbourCount; i++)
            {
                ASAgent otherAgent = agents[neighbours[i].Index];
                float2 otherPosition = ORCAUtils.ToPlane(transforms[neighbours[i].Index].Position);
                float2 otherVelocity = ORCAUtils.ToPlane(otherAgent.CurrentVelocity);
                
                // 以当前Agent为坐标中心，计算另一个Agent的相对位置，以及当前Agent的相对速度
                // 计算相对速度是否会落在速度障碍截锥范围内，以判断是否会碰撞
                float2 relativePosition = otherPosition - position; // 相对位置（另一个Agent的相对坐标点）
                float2 relativeVelocity = velocity - otherVelocity;
                float distSq = math.lengthsq(relativePosition);
                float combinedRadius = agent.Radius + otherAgent.Radius;    // 叠加的碰撞半径
                float combinedRadiusSq = combinedRadius * combinedRadius;  // 使用平方数，避免开方计算开销
                
                ASORCALine line = new ASORCALine();
                float2 u;   // 避障最小速度

                if (distSq > combinedRadiusSq)
                {
                    // 速度空间下的圆半径
                    float circleRadius = combinedRadius / timeHorizon;
                    float circleRadiusSq = circleRadius * circleRadius;
                    
                    // Agent未重叠，构造未来timeHorizon秒内的速度障碍截锥
                    // w = 相对速度在距离速度障碍边界还差多少
                    // relativePosition / timeHorizon 即在timeHorizon时间内，要两个Agent重叠所需要的速度
                    // relativeVelocity 减去这个速度，就是当前速度距离VO（速度障碍）的距离
                    float2 w = relativeVelocity - relativePosition / timeHorizon;
                    float wLengthSq = math.lengthsq(w);
                    // 计算 w 在相对位置上的投影，为正则说明已经进入扇形截面后边，为负则说明还未进入
                    float dotProduct = math.dot(w, relativePosition);

                    if (dotProduct < 0 && dotProduct * dotProduct > combinedRadiusSq * wLengthSq)
                    {
                        // 相对速度指向扇形区域，沿圆的外法线推出
                        if (wLengthSq > circleRadiusSq)
                        {
                            // w 未进入VO前半扇形区域，不构造ORCA约束线
                            continue;
                        }
                        
                        float wLength = math.sqrt(wLengthSq);
                        float2 unitW = ORCAUtils.SafeNormalize(w);
                        
                        // 计算在timeHorizon内离开VO扇形所需的速度
                        // 减去w的速度大小，最终得到的就是u的速度大小
                        u = (circleRadius - wLength) * unitW;
                        
                        // 计算 ORCA 约束线的方向（必须垂直于 u 向量，即圆弧的切线方向）
                        // 常用标准：将法线向量 unitW 顺时针旋转 90 度 -> (y, -x)
                        float2 lineDir = new float2(unitW.y, -unitW.x);
                        line.Direction = ORCAUtils.ToWorld(lineDir);
                    }
                    else
                    {
                        // 相对速度落在截锥两条切线附近
    
                        // 计算切线长度
                        float leg = math.sqrt(math.max(distSq - combinedRadiusSq, 0f));

                        // 判断相对速度偏向左侧还是右侧
                        float determinant = ORCAUtils.Cross(relativePosition, w);
                        float2 direction;
                        if (determinant > 0f)
                        {
                            // 靠近左侧切线，计算左侧切线的单位方向向量
                            direction = new float2(
                                relativePosition.x * leg - relativePosition.y * combinedRadius,
                                relativePosition.x * combinedRadius + relativePosition.y * leg) / distSq;

                            // 【安全检查】如果相对速度已经在左侧切线的外面（安全侧），则跳过
                            // 在这里，如果叉乘大于 0，说明相对速度在左切线的左边，即 VO 甜筒外面
                            if (ORCAUtils.Cross(relativeVelocity, direction) > 0f)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            // 靠近右侧切线，计算右侧切线的单位方向向量
                            direction = -new float2(
                                relativePosition.x * leg + relativePosition.y * combinedRadius,
                                -relativePosition.x * combinedRadius + relativePosition.y * leg) / distSq;

                            // 【安全检查】如果相对速度已经在右侧切线的外面（安全侧），则跳过
                            // 在这里，如果叉乘小于 0，说明相对速度在右切线的右边，即 VO 甜筒外面
                            if (ORCAUtils.Cross(relativeVelocity, direction) < 0f)
                            {
                                continue;
                            }
                        }

                        // 只有沦陷在 VO 锥体内部的危险速度，才需要计算 u 并构造约束线
                        float dotProduct2 = math.dot(relativeVelocity, direction);
                        u = dotProduct2 * direction - relativeVelocity;
    
                        // 赋值约束线方向（转换到世界坐标），由于u垂直于切线，因此切线就是约束线
                        line.Direction = ORCAUtils.ToWorld(direction);
                    }
                }
                else
                {
                    // Agent已经重叠，直接处于扇形截面区域，使用当前帧时间做分离
                    float2 w = relativeVelocity - relativePosition / dt;
                    float wLength = math.length(w);
                    float2 uintW = ORCAUtils.SafeNormalize(w);
                    float circleRadius = combinedRadius / dt;
                    
                    // 休整速度u与ORCA约束线求解
                    u = (circleRadius - wLength) * uintW;
                    float2 lineDir = new float2(uintW.y, -uintW.x);
                    line.Direction = ORCAUtils.ToWorld(lineDir);
                }
                
                // 各占50%责任
                // 将VO空间（相对速度）转换为绝对速度空间
                // 便于后续线性规划求解
                line.Point = ORCAUtils.ToWorld(velocity + u * 0.5f);
                orcaLines.Add(line);
            }

            neighbours.Dispose();
        }

        /// <summary>
        /// 线性规划子问题：在指定约束线上，找到满足满足约束且离目标速度最近的点
        /// </summary>
        private bool LinearProgram1(NativeArray<ASORCALine> lines, int lineIndex, float maxSpeed,
            float2 preferredVelocity, out float2 result)
        {
            result = float2.zero;
            
            // 转换到二维平面
            ASORCALine line = lines[lineIndex];
            float2 point = ORCAUtils.ToPlane(line.Point);
            float2 direction = ORCAUtils.ToPlane(line.Direction);
            
            // 约束线可用参数方程标识 V(t) = {point} + t · {direction}
            // 要让速度落在最大速度圆内，需要满足 |V(t)^2| <= maxSpeed^2
            // 化简 t^2 + 2({point} · {direction})t + ({point}^2 - {maxSpeed}^2) <= 0
            // 其中 b = point · direction
            //     c = point^2 - maxSpeed^2
            float b = math.dot(point, direction);
            float c = math.lengthsq(point) - maxSpeed * maxSpeed;
            
            // 根据一元二次方程求根公式
            // 得到解为 t = -b ± √{b^2 - c}
            // 因此判断是否有解，可以判断 b^2 - c 是否小于 0
            float discriminant = b * b - c;

            // b^2 - c 小于0，无解
            if (discriminant < 0) return false;
            
            // 根据求根公式计算线与最大速度圆的交点
            // 顺着箭头走是tRight，反之是tLeft
            float sqrtDiscriminant = math.sqrt(discriminant);
            float tLeft = -b - sqrtDiscriminant;    // 根1
            float tRight = -b + sqrtDiscriminant;   // 根2

            for (int i = 0; i < lineIndex; i++)
            {
                ASORCALine other = lines[i];
                
                float2 otherPoint = ORCAUtils.ToPlane(other.Point);
                float2 otherDir = ORCAUtils.ToPlane(other.Direction);
                
                // 与其他约束线求交
                // 让当前线上的可行区间[tLeft, tRight]收缩以符合其他约束线
                float determinant = ORCAUtils.Cross(direction, otherDir);
                float numerator = ORCAUtils.Cross(otherDir, point - otherPoint);

                // 两线平行
                if (math.abs(determinant) < EPSILON)
                {
                    // 判断当前线是否落在其他约束线的安全半平面范围内，这样才能构造一个封闭区域
                    // 若不在，若无法构成封闭区域，无解
                    if (numerator < 0) return false;
                    
                    continue;
                }
                
                // 计算交点位置（两条线在平面上的交点公式--克莱姆法则）
                float t = numerator / determinant;

                // 约束线默认右边为可行区域
                // 当前线在旧约束线的顺时针方向，根据当前线的指向，因此收缩右边界
                // 反之收缩左边界
                if (determinant > 0) tRight = math.min(tRight, t);
                else tLeft = math.max(tLeft, t);

                // 收缩边界越界，无解
                if (tLeft > tRight) return false;
            }
            
            // 计算期望速度在当前方向上的投影
            float optimalT = math.dot(direction, preferredVelocity - point);
            // 强制限制速度刻度到安全范围
            optimalT = math.clamp(optimalT, tLeft, tRight);
            
            // 以当前约束线为起点，沿期望方向能够到达的最远的点
            // 由于构建ORCA时每个Agent各占50%，计算了50%的u
            // 因此整体空间从相对速度转换为了绝对速度，此时返回的点就是当前Agent的安全速度
            result = point + optimalT * direction;

            return true;
        }
        
        /// <summary>
        /// 第二阶段线性规划，统筹管理所有约束线
        /// </summary>
        private int LinearProgram2(DynamicBuffer<ASORCALine> lines, float maxSpeed,
            float2 preferredVelocity, out float2 result)
        {
            result = preferredVelocity;
            
            // 初始速度裁剪，防止期望速度超过Agent最大速度
            float speedSq = math.lengthsq(result);
            if (speedSq > maxSpeed * maxSpeed)
            {
                result = math.normalize(result) * maxSpeed;
            }

            // 遍历每一条约束线
            for (int i = 0; i < lines.Length; i++)
            {
                float2 point = ORCAUtils.ToPlane(lines[i].Point);
                float2 dir = math.normalize(ORCAUtils.ToPlane(lines[i].Direction));

                // 当前速度是否违反当前约束
                // 若没有违反则可以直接跳过，否则进行速度修正
                float determinant = ORCAUtils.Cross(dir, result - point);
                if (determinant >= 0f) continue;

                // 保存旧结果
                float2 tempResult = result;
                NativeArray<ASORCALine> tempLines = new NativeArray<ASORCALine>(lines.Length, Allocator.Temp);
                for (int j = 0; j < lines.Length; j++)
                {
                    tempLines[j] = lines[j];
                }
                
                // 进行增量检测
                // 如果新加入的约束线，当前的result仍然在其右侧，那么这条线对Agent没有任何影响，继续检测下一条线
                // 在第i条线上做一维线性规划，不断调整速度以适配前i条约束线
                bool success = LinearProgram1(tempLines, i, maxSpeed, preferredVelocity, out result);
                tempLines.Dispose();

                // 求解失败，返回冲突的线的索引，并回退上一个能够安全运行的速度
                if (!success)
                {
                    result = tempResult;
                    return i;
                }
            }

            return lines.Length;
        }
        
        /// <summary>
        /// 计算两条线的交点
        /// </summary>
        private float2 ComputeIntersection(ASORCALine line1, ASORCALine line2)
        {
            float2 p1 = ORCAUtils.ToPlane(line1.Point);
            float2 d1 = ORCAUtils.ToPlane(line1.Direction);

            float2 p2 = ORCAUtils.ToPlane(line2.Point);
            float2 d2 = ORCAUtils.ToPlane(line2.Direction);

            float determinant = ORCAUtils.Cross(d1, d2);
            float t = ORCAUtils.Cross(d2, p1 - p2) / determinant;

            return p1 + t * d1;
        }
        
        private bool LinearProgram2Projected(NativeArray<ASORCALine> lines, float maxSpeed,
            float2 optVelocity, out float2 result)
        {
            result = optVelocity * maxSpeed;

            for (int i = 0; i < lines.Length; i++)
            {
                float2 point = ORCAUtils.ToPlane(lines[i].Point);
                float2 dir = ORCAUtils.ToPlane(lines[i].Direction);

                if (ORCAUtils.Cross(dir, result - point) >= 0f)
                {
                    continue;
                }

                float2 tempResult = result;
                if (!LinearProgram1(lines, i, maxSpeed, optVelocity * maxSpeed, out result))
                {
                    result = tempResult;
                    return false;
                }
            }
            return true;
        }
        
        private void LinearProgram3(DynamicBuffer<ASORCALine> lines, int beginLine, 
            float maxSpeed, ref float2 result)
        {
            float distance = 0f;

            NativeList<ASORCALine> projLines = new NativeList<ASORCALine>(Allocator.Temp);

            for (int i = beginLine; i < lines.Length; i++)
            {
                float2 pointI = ORCAUtils.ToPlane(lines[i].Point);
                float2 dirI = ORCAUtils.ToPlane(lines[i].Direction);
                float currentDistance = ORCAUtils.Cross(dirI, pointI - result);

                if (currentDistance <= distance)
                {
                    continue;
                }

                projLines.Clear();
                for (int j = 0; j < beginLine; j++)
                {
                    projLines.Add(lines[j]);
                }

                for (int j = beginLine; j < i; j++)
                {
                    ASORCALine projectedLine;

                    float2 pointJ = ORCAUtils.ToPlane(lines[j].Point);
                    float2 dirJ = ORCAUtils.ToPlane(lines[j].Direction);
                    float determinant = ORCAUtils.Cross(dirI, dirJ);
                    if (math.abs(determinant) < EPSILON)
                    {
                        if (math.dot(dirI, dirJ) > 0f)
                        {
                            continue;
                        }

                        float2 midpoint = (pointI + pointJ) * 0.5f;

                        projectedLine.Point = ORCAUtils.ToWorld(midpoint);
                    }
                    else
                    {
                        float2 intersection = ComputeIntersection(lines[i], lines[j]);
                        projectedLine.Point = ORCAUtils.ToWorld(intersection);
                    }

                    float2 direction = ORCAUtils.SafeNormalize(dirJ - dirI);
                    projectedLine.Direction = ORCAUtils.ToWorld(direction);
                    projLines.Add(projectedLine);
                }

                NativeArray<ASORCALine> tempLines = new NativeArray<ASORCALine>(projLines.Length, Allocator.Temp);

                for (int k = 0; k < projLines.Length; k++)
                { 
                    tempLines[k] = projLines[k];
                }

                float2 optVelocity = new float2(-dirI.y, dirI.x);

                LinearProgram2Projected(tempLines, maxSpeed, optVelocity, out result);
                tempLines.Dispose();
                distance = ORCAUtils.Cross(dirI, pointI - result);
            }

            projLines.Dispose();
        }
    }
}