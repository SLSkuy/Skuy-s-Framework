using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 基础网络实体状态定义
    /// </summary>
    public interface INetEntity<T>
    {
        /// <summary>
        /// 网络实体索引
        /// </summary>
        uint Index { get; set; }
        
        Vector3 LogicPosition { get; set; }
        Vector3 LogicRotation { get; set; }
        
        /// <summary>
        /// 应用当前快照状态
        /// </summary>
        /// <param name="snapshot"></param>
        void SetSnapshot(T snapshot);
        
        /// <summary>
        /// 获取当前快照状态
        /// </summary>
        T GetSnapshot();
    }
}