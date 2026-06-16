using EventProcess;

namespace Framework
{
    public class SceneLoadEvent
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
