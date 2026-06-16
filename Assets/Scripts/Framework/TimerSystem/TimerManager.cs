using System;
using System.Collections.Generic;

namespace Framework
{
    /// <summary>
    /// <para>计时器管理器</para>
    /// <para>管理计时器的运行与销毁</para>
    /// </summary>
    public class TimerManager : SubSystemBase
    {
        public override SubSystemPriority Priority => SubSystemPriority.TimerManager;
        private readonly List<Timer> _timers = new();

        /// <summary>
        /// <para>创建新的计时器并开始计时</para>
        /// </summary>
        /// <param name="duration">时间间隔</param>
        /// <param name="onComplete">计时完成回调函数</param>
        /// <param name="onUpdate">更新回调函数</param>
        /// <returns>计时器对象，需要保存以供移除</returns>
        public Timer CreateAndStart(float duration, Action onComplete = null, Action<float> onUpdate = null)
        {
            Timer timer = Global.Get<PoolManager>().Get<Timer>();
            timer.Init(duration, onComplete, onUpdate);
            timer.Start();
            _timers.Add(timer);
            return timer;
        }
        

        /// <summary>
        /// 移除计时器对象，归还对象池，适用于中断计时器操作
        /// </summary>
        public void RemoveTimer(Timer timer)
        {
            if (_timers.Remove(timer))
            {
                if (Global.TryGet(out PoolManager poolManager))
                {
                    poolManager.Release(timer);
                }
                else
                {
                    timer.Dispose();
                }
            }
        }

        /// <summary>
        /// 清空所有计时器，全部归还对象池
        /// </summary>
        public void ClearAllTimers()
        {
            foreach (var timer in _timers)
            {
                if (Global.TryGet(out PoolManager poolManager))
                {
                    poolManager.Release(timer);
                }
                else
                {
                    timer.Dispose();
                }
            }
            _timers.Clear();
        }

        #region 生命周期
        
        public override void Init()
        {
            Global.Get<PoolManager>().RegisterPool<Timer>(() => new Timer(), onRelease: timer => timer.Dispose(),
                onDestroy: timer => timer.Dispose(), defaultCapacity: 4);
        }
        
        public override void Update(float deltaTime)
        {
            for (int i = _timers.Count - 1; i >= 0; i--)
            {
                Timer timer = _timers[i];
                timer.Update(deltaTime);
                if (!timer.IsRunning)
                {
                    _timers.RemoveAt(i);
                    if (Global.TryGet(out PoolManager poolManager))
                    {
                        poolManager.Release(timer);
                    }
                    else
                    {
                        timer.Dispose();
                    }
                }
            }
        }

        public override void Destroy()
        {
            ClearAllTimers();
            if (Global.TryGet(out PoolManager poolManager))
            {
                poolManager.ClearPool<Timer>();
            }
        }

        #endregion
    }
}
