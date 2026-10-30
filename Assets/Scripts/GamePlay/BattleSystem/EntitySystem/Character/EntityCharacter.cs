using Framework;
using GamePlay.DataProxy;
using UnityEngine;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 场景角色实体：装配模块与状态机，并托管固定 Tick 模拟
    /// </summary>
    public class EntityCharacter : MonoBehaviour, IPoolable
    {
        [SerializeField] private CharacterConfig config;
        
        // 能力组件
        private EntityVisualPresentation _visualPresentation;
        private EntitySimulator _simulator;
        private EntityContext _context;
        
        private bool _isInitialized;

        #region 属性
        public EntityContext Context => _context;
        public bool IsInitialized => _isInitialized;
        #endregion

        /// <summary>
        /// 初始化实体入口。
        /// </summary>
        public void Init(bool isReplica = false)
        {
            if (_isInitialized) return;

            InitConfig();
            
            // 统一收集能力组件
            _context = new EntityContext(this, config);
            _context.AddAbility<EntityStateMachineModule>();
            _context.AddAbility<ViewModule>();
            _context.AddAbility<AnimationModule>();
            MovementModule movement = _context.AddAbility<MovementModule>();
            _visualPresentation = _context.AddAbility<EntityVisualPresentation>();

            // 全部能力登记后统一绑定并初始化，防止组件间依赖导致空引用
            _context.BindAbilities();
            movement.SetReplicaMode(isReplica);
            
            // 注册模拟控制器
            _simulator = new EntitySimulator(_context);

            // 实体能力注册完毕之后切换到待机状态
            _context.Get<EntityStateMachineModule>().ChangeState(EntityState.IDLE);
            
            _isInitialized = true;
        }

        protected virtual void InitConfig()
        {
            if (config) return;
            
            config = Global.GetDataProxy<EntityConfigProxy>().Config;
        }
        
        #region 模拟入口

        /// <summary>
        /// 使用完整命令推进一次固定 Tick 模拟
        /// </summary>
        public void Step(uint tick, float deltaTime, in EntityControlCommand command)
        {
            _simulator.Step(tick, deltaTime, command);
        }

        /// <summary>
        /// 获取快照状态
        /// </summary>
        public EntitySnapshot CaptureSnapshot()
        {
            return _simulator.CaptureSnapshot();
        }

        /// <summary>
        /// 写回模拟状态，不改画面采样
        /// </summary>
        public void RestoreSimulation(in EntitySnapshot state)
        {
            _simulator.RestoreSnapshot(state);
        }

        #endregion
        
        #region 画面插值
        
        /// <summary>
        /// 步进前归位画面节点与权威旋转
        /// </summary>
        public void PrepareSimulationTick()
        {
            _visualPresentation.PrepareSimulation();
        }

        /// <summary>
        /// 步进后收录权威位置
        /// </summary>
        public void CaptureAuthorityAfterTick()
        {
            _visualPresentation.CaptureAuthority();
        }

        /// <summary>
        /// 按残差写出画面位姿
        /// </summary>
        public void PresentVisualPose(float alpha)
        {
            _visualPresentation.Present(alpha);
        }
        
        /// <summary>
        /// 按远端插值结果写出画面位姿，不改权威拍缓存
        /// </summary>
        public void PresentReplicaPose(Vector3 rootPosition, Quaternion meshRotation, Quaternion viewRotation)
        {
            _visualPresentation.PresentPose(rootPosition, meshRotation, viewRotation);
        }
        
        /// <summary>
        /// 把当前逻辑旋转写入画面当前拍，不改位置采样。
        /// </summary>
        public void SyncPresentationRotation()
        {
            _visualPresentation.SyncLogicalRotation();
        }
        
        /// <summary>
        /// 写回模拟状态并让画面立刻对齐快照
        /// </summary>
        public void RestoreSnapshot(in EntitySnapshot state)
        {
            RestoreSimulation(state);
            _visualPresentation.SnapToAuthority();
        }
        
        #endregion
        
        /// <summary>
        /// 还池时清掉上一场的运动、视角和状态。
        /// </summary>
        void IPoolable.Reset()
        {
            if (!_isInitialized) return;

            _context.LastMoveInput = Vector2.zero;
            _context.LastAimInput = Vector2.zero;
            _context.CurrentTick = 0;
            _context.RunToggleRequest = false;
            _context.JumpRequest = false;
            _context.IsSprinting = false;
            _context.IsRunning = false;
            _context.IsFocus = false;

            // 清理移动状态
            MovementModule movement = _context.Get<MovementModule>();
            movement.ResetMotion();
            movement.Restore(Quaternion.identity);
            
            // 清理视角状态
            _context.Get<ViewModule>().Restore(Quaternion.identity);
            
            // 清理状态机
            _context.Get<EntityStateMachineModule>().ChangeState(EntityState.IDLE);
            
            // 清理视觉插值
            _visualPresentation.SnapToAuthority();
        }
    }
}
