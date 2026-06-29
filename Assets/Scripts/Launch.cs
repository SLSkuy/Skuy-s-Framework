using System.Linq;
using System.Reflection;
using UnityEngine;

public class Launch : MonoBehaviour
{
    void Start()
    {
#if !UNITY_EDITOR
        // Assembly hotUpdateAss = Assembly.Load(File.ReadAllBytes($"{Application.streamingAssetsPath}/HotUpdate.dll.bytes"));
#else
        // Editor下无需加载，直接查找获得HotUpdate程序集
        // Assembly hotUpdateAss = System.AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "HotUpdate");
#endif
        // hotUpdateAss.GetType("MainEntry").GetMethod("Run")?.Invoke(null, null);
    }
}
