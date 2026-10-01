using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 预制体上的界面壳。编辑期保存绑定的控件引用，运行时挂上生成的界面脚本、写入引用后移除自身。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIView : MonoBehaviour
    {
        #region 内部类型

        /// <summary>
        /// 界面类别，决定生成脚本继承的控制器基类
        /// </summary>
        public enum ViewKind
        {
            Panel,
            Window
        }

        /// <summary>
        /// 生成的界面脚本实现该接口，由 <see cref="UIView"/> 在运行时调用以写入控件引用
        /// </summary>
        public interface IBindable
        {
            void Bind(UIView view);
        }

        /// <summary>
        /// 一条控件引用，字段名对应生成脚本里的字段
        /// </summary>
        [Serializable]
        public sealed class Binding
        {
            [SerializeField] [Tooltip("生成脚本里的字段名，不带下划线")] private string fieldName;
            [SerializeField] [Tooltip("绑定的控件")] private Component target;

            public Binding(string fieldName, Component target)
            {
                this.fieldName = fieldName;
                this.target = target;
            }

            public string FieldName { get => fieldName; set => fieldName = value; }
            public Component Target { get => target; set => target = value; }
        }

        #endregion

        [Header("界面")]
        [SerializeField] [Tooltip("界面类别，决定生成脚本继承的控制器基类")] private ViewKind kind;
        [SerializeField] [Tooltip("生成的界面脚本类型，程序集限定名，完成绑定时写入")] private string controllerTypeName;

        // 实例类型跟着界面类别和生成的属性类走，由 Inspector 维护
        [SerializeReference] [Tooltip("界面属性，运行时写入控制器")] private IUIProperties properties;

        [Header("UI过渡动画")]
        [SerializeField] [Tooltip("显示动画")] private AnimComponent animIn;
        [SerializeField] [Tooltip("隐藏动画")] private AnimComponent animOut;

        [Header("控件引用")]
        [SerializeField] [Tooltip("已绑定的控件，由各组件 Inspector 上的绑定按钮维护")]
        private List<Binding> bindings = new();

        #region 暴露属性

        public ViewKind Kind { get => kind; set => kind = value; }
        public string ControllerTypeName { get => controllerTypeName; set => controllerTypeName = value; }
        public IUIProperties Properties { get => properties; set => properties = value; }
        public AnimComponent AnimIn { get => animIn; set => animIn = value; }
        public AnimComponent AnimOut { get => animOut; set => animOut = value; }
        public List<Binding> Bindings => bindings;

        #endregion

        /// <summary>
        /// 在当前实例上挂载生成的界面脚本、写入控件引用，然后移除自身。
        /// 由 UIManager 在实例尚未激活时调用，挂好之后才会触发界面脚本的 Awake。
        /// </summary>
        /// <param name="uiControllerID">界面ID，写入控制器</param>
        public void ApplyBindings(string uiControllerID)
        {
            if (string.IsNullOrEmpty(controllerTypeName))
            {
                Debug.LogError($"[UIFramework] {name} 上的 UIView 还没有完成绑定，没有可挂载的界面脚本");
                return;
            }

            Type controllerType = Type.GetType(controllerTypeName);
            if (controllerType == null)
            {
                Debug.LogError($"[UIFramework] 找不到界面脚本 {controllerTypeName}，请重新完成绑定");
                return;
            }

            Component controller = gameObject.AddComponent(controllerType);
            ((IBindable)controller).Bind(this);
            ((IUIController)controller).UIControllerID = uiControllerID;

            Destroy(this);
        }

        /// <summary>
        /// 按字段名取出控件引用，供生成的 Bind 方法使用
        /// </summary>
        /// <param name="fieldName">绑定时记录的字段名</param>
        public Component GetBinding(string fieldName)
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                if (bindings[i].FieldName == fieldName)
                    return bindings[i].Target;
            }

            Debug.LogError($"[UIFramework] {name} 上没有名为 {fieldName} 的控件引用，请重新完成绑定");
            return null;
        }

        /// <summary>
        /// 查找某个控件当前是否已经绑定
        /// </summary>
        /// <param name="target">待查找的控件</param>
        public Binding FindBinding(Component target)
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                if (bindings[i].Target == target)
                    return bindings[i];
            }

            return null;
        }
    }
}
