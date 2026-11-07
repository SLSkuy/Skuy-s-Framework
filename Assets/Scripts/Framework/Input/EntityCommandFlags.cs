using System;

namespace Framework
{
    /// <summary>
    /// 实体控制命令类型枚举定义
    /// </summary>
    [Flags]
    public enum EntityCommandFlags : ushort
    {
        None = 0,
        
        /// <summary> 奔跑控制 </summary>
        Run = 1 << 0,
        /// <summary> 跳跃控制 </summary>
        Jump = 1 << 1,
        
        /// <summary> 键盘F键，交互控制 </summary>
        Interact = 1 << 2,
        
        /// <summary> 鼠标左键，普通攻击 </summary>
        PrimaryAttack = 1 << 3,
        /// <summary> 鼠标右键，特殊攻击 </summary>
        SpecialAttack = 1 << 4,
        /// <summary> 键盘R键，特殊操作 </summary>
        SpecialAction = 1 << 5,
        
        /// <summary> 翻滚控制 </summary>
        Roll = 1 << 6,
        /// <summary> 键盘Q键，技能1 </summary>
        SkillQ = 1 << 7, 
        /// <summary> 键盘E键，技能2 </summary>
        SkillE = 1 << 8,
        /// <summary> 键盘X键，技能3 </summary>
        SkillX = 1 << 9
    }
}