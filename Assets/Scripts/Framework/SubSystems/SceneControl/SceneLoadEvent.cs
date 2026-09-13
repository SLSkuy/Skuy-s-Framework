namespace Framework
{
    /// <summary>
    /// 场景加载全局事件。Started 在请求被接受时立刻发出；进度不走事件。
    /// </summary>
    public abstract class SceneLoadEvent
    {
        public class Started : AEvent<StartedData> { }
        public struct StartedData
        {
            public string SceneName;
        }

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
