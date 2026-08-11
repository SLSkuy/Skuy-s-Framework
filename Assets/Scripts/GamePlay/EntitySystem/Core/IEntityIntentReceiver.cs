using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体控制意图接收接口，用于隔离输入来源与实体实现
    /// 只处理最基础的实体控制意图
    /// </summary>
    public interface IEntityIntentReceiver
    {
        /// <summary>
        /// 写入移动意图。
        /// </summary>
        void Move(Vector2 dir);

        /// <summary>
        /// 写入瞄准意图。
        /// </summary>
        void Aim(Vector2 dir);

        /// <summary>
        /// 请求跳跃。
        /// </summary>
        void Jump();

        /// <summary>
        /// 开始疾跑。
        /// </summary>
        void StartSprint();

        /// <summary>
        /// 停止疾跑。
        /// </summary>
        void StopSprint();

        /// <summary>
        /// 切换行走/奔跑模式。
        /// </summary>
        void ToggleRun();
    }
}

