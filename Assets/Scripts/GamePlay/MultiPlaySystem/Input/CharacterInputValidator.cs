using Framework;
using UnityEngine;

namespace GamePlay.MultiPlaySystem
{
    /// <summary>
    /// 角色输入合法性校验：有限数值，并限制移动向量模长。
    /// </summary>
    public static class CharacterInputValidator
    {
        /// <summary>
        /// 校验有限数值并将移动向量限制在配置模长内。瞄准不参与模长否决，避免鼠标 delta 丢掉整包移动。
        /// </summary>
        public static bool TrySanitize(ref InputState state, float maxMoveMagnitude)
        {
            if (!IsFinite(state.MoveInput) || !IsFinite(state.AimInput)) return false;
            if (maxMoveMagnitude > 0f)
            {
                state.MoveInput = Vector2.ClampMagnitude(state.MoveInput, maxMoveMagnitude);
            }

            return true;
        }

        /// <summary>
        /// 判断二维向量分量是否有限。
        /// </summary>
        public static bool IsFinite(Vector2 value)
        {
            return float.IsFinite(value.x) && float.IsFinite(value.y);
        }
    }
}
