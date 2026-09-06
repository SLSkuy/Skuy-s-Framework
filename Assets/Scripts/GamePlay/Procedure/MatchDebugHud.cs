using System.Collections.Generic;
using UnityEngine;

namespace GamePlay.Procedure
{
    /// <summary>
    /// 对局常驻调试 HUD：名册与解散。只对流程核心发意图。
    /// </summary>
    public sealed class MatchDebugHud : MonoBehaviour
    {
        private readonly List<uint> _roster = new();

        private void OnGUI()
        {
            ProcedureCore procedures = ProcedureCore.Instance;
            if (procedures == null || procedures.CurrentProcedure != GameProcedure.Match)
            {
                return;
            }

            procedures.CopyRosterPlayerIds(_roster);
            GUILayout.BeginArea(new Rect(12f, 12f, 280f, 160f), GUI.skin.box);
            GUILayout.Label("名册");
            if (_roster.Count == 0)
            {
                GUILayout.Label("(空)");
            }
            else
            {
                for (int i = 0; i < _roster.Count; i++)
                {
                    GUILayout.Label($"玩家 {_roster[i]}");
                }
            }

            if (GUILayout.Button("解散") || Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                procedures.LeaveSession();
            }

            GUILayout.EndArea();
        }
    }
}
