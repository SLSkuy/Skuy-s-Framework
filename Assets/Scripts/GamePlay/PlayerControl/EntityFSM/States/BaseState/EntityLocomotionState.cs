using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体运动状态基类，统一编排每帧的运动调用（旋转 + 位移 + 重力）。
    /// 物理状态（垂直速度、跳跃次数、冲刺计时、视角等）由 EntityMotor 集中持有跨状态共享，
    /// 避免每个状态实例各自保存副本导致切换时丢失。
    /// _locomotionSpeed 为状态目标速度，由各状态在 Enter 时设定，每状态独立。
    /// </summary>
    public abstract class EntityLocomotionState : EntityBaseState
    {
        /// <summary>
        /// 当前状态的目标移动速度，由派生状态在 Enter 时设置
        /// </summary>
        protected float _locomotionSpeed;

        protected EntityLocomotionState(EntityContext context) : base(context) { }

        #region 状态控制

        protected override void Tick(float dt)
        {
            Motor.Rotate(Context.LastAimInput, dt);
            Motor.Move(Context.LastMoveInput, _locomotionSpeed, dt);
        }

        #endregion
    }
}
