using Framework;
using UnityEngine;

namespace GamePlay.MultiPlaySystem
{
    /// <summary>
    /// 角色输入合法性校验：有限数值与向量模长上限。
    /// </summary>
    public static class CharacterInputValidator
    {
        /// <summary>
        /// 判断输入向量是否有限且未超过配置模长。
        /// </summary>
        public static bool IsValid(in InputState state, float maxInputVectorMagnitude)
        {
            if (!IsFinite(state.MoveInput) || !IsFinite(state.AimInput)) return false;
            float maxMagnitudeSquared = maxInputVectorMagnitude * maxInputVectorMagnitude;
            return state.MoveInput.sqrMagnitude <= maxMagnitudeSquared &&
                state.AimInput.sqrMagnitude <= maxMagnitudeSquared;
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
