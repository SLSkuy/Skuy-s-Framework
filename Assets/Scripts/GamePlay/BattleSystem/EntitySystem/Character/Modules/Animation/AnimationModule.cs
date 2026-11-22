using System.Collections.Generic;
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

        private MovementModule _movement;
        private ViewModule _view;
        private AnimationConfig _config;

        private Animator _animator;
        private PlayableGraph _graph;
        /// <summary> 顶层动画混合器，用于混合移动动画和战斗动画 </summary>
        private AnimationMixerPlayable _topMixer;
        /// <summary> 基础移动动画混合器（待机，走，跑等持续动画） </summary>
        private AnimatorControllerPlayable _locomotion;
        /// <summary> 战斗动画混合器 </summary>
        private AnimationMixerPlayable _actionMixer;

        // 当前待过渡动画列表
        private List<AnimationTransitionData> _actions = new();
        private bool _isTransitioning;
        private bool _isAction;
        // 一次性动画时间记录
        private float _actionElapsed;
        private float _actionDuration;
        // 动画过渡时间记录
        private float _transitionElapsed;
        private float _transitionDuration;

        #region Animator参数
        /// <summary> 角色移动动画参数 </summary>
        private float _curSpeed;
        /// <summary> 角色转向动画参数 </summary>
        private float _curTurn;

        private int _speedHash;
        private int _turnHash;
        #endregion
        
        #region 属性
        public override ModuleType Type => ModuleType.Animation;
        #endregion

        #region 动画播放管理

        /// <summary>
        /// 播放一次性动画，传入开始事件后立刻从某一进度开始播放动画
        /// </summary>
        public void PlayActionAnimation(in EntityAnimationRequest request)
        {
            int port = _actions.Count;
            bool isFirstAction = port == 0;
            
            // 设置新的动作窗口
            _actionElapsed = 0f;
            _actionDuration = request.duration;

            // 设置过渡时间
            _transitionElapsed = 0f;
            _transitionDuration = Mathf.Min(request.duration * 0.5f, TRANSITION_TIME);

            // 更新起始权重
            foreach (AnimationTransitionData action in _actions)
            {
                action.startWeight = action.currentWeight;
            }

            float initWeight = isFirstAction ? 1f : 0f;
            AnimationClipPlayable clip = AnimationClipPlayable.Create(_graph, request.clip);
            clip.SetTime(request.sampleOffset);
            
            // 连接混合器
            _actionMixer.SetInputCount(port + 1);
            _actionMixer.ConnectInput(port, clip, 0, initWeight);
            
            // TODO: 使用对象池进行优化
            _actions.Add(new AnimationTransitionData
            {
                clip =  clip,
                startWeight = initWeight,
                currentWeight = initWeight,
            });
            
            _isAction = true;
            _isTransitioning = !isFirstAction;
        }
        
        /// <summary>
        /// 立刻打断当前一次性动画，并过渡到移动动画
        /// </summary>
        public void InterruptActionAnimation()
        {
            _isAction = false;
            _isTransitioning = false;
        }
        
        /// <summary>
        /// 计算顶层和Action内部动画混合
        /// </summary>
        private void UpdateActionAnimation(float deltaTime)
        {
            if (_actions.Count == 0) return;

            // 更新动作内部混合
            UpdateActionTransition(deltaTime);

            // 更新Action播放进度
            if (_isAction)
            {
                _actionElapsed += deltaTime;

                if (_actionElapsed >= _actionDuration)
                {
                    InterruptActionAnimation();
                }
            }

            // 顶层过渡，由Locomotion过渡到Action
            float targetWeight = _isAction ? 1f : 0f;
            float actionWeight = Mathf.MoveTowards(_topMixer.GetInputWeight(ACTION_PORT), targetWeight, deltaTime / TRANSITION_TIME);
            _topMixer.SetInputWeight(ACTION_PORT, actionWeight);
            _topMixer.SetInputWeight(LOCOMOTION_PORT, 1f - actionWeight);

            if (!_isAction && actionWeight == 0f)
            {
                ClearActionAnimation();
            }
        }

        /// <summary>
        /// 更新一次性动画进度，混合多个一次性动作过渡
        /// </summary>
        private void UpdateActionTransition(float deltaTime)
        {
            if (!_isTransitioning) return;
            
            // 更新过渡进度
            _transitionElapsed += deltaTime;
            float progress = _transitionDuration > 0f ? Mathf.Clamp01(_transitionElapsed / _transitionDuration) : 1f;
            int latestIndex = _actions.Count - 1;
            
            // smoothStep算法 blend = t^2 * (3 - 2*t);
            // 开始过渡慢，中间速度快，结尾过渡慢
            float blend = progress * progress * (3f - 2f * progress);
            for (int i = 0; i < _actions.Count; i++)
            {
                AnimationTransitionData action = _actions[i];
                action.currentWeight = i == latestIndex ? blend : action.startWeight * (1f - blend);
                _actionMixer.SetInputWeight(i, action.currentWeight);
            }
            
            // 清理过渡节点，保留最后的动作节点
            if (progress >= 1f)
            {
                _isTransitioning = false;
                AnimationTransitionData latest = _actions[latestIndex];
                
                // 先断开所有端口，保留最新节点
                for (int i = 0; i < _actions.Count; i++)
                {
                    _actionMixer.DisconnectInput(i);

                    if (i != latestIndex)
                    {
                        _graph.DestroyPlayable(_actions[i].clip);
                    }
                }

                _actions.Clear();
                _actions.Add(latest);
                _actionMixer.SetInputCount(1);
                _actionMixer.ConnectInput(0, latest.clip, 0, 1f);
            }
        }
                
        private void ClearActionAnimation()
        {
            _topMixer.SetInputWeight(ACTION_PORT, 0f);
            _topMixer.SetInputWeight(LOCOMOTION_PORT, 1f);

            for (int i = 0; i < _actions.Count; i++)
            {
                _actionMixer.DisconnectInput(i);
                _graph.DestroyPlayable(_actions[i].clip);
            }

            _actions.Clear();
            _actionMixer.SetInputCount(0);

            _isAction = false;
            _isTransitioning = false;
            _actionElapsed = 0f;
            _actionDuration = 0f;
            _transitionElapsed = 0f;
            _transitionDuration = 0f;
        }
        
        private void DestroyGraph()
        {
            if (_graph.IsValid())
            {
                _graph.Destroy();
            }
        }

        #endregion

        #region 动画参数管理

        private void UpdateSpeedParam()
        {
            // 动画速度只在表现层平滑，不参与移动模拟或快照恢复
            float targetSpeed = Context.DesiredSpeed * Context.LastMoveInput.magnitude; // 神秘手柄输入
            float smoothTime = _config.animSpeedSmoothTime;
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
            float smoothTime = _config.animTurnSmoothTime;
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
            ClearActionAnimation();

            _curSpeed = 0f;
            _curTurn = 0f;
        }

        #endregion

        #region 生命周期
        
        protected override void OnBound(EntityCharacter target)
        {
            DestroyGraph();

            _animator = GetComponent<Animator>();
            _movement = Context.Get<MovementModule>();
            _view = Context.Get<ViewModule>();

            _config = Config.animationConfig;

            _speedHash = Animator.StringToHash("Speed");
            _turnHash = Animator.StringToHash("Turn");

            // 创建动画输出通道
            _graph = PlayableGraph.Create("EntityAnimationGraph");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);    // 自定义时间驱动

            _locomotion = AnimatorControllerPlayable.Create(_graph, _config.animatorController);
            _actionMixer = AnimationMixerPlayable.Create(_graph, 2);
            _topMixer = AnimationMixerPlayable.Create(_graph, 2);   // 两个输入通道（基础移动/战斗动画）

            _topMixer.ConnectInput(LOCOMOTION_PORT, _locomotion, 0);
            _topMixer.ConnectInput(ACTION_PORT, _actionMixer, 0);
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
        
        private void OnDisable()
        {
            if (_graph.IsValid())
            {
                // 还池后从待机和初始播放时间重新开始
                ResetAnimation();
            }
        }
        
        private void Update()
        {
            UpdateSpeedParam();
            UpdateTurnParam();
        }

        /// <summary>
        /// Tick驱动动画更新
        /// </summary>
        public void Tick(float deltaTime)
        {
            UpdateActionAnimation(deltaTime);
            _graph.Evaluate(deltaTime);
        }

        private void OnDestroy()
        {
            DestroyGraph();

            _actions = null;
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