using UnityEngine;

namespace Framework
{
    public abstract class SubSystemBase : ISubSystem
    {
        public abstract SubSystemPriority Priority { get; }
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// 内置初始化方法，不应该被重写
        /// </summary>
        public void _Init()
        {
            if (IsInitialized)
            {
                Debug.Log($"[{GetType().Name}] 已经初始化");
                return;
            }
            
            Init();
            
            // 自动注册到全局服务定位器，业务层通过 Services.Get<T>() 获取
            Global.Register(this);
            
            BindEvents();
            
            Debug.Log($"[{GetType().Name}] 初始化");
            IsInitialized = true;
        }

        public virtual void Init()
        {
            
        }

        public virtual void BindEvents()
        {
            
        }

        public virtual void Update(float deltaTime)
        {

        }

        public virtual void LateUpdate()
        {

        }

        public virtual void FixedUpdate(float fixedDeltaTime)
        {

        }

        /// <summary>
        /// 内置销毁方法，不应该被重写
        /// </summary>
        public void _Destroy()
        {
            if (!IsInitialized)
            {
                Debug.Log($"[{GetType().Name}] 未被初始化，不能被销毁");
                return;
            }
            
            Destroy();
            
            // 自动从全局服务定位器注销
            Global.Unregister(GetType());
            
            IsInitialized = false;
            Debug.Log($"[{GetType().Name}] 销毁");
        }
        
        /// <summary>
        /// 销毁方法，基类会自动处理顺序问题
        /// </summary>
        public virtual void Destroy()
        {
            
        }
    }
}
