using Framework.StateMachine;
using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体最基础状态，持有 EntityContext，提供所有状态共有属性
    /// </summary>
    public abstract class EntityBaseState : ExtendableStateBase<uint>
    {
        protected readonly EntityContext Context;

        #region 状态属性
        protected EntityConfig Config => Context.Config;
        protected MovementModule Motor => Context.Motor;
        protected Vector2 LastMoveInput => Context.LastMoveInput;
        protected Vector2 LastAimInput => Context.LastAimInput;
        protected float LocomotionSpeed => Context.locomotionSpeed;
        protected bool IsGrounded => Context.IsGrounded;
        protected bool IsSprinting => Context.IsSprinting;   // Sprint 键按住（持续型）
        protected bool IsRunning => Context.IsRunning;         // 奔跑模式（walk/run toggle 持续型）
        protected bool RunToggleRequest => Context.RunToggleRequest;  // 奔跑 toggle 瞬时请求
        protected bool JumpRequest => Context.JumpRequest;
        #endregion

        protected EntityBaseState(EntityContext context) : base(context.StateMachine)   // 通用基类依旧只看到状态机
        {
            Context = context;
        }
    }
}
