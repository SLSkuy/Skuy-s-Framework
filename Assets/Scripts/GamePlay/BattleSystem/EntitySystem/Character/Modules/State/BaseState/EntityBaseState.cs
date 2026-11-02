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
        protected readonly AbilityModule Ability;

        #region 基础状态属性
        protected CharacterConfig Config => Context.Config;
        
        protected Vector2 LastMoveInput => Context.LastMoveInput;
        protected Vector2 LastAimInput => Context.LastAimInput;
        
        protected float DesiredSpeed => Context.DesiredSpeed;
        protected bool JumpRequest => Context.JumpRequest;
        
        protected bool IsGrounded => Movement.IsGrounded;
        protected bool IsRunning => Context.IsRunning;
        #endregion

        protected EntityBaseState(EntityContext context) : base(context.Get<EntityStateMachineModule>().StateMachine)   // 通用基类只看到状态机
        {
            Context = context;
            
            Movement = context.Get<MovementModule>();
            View = context.Get<ViewModule>();
            Ability = context.Get<AbilityModule>();
        }

        /// <summary>
        /// 检测是否转换为技能状态
        /// </summary>
        protected bool CheckActionTransition()
        {
            if (Context.SkillRequest)
            {
                // TODO: 技能ID站位，后续使用技能槽读取获取技能信息
                if (Ability.TryCastSkill(1))
                {
                    _stateMachine.ChangeState(EntityState.ACTION);
                    return true;
                }
            }

            return false;
        }
    }
}
