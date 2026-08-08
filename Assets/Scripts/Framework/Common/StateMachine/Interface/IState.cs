namespace Framework.StateMachine
{
    /// <summary>
    /// 状态接口
    /// </summary>
    public interface IState
    {
        void Enter();

        void Exit();

        void Update(float deltaTime);

        void FixedUpdate(float fixedDeltaTime);

        void LateUpdate();
    }
}