using UnityEngine;

namespace GamePlay.BattleSystem
{
    public class InteractionModule : EntityModuleBase
    {
        private Transform _orientation;
        
        #region 属性
        public override ModuleType ModuleType => ModuleType.Interaction;
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
    }
}