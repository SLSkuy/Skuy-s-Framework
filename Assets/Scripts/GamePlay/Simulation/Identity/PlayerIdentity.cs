using System;
using UnityEngine;

namespace GamePlay.Simulation
{
    /// <summary>
    /// 玩家对象身份识别器
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerIdentity : EntityIdentity
    {
        #region 属性
        public Guid PlayerID { get; private set; }
        #endregion

        public void BindPlayerID(Guid uuid)
        {
            PlayerID = uuid;
        }

        public override void Reset()
        {
            base.Reset();
            PlayerID = Guid.Empty;
        }
    }
}