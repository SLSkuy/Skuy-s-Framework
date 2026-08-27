using System.Collections.Generic;
using Framework;

namespace GamePlay.Simulator
{
    /// <summary>
    /// 单拍意图收集槽：按 entityId 覆盖写入，步进时取出后清空
    /// </summary>
    public sealed class CommandMailbox
    {
        private readonly Dictionary<uint, InputState> _pending = new();

        /// <summary>
        /// 提交本Tick意图
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="state"></param>
        public void Submit(uint entityId, in InputState state)
        {
            if (entityId == 0) return;
            _pending[entityId] = state;
        }

        /// <summary>
        /// 消费所有实体的意图
        /// </summary>
        public InputState Consume(uint entityId)
        {
            if (_pending.Remove(entityId, out InputState state)) return state;
            return default;
        }

        public void RemoveEntity(uint entityId)
        {
            if (entityId == 0) return;
            _pending.Remove(entityId);
        }

        public void Clear()
        {
            _pending.Clear();
        }
    }
}
