using System.Collections.Generic;
using Framework;
using GamePlay.BattleSystem;
using YooAsset;

namespace GamePlay.DataProxy
{
    /// <summary>
    /// 技能配置
    /// </summary>
    public class AbilityConfigProxy : BaseDataProxy
    {
        public override string DataPath => "Config_SkillTable";
        public override bool IsNeedSaveToLocal => false;
        
        private readonly Dictionary<uint, SkillConfig> _skills = new();
        private AssetHandle _handle;
        
        public bool TryGetSkillConfig(uint skillId, out SkillConfig skillConfig)
        {
            return _skills.TryGetValue(skillId, out skillConfig);
        }
        
        public override void Load()
        {
            _handle = Global.Load<SkillTable>(DataPath);
            SkillTable table = _handle.AssetObject as SkillTable;
            foreach (SkillConfig skill in table.skills)
            {
                // TODO: ========== DEBUG ==========
                if (skill.skillId == 1)
                {
                    skill.effects = new SkillEffect[]
                    {
                        new AnimationSkillEffect
                        {
                            clip = skill.clip,
                            startTick = 0,
                            durationTicks = skill.durationTicks,
                        }
                    };
                }
                // TODO: ========== DEBUG ==========
                
                _skills.Add(skill.skillId, skill);
            }
        }

        public override void Destroy()
        {
            _skills.Clear();
            _handle.Dispose();
        }
    }
}