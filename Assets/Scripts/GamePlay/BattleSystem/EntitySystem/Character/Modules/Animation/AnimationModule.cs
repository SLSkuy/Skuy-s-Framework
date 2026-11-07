using System.Collections;
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
        private const float TRANSITION_TIME = 0.15f;
        
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
        private AnimationClipPlayable _action;
        
        private Coroutine _actionTransition;
        
        /// <summary> 角色移动动画参数 </summary>
        private float _curSpeed;
        /// <summary> 角色转向动画参数 </summary>
        private float _curTurn;

        private int _speedHash;
        private int _turnHash;
        
        #region 属性
        public override ModuleType Type => ModuleType.Animation;
        #endregion

        #region 动画播放入口

        /// <summary>
        /// 播放一次性动画，传入开始事件后立刻从某一进度开始播放动画
        /// </summary>
        public void PlayActionAnimation(in EntityAnimationRequest request)
        {
            if (_action.IsValid() && _action.GetAnimationClip() == request.clip)
            {
                // 当前与目标相同，设置播放进度
                _action.SetTime(request.sampleOffset);
                return;
            }
            
            InterruptActionAnimation();
            
            _actionTransition = StartCoroutine(TransitionAnimation(request.clip, request.duration, request.sampleOffset));
        }

        private IEnumerator TransitionAnimation(AnimationClip clip, float duration, float sampleOffset = 0f)
        {
            _action = AnimationClipPlayable.Create(_graph, clip);
            _action.SetTime(sampleOffset);
            _topMixer.ConnectInput(ACTION_PORT, _action, 0);

            // duration 包含淡入和淡出；短动作将两段过渡等比例缩短
            float blendTime = Mathf.Min(TRANSITION_TIME, duration * 0.5f);
            float elapsed = sampleOffset;
            while (elapsed < duration)
            {
                float blendIn = Mathf.Clamp01(elapsed / blendTime);
                float blendOut = Mathf.Clamp01((duration - elapsed) / blendTime);
                float actionWeight = Mathf.Min(blendIn, blendOut);
                _topMixer.SetInputWeight(LOCOMOTION_PORT, 1f - actionWeight);
                _topMixer.SetInputWeight(ACTION_PORT, actionWeight);

                yield return null;
                elapsed += Time.deltaTime;
            }

            _actionTransition = null;
            InterruptActionAnimation();
        }

        /// <summary>
        /// 中断当前播放的一次性动画
        /// </summary>
        public void InterruptActionAnimation()
        {
            if (_actionTransition != null)
            {
                StopCoroutine(_actionTransition);
                _actionTransition = null;
            }

            _topMixer.SetInputWeight(LOCOMOTION_PORT, 1f);
            _topMixer.SetInputWeight(ACTION_PORT, 0);

            if (_action.IsValid())
            {
                _topMixer.DisconnectInput(ACTION_PORT);
                _graph.DestroyPlayable(_action);
            }
        }

        #endregion

        protected override void OnBound(EntityCharacter target)
        {
            DestroyGraph();
            
            _animator = GetComponent<Animator>();
            _movement = Context.Get<MovementModule>();
            _view = Context.Get<ViewModule>();
            
            _speedHash = Animator.StringToHash("Speed");
            _turnHash = Animator.StringToHash("Turn");
            
            // 创建动画输出通道
            _graph = PlayableGraph.Create("EntityAnimationGraph");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);    // Tick驱动
            
            _locomotion = AnimatorControllerPlayable.Create(_graph, Config.animatorController);
            _topMixer = AnimationMixerPlayable.Create(_graph, 2);   // 两个输入通道（基础移动/战斗动画）
            
            _topMixer.ConnectInput(LOCOMOTION_PORT, _locomotion, 0);
            _topMixer.SetInputWeight(LOCOMOTION_PORT, 1f);
            
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
        
        private void DestroyGraph()
        {
            StopAllCoroutines();
            
            if (_graph.IsValid())
            {
                _graph.Destroy();
            }
        }

        #region 动画参数管理

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
            InterruptActionAnimation();
            
            _curSpeed = 0f;
            _curTurn = 0f;
        }

        #endregion

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

        public override void Reset()
        {
            ResetAnimation();
            
            _locomotion.Play(0, 0, 0f);
            _locomotion.SetFloat(_speedHash, 0f);
            _locomotion.SetFloat(_turnHash, 0f);
        }

        #endregion
    }
}