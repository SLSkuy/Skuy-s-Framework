using System.Collections.Generic;
using Framework;

namespace GamePlay.Simulator
{
    /// <summary>
    /// 按 entityId + tick 对齐的意图邮箱；缺失时返回空输入。
    /// </summary>
    public sealed class CommandMailbox
    {
        private readonly Dictionary<ulong, InputState> _pending = new();

        public void Submit(uint entityId, uint tick, in InputState state)
        {
            if (entityId == 0 || tick == 0) return;
            _pending[Pack(entityId, tick)] = state;
        }

        public InputState Consume(uint entityId, uint tick)
        {
            ulong key = Pack(entityId, tick);
            if (_pending.Remove(key, out InputState state)) return state;
            return default;
        }

        public void RemoveEntity(uint entityId)
        {
            if (entityId == 0) return;

            List<ulong> keysToRemove = null;
            foreach (KeyValuePair<ulong, InputState> pair in _pending)
            {
                if ((uint)(pair.Key >> 32) != entityId) continue;
                keysToRemove ??= new List<ulong>();
                keysToRemove.Add(pair.Key);
            }

            if (keysToRemove == null) return;
            for (int i = 0; i < keysToRemove.Count; i++) _pending.Remove(keysToRemove[i]);
        }

        public void Clear()
        {
            _pending.Clear();
        }

        private static ulong Pack(uint entityId, uint tick)
        {
            return ((ulong)entityId << 32) | tick;
        }
    }
}
