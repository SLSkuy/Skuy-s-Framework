using Framework.StateMachine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 基础实体运行时上下文。
    /// 保存实体运行期间产生的状态数据以及运行时模块。
    /// </summary>
    public abstract class EntityObjectContext<TConfig> where TConfig : class
    {
        /// <summary>
        /// 实体状态机
        /// </summary>
        public readonly ExtendableStateMachine<uint> StateMachine;

        /// <summary>
        /// 实体静态配置
        /// </summary>
        public readonly TConfig Config;

        protected EntityObjectContext(TConfig config)
        {
            Config = config;
            StateMachine = new ExtendableStateMachine<uint>();
        }
    }
}
