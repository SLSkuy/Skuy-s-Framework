using System;

namespace Framework.StateMachine
{
    /// <summary>
    /// 泛型状态机
    /// </summary>
    /// <typeparam name="TEnum">枚举类型</typeparam>
    public class EnumStateMachine<TEnum> where TEnum : Enum
    {
        private readonly ExtendableStateMachine<int> _stateMachine;
        
        public EnumStateMachine()
        {
            _stateMachine = new ExtendableStateMachine<int>();
            _stateMachine.OnStateChange += StateChange;
        }

        /// <summary>
        /// 当前状态
        /// </summary>
        public TEnum CurrentState => (TEnum)Enum.ToObject(typeof(TEnum), _stateMachine.CurrentState);

        #region 事件
        public event Action<TEnum, TEnum> OnStateChange;
        #endregion

        #region 状态管理

        /// <summary>
        /// 注册状态
        /// </summary>
        public void RegisterState(IEnumState state)
        {
            _stateMachine.RegisterState(state);
        }

        /// <summary>
        /// 更换状态
        /// </summary>
        /// <param name="state"></param>
        public void ChangeState(TEnum state)
        {
            _stateMachine.ChangeState(Convert.ToInt32(state));
        }

        private void StateChange(int oldState, int newState)
        {
            TEnum oldStateKey = (TEnum)Enum.ToObject(typeof(TEnum), oldState);
            TEnum newStateKey = (TEnum)Enum.ToObject(typeof(TEnum), newState);
            OnStateChange?.Invoke(oldStateKey, newStateKey);
        }

        #endregion

        #region 生命周期

        public void Update(float deltaTime)
        {
            _stateMachine.Update(deltaTime);
        }

        public void FixedUpdate(float fixedDeltaTime)
        {
            _stateMachine.FixedUpdate(fixedDeltaTime);
        }

        public void LateUpdate()
        {
            _stateMachine.LateUpdate();
        }

        #endregion
    }
}