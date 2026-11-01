using System;
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

        /// <summary> 角色移动动画参数 </summary>
        private float _curSpeed;
        /// <summary> 角色转向动画参数 </summary>
        private float _curTurn;
        /// <summary> Walk、Run、Sprint 共用的累计步态周期，Idle 独立播放 </summary>
        private double _locomotionPhase;
        
        private PlayableGraph _graph;
        /// <summary> 基础移动动画混合器（待机，走，跑等持续动画） </summary>
        private AnimationMixerPlayable _locomotionMixer;
        /// <summary> 顶层动画混合器，用于混合移动动画和战斗动画 </summary>
        private AnimationMixerPlayable _topMixer;
        /// <summary> 战斗动画混合器 </summary>
        private AnimationMixerPlayable _actionMixer;
        
        /// <summary> 基础动画集合 </summary>
        private AnimationClipPlayable[] _locomotionClips;
        private float[] _locomotionClipLengths;
        
        #region 属性
        public override ModuleType ModuleType => ModuleType.Animation;
        #endregion
        
        protected override void OnBound(EntityCharacter target)
        {
            _animator = GetComponent<Animator>();
            _movement = Context.Get<MovementModule>();
            _view = Context.Get<ViewModule>();
            
            // 创建动画输出通道
            _graph = PlayableGraph.Create("EntityAnimationGraph");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);    // Tick驱动

            _topMixer = AnimationMixerPlayable.Create(_graph, 2);   // 两个输入通道（基础移动/战斗动画）
            _locomotionMixer = AnimationMixerPlayable.Create(_graph, 4);    // 混合移动动画
            _actionMixer = AnimationMixerPlayable.Create(_graph, 0);    // 战斗动画通道（站位）

            _locomotionClips = new[]
            {
                CreateClip(Config.idleAnim, _locomotionMixer, 0),
                CreateClip(Config.walkAnim, _locomotionMixer, 1),
                CreateClip(Config.runAnim, _locomotionMixer, 2),
                CreateClip(Config.sprintAnim, _locomotionMixer, 3),
            };
            
            _locomotionClipLengths = new float[_locomotionClips.Length];
            for (int i = 0; i < _locomotionClips.Length; i++)
            {
                _locomotionClipLengths[i] = _locomotionClips[i].GetAnimationClip().length;
                if (i > 0)
                {
                    // 移动动画统一按相位采样，避免 Evaluate 再次推进各自时间。
                    _locomotionClips[i].SetSpeed(0d);
                }
            }
            ResetAnimation();

            _topMixer.ConnectInput(LOCOMOTION_PORT, _locomotionMixer, 0);
            _topMixer.ConnectInput(ACTION_PORT, _actionMixer, 0);
            _topMixer.SetInputWeight(LOCOMOTION_PORT, 1f);
            _topMixer.SetInputWeight(ACTION_PORT, 0);
            
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(_graph, "Output", _animator);
            output.SetSourcePlayable(_topMixer); // 连接动画输出
            _graph.Play();

#if UNITY_EDITOR
            // DEBUG显示动画节点图
            GraphVisualizerClient.Show(_graph);
#endif
        }
        
        protected override void OnUnbound(EntityCharacter oldTarget)
        {
            DestroyGraph();
        }

        /// <summary>
        /// 创建Playable动画片段，并连入混合器
        /// </summary>
        private AnimationClipPlayable CreateClip(EntityAnimData data, AnimationMixerPlayable mixer, int input)
        {
            if (!data.clip || data.clip.length <= 0f)
            {
                throw new ArgumentException("实体动画必须配置时长大于零的 Clip");
            }
            
            AnimationClipPlayable clipPlayable = AnimationClipPlayable.Create(_graph, data.clip);
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
        
        private void UpdateBlendWeights()
        {
            // 混合移动参数
            float walkBlend = Mathf.InverseLerp(0f, Config.walkSpeed, _curSpeed);
            float runBlend = Mathf.InverseLerp(Config.walkSpeed, Config.runSpeed, _curSpeed);
            float sprintBlend = Mathf.InverseLerp(Config.runSpeed, Config.sprintSpeed, _curSpeed);
            _locomotionMixer.SetInputWeight(0, 1f - walkBlend);
            _locomotionMixer.SetInputWeight(1, walkBlend - runBlend);
            _locomotionMixer.SetInputWeight(2, runBlend - sprintBlend);
            _locomotionMixer.SetInputWeight(3, sprintBlend);
        }
        
        /// <summary>
        /// 按移动动画的归一化权重混合步频，让不同长度的 Clip 保持相同步态相位
        /// </summary>
        private void UpdateLocomotionTime(float deltaTime)
        {
            float totalWeight = 0f; // 当前移动动画占比权重（可能有部分被Idle分配）
            double weightedFrequency = 0d;  // 当前加权周期
            for (int i = 1; i < _locomotionClips.Length; i++)
            {
                float weight = _locomotionMixer.GetInputWeight(i);
                totalWeight += weight;  // clip步频越快，加权周期频率就更高
                weightedFrequency += weight / (double)_locomotionClipLengths[i];  // 计算当前clip的步频对周期的贡献，获取当前总的周期频率，更新步态周期
            }

            if (totalWeight > 0f)
            {
                // 不将 Idle 计入步频
                // 除以totalWeight，低速混合时仍维持移动动画自身的周期
                // 用加权周期频率 * deltaTime 推进公共步态相位
                _locomotionPhase += weightedFrequency / totalWeight * deltaTime;
            }
            else
            {
                // totalWeight为0，说明Idle占所有权重，此时不播放移动动画，重置移动相位
                _locomotionPhase = 0d;
            }

            for (int i = 1; i < _locomotionClips.Length; i++)
            {
                // 保留累计周期，设置播放时间而不是由默认的Speed驱动
                // 根据当前周期乘以每周期长度，获取对应的动画采样时间
                // 由 Clip 的 Loop Time 处理循环，避免每圈主动倒拨时间
                _locomotionClips[i].SetTime(_locomotionPhase * _locomotionClipLengths[i]);
            }
        }

        private void ResetAnimation()
        {
            _curSpeed = 0f;
            _curTurn = 0f;
            _locomotionPhase = 0d;
            
            // 清空动画进度
            foreach (AnimationClipPlayable playable in _locomotionClips)
            {
                playable.SetTime(0d);
            }
            
            UpdateBlendWeights();
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

            // 设置混合权重
            UpdateBlendWeights();
            UpdateLocomotionTime(Time.deltaTime);
            
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