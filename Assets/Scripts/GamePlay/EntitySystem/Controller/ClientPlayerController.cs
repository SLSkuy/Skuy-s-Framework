using Framework;
using UnityEngine;
using Utils;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 玩家控制器，需要访问到EntityCharacter组件进行控制
    /// 桥接输入源和玩家实体
    /// </summary>
    public class ClientPlayerController : AutoEventMonoBehaviour
    {
        private IPlayerCharacter _clientPlayerCharacter;
        private IInputStateProvider _inputProvider;
        private Transform _cameraTransform;

        /// <summary>
        /// 缓存当前原始移动输入
        /// </summary>
        private Vector2 _lastTickInput;
        private Vector2 _currentTickInput;
        
        // 预测状态缓存，每一个输入Tick对应一个快照Tick
        // 权威处理输入Tick下发后，能够获取到对应的预测状态
        // 采用循环队列，通过取余获取索引
        private InputState[] _inputStates;
        private NetPlayerSnapshot[] _snapshots;
        private uint _lastEnsureInputIndex;
        
        /// <summary>
        /// 设置相机Transform
        /// </summary>
        /// <param name="cameraTrans">相机Transform</param>
        public void SetCameraTransform(Transform cameraTrans)
        {
            _cameraTransform = cameraTrans;
        }

        /// <summary>
        /// 设置输入来源
        /// </summary>
        /// <param name="provider"></param>
        public void SetInputStateProvider(IInputStateProvider provider)
        {
            _inputProvider = provider;
        }

        /// <summary>
        /// 获取映射后的输入方向
        /// </summary>
        /// <returns>映射后的方向</returns>
        private Vector2 GetMappedDirection(Vector2 inputDir)
        {
            if (!_cameraTransform)
            {
                return inputDir;
            }
            
            return TransformUtils.MapInputToWorldDirection2D(inputDir, _cameraTransform);
        }

        #region 事件订阅
        
        [AutoEvent("OnMove", nameof(_inputProvider))]
        private void OnMoveEvent(Vector2 moveInput)
        {
            _lastTickInput = _currentTickInput;
            _currentTickInput = GetMappedDirection(moveInput);
        }
        
        #endregion

        #region 生命周期

        protected override void Start()
        {
            // 如果没有设置相机，自动查找主相机
            if (_cameraTransform == null)
            {
                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    _cameraTransform = mainCamera.transform;
                }
            }
            
            Global.Get<CameraManager>().SetTarget(transform);
            
            base.Start();
        }
        
        #endregion
    }
}