using UnityEngine;

namespace GamePlay.Procedure
{
    /// <summary>
    /// 对局常驻调试 HUD：名册、加入方离开、房主解散。只对流程核心发意图。
    /// </summary>
    public sealed class MatchDebugHud : MonoBehaviour
    {
        private void OnGUI()
        {
            ProcedureCore procedures = ProcedureCore.Instance;
            if (procedures == null || procedures.CurrentProcedure != GameProcedure.Match)
            {
                return;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            GUI.depth = -1000;
            uint[] roster = procedures.GetRosterPlayerIds();
            GUILayout.BeginArea(new Rect(12f, 12f, 320f, 200f), GUI.skin.box);
            GUILayout.Label("对局名册");
            if (roster.Length == 0)
            {
                GUILayout.Label("(空)");
            }
            else
            {
                for (int i = 0; i < roster.Length; i++)
                {
                    GUILayout.Label($"玩家 {roster[i]}");
                }
            }

            string leaveLabel = procedures.SessionIsHost ? "解散" : "离开";
            bool f5Pressed = Event.current is { type: EventType.KeyDown, keyCode: KeyCode.F5 };
            if (GUILayout.Button(leaveLabel) || f5Pressed)
            {
                procedures.LeaveSession();
            }

            GUILayout.EndArea();
        }
    }
}
