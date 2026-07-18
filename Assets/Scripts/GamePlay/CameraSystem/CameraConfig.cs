using System;
using System.Collections.Generic;
using Framework;
using UnityEngine;
using Unity.Cinemachine;

namespace GamePlay.CameraSystem
{
    /// <summary>
    /// 摄像机设置实例
    /// </summary>
    [Serializable]
    public struct CameraEntry
    {
        public GameCameraState state;
        public CinemachineCamera camera;
        public int priority;
    }
    
    [CreateAssetMenu(fileName = "CameraConfig", menuName = "GamePlay/CameraConfig")]
    public class CameraConfig : ScriptableObjectSingleton<CameraConfig>
    {
        [Header("摄像机设置")]
        public List<CameraEntry> cameras;
    }
}
