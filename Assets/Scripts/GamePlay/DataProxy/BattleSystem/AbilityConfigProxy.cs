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
        
        private readonly Dictionary<uint, BaseSkill> _skills = new();
        private AssetHandle _handle;
        
        public bool TryGetSkillConfig(uint skillId, out BaseSkill skillConfig)
        {
            return _skills.TryGetValue(skillId, out skillConfig);
        }
        
        public override void Load()
        {
            _handle = Global.Load<SkillTable>(DataPath);
            SkillTable table = _handle.AssetObject as SkillTable;
            foreach (BaseSkill skill in table.skills)
            {
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