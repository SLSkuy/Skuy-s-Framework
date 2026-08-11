using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 移动能力模块，当前包装旧版 EntityMotor 运动执行能力。
    /// </summary>
    public class MovementModule : EntityModuleBase
    {
        private EntityMotor _motor;

        #region 属性
        public EntityMotor Motor => _motor;
        public bool HasMotor => _motor != null;
        #endregion

        #region 移动能力
        /// <summary>
        /// 绑定旧版运动执行器。
        /// </summary>
        public void BindMotor(EntityMotor motor)
        {
            _motor = motor;
        }

        /// <summary>
        /// 清理运动执行器。
        /// </summary>
        public void ClearMotor()
        {
            _motor = null;
        }
        #endregion
    }
}

