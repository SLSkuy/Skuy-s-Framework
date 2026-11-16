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
            public EntityCommandFlags command;
            public uint targetNodeId;
        }

        /// <summary>验证配置并生成角色独立的运行时图。</summary>
        public CombatGraph CreateGraph()
        {
            if (!skillTable) throw new InvalidOperationException($"{name} 未配置 SkillTable");
            var skills = new HashSet<uint>();
            foreach (var skill in skillTable.skills ?? Array.Empty<SkillConfig>())
            {
                if (skill != null) skills.Add(skill.skillId);
            }
            var graph = new CombatGraph { rootNode = new CombatGraphRootNode(), nodes = new CombatGraphNode[nodes.Count] };
            var lookup = new Dictionary<uint, CombatGraphNode>();
            for (var i = 0; i < nodes.Count; i++)
            {
                var data = nodes[i];
                if (data.nodeId == 0 || !skills.Contains(data.skillId))
                    throw new InvalidOperationException($"{name} 节点 {data.nodeId} 的技能或节点 ID 非法");
                var node = new CombatGraphNode(data.nodeId, data.skillId);
                lookup.Add(data.nodeId, node);
                graph.nodes[i] = node;
            }
            BindTransitions(graph.rootNode, rootTransitions, lookup);
            for (var i = 0; i < nodes.Count; i++) BindTransitions(graph.nodes[i], nodes[i].transitions, lookup);
            return graph;
        }

        private static void BindTransitions(ICombatGraphNode source, List<TransitionData> transitions,
            Dictionary<uint, CombatGraphNode> lookup)
        {
            foreach (var transition in transitions)
            {
                var command = (uint)transition.command;
                if (command == 0 || (command & (command - 1)) != 0 || !Enum.IsDefined(typeof(EntityCommandFlags), transition.command))
                    throw new InvalidOperationException("连招转换必须使用单一合法输入命令");
                source.AddTransition(transition.command, lookup[transition.targetNodeId]);
            }
        }
    }
}