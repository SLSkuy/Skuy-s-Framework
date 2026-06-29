using EventProcess;

namespace Framework
{
    /// <summary>
    /// 场景加载全局事件
    /// </summary>
    public abstract class SceneLoadEvent
    {
        public class Completed : AEvent<CompletedData> { }
        public struct CompletedData
        {
            public string SceneName;
        }

        public class Failed : AEvent<FailedData> { }
        public struct FailedData
        {
            public string SceneName;
            public string ErrorMessage;
        }
    }
}
