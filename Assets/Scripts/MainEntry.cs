using UnityEngine.SceneManagement;

/// <summary>
/// HybridCLR 热更入口。由 AOT 的 <see cref="Launch"/> 在加载热更程序集之后调用，不要用运行时初始化或框架事件去勾它。
/// </summary>
public class MainEntry
{
    private const string MAIN_SCENE = "MainScene";

    /// <summary>
    /// 热更程序集启动：切到带 GameCore 与 ProcedureManager 的主场景。
    /// </summary>
    public static void Run()
    {
        if (SceneManager.GetActiveScene().name == MAIN_SCENE)
        {
            return;
        }

        SceneManager.LoadScene(MAIN_SCENE);
    }
}
