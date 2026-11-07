using UnityEngine;

namespace GamePlay.BattleSystem
{
    [CreateAssetMenu(fileName = "CharacterConfig", menuName = "GamePlay/CharacterConfig")]
    public class CharacterConfig : ScriptableObject
    {
        [Header("基础移动属性")]
        public MovementConfig movementConfig = new MovementConfig();

        [Header("视角控制属性")]
        public ViewConfig viewConfig = new ViewConfig();
        
        [Header("动画属性")]
        public AnimationConfig animationConfig = new AnimationConfig();

        [Header("战斗输入映射")]
        public CombatConfig combatConfig = new CombatConfig();
    }
}
