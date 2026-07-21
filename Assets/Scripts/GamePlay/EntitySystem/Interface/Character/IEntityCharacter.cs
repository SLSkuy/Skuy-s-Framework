using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 网络实体角色接口，具备基础的移动能力
    /// </summary>
    public interface IEntityCharacter<T>  where T : struct, IEntitySnapshot
    {
        /// <summary>
        /// 朝指定方向移动
        /// </summary>
        /// <param name="dir">移动方向</param>
        /// <param name="deltaTime">移动步长</param>
        void Move(Vector2 dir, float deltaTime);

        /// <summary>
        /// 开始疾跑
        /// </summary>
        void StartSprint();

        /// <summary>
        /// 结束疾跑
        /// </summary>
        void StopSprint();

        /// <summary>
        /// 朝当前移动方向开始冲刺
        /// </summary>
        void Dash();

        /// <summary>
        /// 朝设定位置旋转
        /// </summary>
        /// <param name="dir">目标朝向</param>
        void Rotate(Vector3 dir);
        
        /// <summary>
        /// 直接应用快照状态
        /// </summary>
        void ApplySnapshot(in T snapshot);
        
        /// <summary>
        /// 插值应用快照状态
        /// </summary>
        void ApplyInterpolatedSnapshot(in T from, in T to, float t);
        
        /// <summary>
        /// 获取当前Tick对应的快照状态
        /// </summary>
        T CaptureSnapshot(uint snapshotTick, uint lastProcessedInputTick = 0);
    }
}