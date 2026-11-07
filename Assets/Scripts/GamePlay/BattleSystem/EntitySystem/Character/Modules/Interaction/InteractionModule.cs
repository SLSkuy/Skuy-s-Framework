using UnityEngine;

namespace GamePlay.BattleSystem
{
    public class InteractionModule : EntityModuleBase
    {
        private Transform _orientation;
        
        #region 属性
        public override ModuleType Type => ModuleType.Interaction;
        #endregion
        
        protected override void OnBound(EntityCharacter target)
        {
            // 获取当前视角
            _orientation = target.Context.Get<ViewModule>().Orientation;
        }

        /// <summary>
        /// 以当前视角朝向，尝试与可交互物品进行一次交互
        /// </summary>
        public void Interact()
        {
            
        }
        
        /// <summary>
        /// 清理模块的还池运行状态。
        /// </summary>
        public override void Reset()
        {
            // 交互模块没有跨次借出的运行状态，保留绑定的视角引用。
        }
    }
}