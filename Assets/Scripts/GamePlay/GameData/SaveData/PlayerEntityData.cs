using UnityEngine;

namespace GamePlay.GameData
{
    /// <summary>
    /// 玩家在这一局里的出场进度，只放在存档的 PlayerData 里
    /// </summary>
    public class PlayerEntityData
    {
        /// <summary>
        /// 上一次保存时玩家的位置
        /// </summary>
        public Vector3 position = Vector3.zero;

        /// <summary>
        /// 上一次保存时玩家的旋转
        /// </summary>
        public Quaternion rotation = Quaternion.identity;
    }
}
