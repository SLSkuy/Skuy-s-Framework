using Framework;
using UnityEngine;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 实体最基础状态，持有 EntityContext，提供所有状态共有属性
    /// </summary>
    public abstract class EntityBaseState : StateBase<EntityState>
    {
        protected readonly EntityContext Context;
        protected readonly MovementModule Movement;
        protected readonly ViewModule View;

        #region 状态属性
        protected CharacterConfig Config => Context.Config;
        
        protected Vector2 LastMoveInput => Context.LastMoveInput;
        protected Vector2 LastAimInput => Context.LastAimInput;
        
        protected float DesiredSpeed => Context.DesiredSpeed;
        
        protected bool IsGrounded => Movement.IsGrounded;
        protected bool IsSprinting => Context.IsSprinting;   // Sprint 键按住（持续型）
        protected bool IsRunning => Context.IsRunning;         // 奔跑模式（walk/run toggle 持续型）
        protected bool RunToggleRequest => Context.RunToggleRequest;  // 奔跑 toggle 瞬时请求
        protected bool JumpRequest => Context.JumpRequest;
        #endregion

        protected EntityBaseState(EntityContext context) : base(context.Get<EntityStateMachineModule>().StateMachine)   // 通用基类只看到状态机
        {
            Context = context;
            
            Movement = context.Get<MovementModule>();
            View = context.Get<ViewModule>();
        }
    }
}
