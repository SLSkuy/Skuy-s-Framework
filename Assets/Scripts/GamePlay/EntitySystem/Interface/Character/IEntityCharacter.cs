using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体接口，定义实体能够进行哪些操作行为
    /// </summary>
    public interface IEntityCharacter
    {
        /// <summary>
        /// 朝指定方向移动
        /// </summary>
        /// <param name="dir">移动方向</param>
        /// <param name="deltaTime">移动步长</param>
        void Move(Vector2 dir, float deltaTime);

        /// <summary>
        /// 开始疾跑
        /// </summary>
        void StartSprint();

        /// <summary>
        /// 结束疾跑
        /// </summary>
        void StopSprint();

        /// <summary>
        /// 朝当前移动方向开始冲刺
        /// </summary>
        void Dash();

        /// <summary>
        /// 朝设定位置旋转
        /// </summary>
        /// <param name="dir">目标朝向</param>
        void Rotate(Vector3 dir);
    }
}