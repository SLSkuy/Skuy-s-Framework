using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// Playable纯代码控制动画能力组件，提供动画播放入口，对接战斗能力模块
    /// </summary>
    public class AnimationModule : EntityModuleBase
    {
        private const int LOCOMOTION_PORT = 0;
        private const int ACTION_PORT = 1;
        
        private Animator _animator;
        private MovementModule _movement;
        private ViewModule _view;
        
        private PlayableGraph _graph;
        
        /// <summary> 顶层动画混合器，用于混合移动动画和战斗动画 </summary>
        private AnimationMixerPlayable _topMixer;
        /// <summary> 基础移动动画混合器（待机，走，跑等持续动画） </summary>
        private AnimatorControllerPlayable _locomotion;
        /// <summary> 战斗动画混合器 </summary>
        private AnimationMixerPlayable _actionMixer;
        
        /// <summary> 角色移动动画参数 </summary>
        private float _curSpeed;
        /// <summary> 角色转向动画参数 </summary>
        private float _curTurn;

        private int _speedHash;
        private int _turnHash;
        
        #region 属性
        public override ModuleType ModuleType => ModuleType.Animation;
        #endregion
        
        protected override void OnBound(EntityCharacter target)
        {
            _animator = GetComponent<Animator>();
            _movement = Context.Get<MovementModule>();
            _view = Context.Get<ViewModule>();
            
            _speedHash = Animator.StringToHash("Speed");
            _turnHash = Animator.StringToHash("Turn");
            
            // 创建动画输出通道
            _graph = PlayableGraph.Create("EntityAnimationGraph");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);    // Tick驱动
            
            _locomotion = AnimatorControllerPlayable.Create(_graph, _animator.runtimeAnimatorController);
            _actionMixer = AnimationMixerPlayable.Create(_graph, 0);    // 战斗动画通道（站位）
            _topMixer = AnimationMixerPlayable.Create(_graph, 2);   // 两个输入通道（基础移动/战斗动画）
            
            _topMixer.ConnectInput(LOCOMOTION_PORT, _locomotion, 0);
            _topMixer.ConnectInput(ACTION_PORT, _actionMixer, 0);
            _topMixer.SetInputWeight(LOCOMOTION_PORT, 1f);
            _topMixer.SetInputWeight(ACTION_PORT, 0);
            
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(_graph, "BlendResult", _animator);
            output.SetSourcePlayable(_topMixer); // 连接动画输出
            _graph.Play();

#if UNITY_EDITOR
            // DEBUG显示动画节点图
            GraphVisualizerClient.Show(_graph);
#endif
        }
        
        protected override void OnUnbound(EntityCharacter oldTarget)
        {
            ResetAnimation();
            DestroyGraph();
        }

        /// <summary>
        /// 创建Playable动画片段，并连入混合器
        /// </summary>
        private AnimationClipPlayable CreateClip(AnimationClip clip, AnimationMixerPlayable mixer, int input)
        {
            AnimationClipPlayable clipPlayable = AnimationClipPlayable.Create(_graph, clip);
            _graph.Connect(clipPlayable, 0, mixer, input);
            return clipPlayable;
        }

        private void UpdateSpeedParam()
        {
            // 动画速度只在表现层平滑，不参与移动模拟或快照恢复
            float targetSpeed = Context.DesiredSpeed * Context.LastMoveInput.magnitude; // 神秘手柄输入
            float smoothTime = Config.animSpeedSmoothTime;
            float blend = smoothTime > 0f ? 1f - Mathf.Exp(-Time.deltaTime / smoothTime) : 1f;
            _curSpeed = Mathf.Lerp(_curSpeed, targetSpeed, blend);
            if (Mathf.Abs(_curSpeed - targetSpeed) < 0.001f)
            {
                _curSpeed = targetSpeed;
            }
            _animator.SetFloat(_speedHash, _curSpeed);
        }
        
        private void UpdateTurnParam()
        {
            float targetTurn = CalculateTurn();
            float smoothTime = Config.animTurnSmoothTime;
            float blend = smoothTime > 0f ? 1f - Mathf.Exp(-Time.deltaTime / smoothTime) : 1f;
            _curTurn = Mathf.Lerp(_curTurn, targetTurn, blend);
            if (Mathf.Abs(_curTurn - targetTurn) < 0.001f)
            {
                _curTurn = targetTurn;
            }
            _animator.SetFloat(_turnHash, _curTurn);
        }
        
        /// <summary>
        /// 将 mesh 朝向到视角相对输入朝向的水平夹角映射到 [-1, 1]，左负右正。
        /// </summary>
        private float CalculateTurn()
        {
            Vector2 moveInput = Context.LastMoveInput;
            if (moveInput.sqrMagnitude <= Mathf.Epsilon) return 0f;

            Vector3 inputDirection = Quaternion.Euler(0f, _view.Yaw, 0f) * new Vector3(moveInput.x, 0f, moveInput.y);
            Vector3 meshForward = Vector3.ProjectOnPlane(_movement.Rotation * Vector3.forward, Vector3.up);
            float angle = Vector3.SignedAngle(meshForward, inputDirection, Vector3.up);
            return Mathf.Clamp(angle / 180f, -1f, 1f);
        }

        private void ResetAnimation()
        {
            _curSpeed = 0f;
            _curTurn = 0f;
        }
        
        private void DestroyGraph()
        {
            if (_graph.IsValid())
            {
                _graph.Destroy();
            }
        }
        
        #region 生命周期

        private void Update()
        {
            UpdateSpeedParam();
            UpdateTurnParam();
            
            _graph.Evaluate(Time.deltaTime);
        }
        
        private void OnDisable()
        {
            if (_graph.IsValid())
            {
                // 还池后从待机和初始播放时间重新开始
                ResetAnimation();
            }
        }
        
        private void OnDestroy()
        {
            DestroyGraph();
        }

        #endregion
    }
}