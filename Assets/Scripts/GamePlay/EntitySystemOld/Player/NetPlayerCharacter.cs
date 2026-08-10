namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 网络玩家角色模拟本体，网络运行位置由外部Controller决定
    /// </summary>
    public class NetPlayerCharacter : NetEntityCharacter<NetPlayerSnapshot>, IPlayerCharacter
    {
        #region 快照管理

        public override NetPlayerSnapshot CaptureSnapshot(uint snapshotTick, uint lastProcessedInputTick = 0)
        {
            return new NetPlayerSnapshot
            {
                EntityId = EntityId,
                SnapshotTick = snapshotTick,
                LastProcessedInputTick = lastProcessedInputTick,
                Position = transform.position,
                Rotation = transform.eulerAngles
            };
        }

        #endregion
        
        #region 实体操控

        public void PrimaryAttack()
        {
            // TODO: 先保证最小同步原型，暂时不做
        }

        public void StopPrimaryAttack()
        {
            // TODO: 先保证最小同步原型，暂时不做
        }

        public void SpecialAttack()
        {
            // TODO: 先保证最小同步原型，暂时不做
        }

        public void StopSpecialAttack()
        {
            // TODO: 先保证最小同步原型，暂时不做
        }

        public void SpecialAction()
        {
            // TODO: 先保证最小同步原型，暂时不做
        }

        public void StopSpecialAction()
        {
            // TODO: 先保证最小同步原型，暂时不做
        }

        public void Interact()
        {
            // TODO: 先保证最小同步原型，暂时不做
        }

        public void StopInteract()
        {
            // TODO: 先保证最小同步原型，暂时不做
        }

        #endregion
    }
}
