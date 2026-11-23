using UnityEngine;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 相对于动作起点的水平运动轨迹，时间为秒，位置为米，偏航为累计角度
    /// </summary>
    [CreateAssetMenu(fileName = "AnimationMotionData", menuName = "GamePlay/AnimationMotionData")]
    public class BakedAnimMotionData : ScriptableObject
    {
        [Header("数据源")]
        public AnimationClip sourceClip;
        public Avatar sourceAvatar;
        
        [Header("烘培动画信息")]
        [Tooltip("动画持续时间")]public float duration;
        [Tooltip("平面移动距离")]public float planarDistance;
        [Tooltip("动画采样率")]public uint sampleRate;
        
        [Header("动画曲线")]
        public AnimationCurve positionX = new();
        public AnimationCurve positionY = new();
        public AnimationCurve positionZ = new();
        public AnimationCurve yaw = new();

        #region 属性
        public float AveragePlanarSpeed => duration > 0f ? planarDistance / duration : 0f;
        public float TotalYaw => yaw.Evaluate(duration) - yaw.Evaluate(0f);
        public float AverageYawSpeed => duration > 0f ? TotalYaw / duration : 0f;
        #endregion

        #region 根运动查询

        /// <summary>
        /// 查询指定Tick时对应根运动位移
        /// </summary>
        public Vector3 EvaluatePosition(uint tick)
        {
            float tickTime = (float)tick / sampleRate;
            tickTime = Mathf.Clamp(tickTime, 0f, duration);
            return new Vector3(positionX.Evaluate(tickTime), positionY.Evaluate(tickTime), positionZ.Evaluate(tickTime));
        }
        
        /// <summary>
        /// 查询指定时间对应根运动位移
        /// </summary>
        public Vector3 EvaluatePosition(float time)
        {
            time = Mathf.Clamp(time, 0f, duration);
            return new Vector3(positionX.Evaluate(time), positionY.Evaluate(time), positionZ.Evaluate(time));
        }

        /// <summary>
        /// 查询指定Tick时对应根运动XZ轴位移
        /// </summary>
        public Vector2 EvaluatePositionXZ(uint tick)
        {
            float tickTime = (float)tick / sampleRate;
            tickTime = Mathf.Clamp(tickTime, 0f, duration);
            return new Vector2(positionX.Evaluate(tickTime), positionZ.Evaluate(tickTime));
        }
        
        public Vector2 EvaluatePositionXZ(float time)
        {
            time = Mathf.Clamp(time, 0f, duration);
            return new Vector2(positionX.Evaluate(time), positionZ.Evaluate(time));
        }

        /// <summary>
        /// 查询指定Tick时对应根运动Y轴位移
        /// </summary>
        public float EvaluatePositionY(uint tick)
        {
            float tickTime = (float)tick / sampleRate;
            tickTime = Mathf.Clamp(tickTime, 0f, duration);
            float posY = positionY.Evaluate(tickTime);
            return posY;
        }
        
        public float EvaluatePositionY(float time)
        {
            float posY = positionY.Evaluate(time);
            return posY;
        }

        public float EvaluateYaw(uint tick)
        {
            float tickTime = (float)tick / sampleRate;
            tickTime = Mathf.Clamp(tickTime, 0f, duration);
            return yaw.Evaluate(tickTime);
        }

        /// <summary>
        /// 查询指定Tick范围类的位移变化
        /// </summary>
        public Vector3 EvaluateDeltaPos(uint fromTick, uint toTick)
        {
            return EvaluatePosition(toTick) - EvaluatePosition(fromTick);
        }

        public Vector2 EvaluateDeltaPosXZ(uint fromTick, uint toTick)
        {
            return EvaluatePositionXZ(toTick) - EvaluatePositionXZ(fromTick);
        }

        public float EvaluateDeltaPosY(uint fromTick, uint toTick)
        {
            return EvaluatePositionY(toTick) - EvaluatePositionY(fromTick);
        }

        public float EvaluateDeltaYaw(uint fromTick, uint toTick)
        {
            return EvaluateYaw(toTick) - EvaluateYaw(fromTick);
        }

        /// <summary>
        /// 查询某一Tick相对于之前的Tick位置变化
        /// </summary>
        public Vector3 EvaluateTickDeltaPos(uint tick)
        {
            if (tick == 0) return Vector3.zero;
            return EvaluateDeltaPos(tick - 1, tick);
        }
        
        public Vector2 EvaluateTickDeltaPosXZ(uint tick)
        {
            if (tick == 0) return Vector2.zero;
            return EvaluateDeltaPosXZ(tick - 1, tick);
        }

        #endregion
    }
}
