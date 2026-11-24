using System;
using System.Collections.Generic;
using Framework;
using UnityEngine;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 连招图资产。转换保存在各节点内，运行时创建角色独立实例
    /// TODO: 优化连招写入逻辑
    /// </summary>
    [CreateAssetMenu(menuName = "GamePlay/CombatGraphConfig")]
    public sealed class CombatGraphConfig : ScriptableObject
    {
        public SkillTable skillTable;
        public Vector2 rootPosition = new Vector2(40, 100);
        public List<TransitionData> rootTransitions = new();
        public List<NodeData> nodes = new();

        [Serializable]
        public sealed class NodeData
        {
            public uint nodeId;
            public uint skillId;
            public Vector2 position;
            public List<TransitionData> transitions = new();
        }

        [Serializable]
        public sealed class TransitionData
        {
            public InputCommandFlags command;
            public uint targetNodeId;
        }

        /// <summary>验证配置并生成角色独立的运行时图。</summary>
        public CombatGraph CreateGraph()
        {
            return null;
        }
    }
}