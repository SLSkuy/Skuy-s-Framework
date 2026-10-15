using UnityEngine;

namespace GamePlay.Room
{
    [CreateAssetMenu(fileName = "RoomConfig", menuName = "GamePlay/RoomConfig")]
    public class RoomConfigSO : ScriptableObject
    {
        [Header("房间默认配置")]
        public bool acceptRemoteJoin = true;
        public int capacity = 4;
    }
}