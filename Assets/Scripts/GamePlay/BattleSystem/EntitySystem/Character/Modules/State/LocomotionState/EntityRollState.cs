namespace GamePlay.BattleSystem
{
    public class EntityRollState : EntityLocomotionState
    {
        public override EntityState StateKey => EntityState.ROLL;
        
        public EntityRollState(EntityContext context) : base(context) { }

        #region 状态控制

        public override void Enter()
        {
            
        }

        protected override void CheckStateChange()
        {
            if (CheckGroundTransitions()) return;
        }

        #endregion
    }
}