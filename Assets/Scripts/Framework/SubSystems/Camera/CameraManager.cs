using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 摄像机管理器，管理当前玩家摄像机状态
    /// </summary>
    public class CameraManager : SubSystemBase
    {
        public override int Priority => (int)SubSystemPriority.CameraManager;
        private readonly Dictionary<GameCameraState, CameraEntry> _cameraMap = new();
        private GameCameraState _currentState;
        private Transform _cameraRoot;
        private bool _hasInitializedState;
        
        public override void Init()
        {
            _cameraRoot = new GameObject("[CameraRoot]").transform;
            Object.DontDestroyOnLoad(_cameraRoot);
            
            // 读取摄像机配置
            CameraConfig config = CameraConfig.Instance;
            foreach (var entry in config.cameras)
            {
                GameObject obj = Object.Instantiate(entry.camera.gameObject, _cameraRoot);
                CinemachineCamera cam = obj.GetComponent<CinemachineCamera>();
                
                CameraEntry runtimeEntry = new()
                {
                    state = entry.state,
                    camera = cam,
                    priority = entry.priority
                };

                _cameraMap.Add(entry.state, runtimeEntry);
            }
            
            SwitchTo(GameCameraState.Normal);
            Debug.Log("[CameraManager]初始化");
        }
        
        /// <summary>
        /// 设置摄像机跟随目标
        /// </summary>
        public void SetTarget(Transform target)
        {
            foreach (var entry in _cameraMap.Values)
            {
                entry.camera.Follow = target;
            }
        }

        /// <summary>
        /// 切换摄像机状态
        /// </summary>
        /// <param name="state">摄像机目标状态</param>
        public void SwitchTo(GameCameraState state)
        {
            if (_currentState == state&&_hasInitializedState) return;
            _hasInitializedState = true;
            
            if (!_cameraMap.ContainsKey(state))
            {
                Debug.LogWarning($"[CameraManager] Camera state {state} not found!");
                return;
            }

            _currentState = state;

            foreach (var entry in _cameraMap.Values)
                entry.camera.Priority = 0;

            _cameraMap[state].camera.Priority = _cameraMap[state].priority;
            Debug.Log($"[CameraManager] 切换到 {state}");
        }
        
        public override void Destroy()
        {
            if (_cameraRoot)
            {
                Object.Destroy(_cameraRoot.gameObject);
            }
            
            _cameraMap.Clear();
        }
    }
}