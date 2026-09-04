using UnityEngine;

/// <summary>
/// AOT 启动场景入口。Player 在此加载 HybridCLR 热更程序集，再调用热更里的 <see cref="MainEntry.Run"/>。
/// </summary>
public class Launch : MonoBehaviour
{
    private void Start()
    {
#if UNITY_EDITOR
        MainEntry.Run();
#else
        // Assembly hotUpdateAss = Assembly.Load(System.IO.File.ReadAllBytes(
        //     $"{Application.streamingAssetsPath}/HotUpdate.dll.bytes"));
        // hotUpdateAss.GetType("MainEntry").GetMethod("Run")?.Invoke(null, null);
        MainEntry.Run();
#endif
    }
}
