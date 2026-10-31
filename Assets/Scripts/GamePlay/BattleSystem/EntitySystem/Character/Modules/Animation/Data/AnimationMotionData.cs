using UnityEngine;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 相对于动作起点的水平运动轨迹，时间为秒，位置为米，偏航为累计角度
    /// </summary>
    [CreateAssetMenu(fileName = "AnimationMotionData", menuName = "GamePlay/AnimationMotionData")]
    public class AnimationMotionData : ScriptableObject
    {
        public AnimationClip sourceClip;
        public Avatar sourceAvatar;
        public float duration;
        public int sampleRate;
        public float planarDistance;
        public AnimationCurve positionX = new();
        public AnimationCurve positionZ = new();
        public AnimationCurve yaw = new();

        #region 属性
        public float AveragePlanarSpeed => duration > 0f ? planarDistance / duration : 0f;
        public float TotalYaw => yaw.Evaluate(duration) - yaw.Evaluate(0f);
        public float AverageYawSpeed => duration > 0f ? TotalYaw / duration : 0f;
        #endregion

        /// <summary>
        /// 查询动作起点坐标系中的累计水平位置
        /// </summary>
        public Vector3 EvaluatePosition(float time)
        {
            time = Mathf.Clamp(time, 0f, duration);
            return new Vector3(positionX.Evaluate(time), 0f, positionZ.Evaluate(time));
        }

        /// <summary>
        /// 查询一个周期内的运动差值，跨循环时由调用方分段查询
        /// </summary>
        public void EvaluateDelta(float fromTime, float toTime, out Vector3 localDelta, out float yawDelta)
        {
            fromTime = Mathf.Clamp(fromTime, 0f, duration);
            toTime = Mathf.Clamp(toTime, 0f, duration);
            localDelta = EvaluatePosition(toTime) - EvaluatePosition(fromTime);
            yawDelta = yaw.Evaluate(toTime) - yaw.Evaluate(fromTime);
        }
    }
}
