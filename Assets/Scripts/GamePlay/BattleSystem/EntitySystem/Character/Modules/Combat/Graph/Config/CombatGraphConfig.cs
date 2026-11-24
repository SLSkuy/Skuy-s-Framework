using System;
using System.Collections.Generic;
using Framework;
using UnityEngine;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 连招图资产。转换保存在各节点内，运行时创建角色独立实例
    /// 视图副本仅用于编辑器布局。
    /// </summary>
    [CreateAssetMenu(menuName = "GamePlay/CombatGraphConfig")]
    public sealed class CombatGraphConfig : ScriptableObject
    {
        public SkillTable skillTable;
        public Vector2 rootPosition = new Vector2(40, 100);
        public List<TransitionData> rootTransitions = new();
        public Vector2 anyPosition = new Vector2(40, 240);
        public List<TransitionData> anyTransitions = new();
        public List<NodeData> nodes = new();

        [Serializable]
        public sealed class NodeData
        {
            public uint nodeId;
            public uint skillId;
            public Vector2 position;
            public List<Vector2> copies = new();
            public List<TransitionData> transitions = new();
        }

        [Serializable]
        public sealed class TransitionData
        {
            public InputCommandFlags command;
            public uint targetNodeId;
            public InputCommandState commandState = InputCommandState.Pressed;
            public List<EntityStateTransitionCondition> stateConditions = new();
            public int sourceView;
            public int targetView;
        }

        /// <summary>验证配置并生成角色独立的运行时图。</summary>
        public CombatGraph CreateGraph()
        {
            var graph = new CombatGraph
            {
                rootNode = new CombatGraphRootNode(),
                anyNode = new CombatGraphAnyNode(),
                nodes = new CombatGraphNode[nodes.Count]
            };
            graph.anyNode.InitializeId(uint.MaxValue);
            var targets = new Dictionary<uint, CombatGraphNode>();
            for (int i = 0; i < nodes.Count; i++)
            {
                var data = nodes[i];
                if (data.nodeId == 0 || data.nodeId == uint.MaxValue)
                    throw new InvalidOperationException($"技能节点使用了保留 ID {data.nodeId}");
                var node = new CombatGraphNode();
                node.InitializeId(data.nodeId);
                node.InitializeSkill(data.skillId);
                graph.nodes[i] = node;
                targets.Add(data.nodeId, node);
            }
            Bind(graph.rootNode, rootTransitions);
            Bind(graph.anyNode, anyTransitions);
            foreach (var data in nodes) Bind(targets[data.nodeId], data.transitions);
            return graph;

            void Bind(ICombatGraphNode source, List<TransitionData> transitions)
            {
                foreach (var data in transitions)
                {
                    source.AddTransition(new CombatGraphTransition
                    {
                        targetNode = targets[data.targetNodeId],
                        commandFlag = data.command,
                        commandState = data.commandState,
                        conditions = data.stateConditions.ConvertAll<ITransitionCondition>(condition =>
                            new EntityStateTransitionCondition { allowedStates = (EntityState[])condition.allowedStates.Clone() }).ToArray()
                    });
                }
            }
        }
    }
}
