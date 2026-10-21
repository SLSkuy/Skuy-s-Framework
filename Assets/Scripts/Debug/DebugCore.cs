using Core;
using Framework;
using GamePlay.Procedure;
using UnityEngine;
using UnityEngine.InputSystem;

public class DebugCore : MonoSingleton<DebugCore>
{
    protected override void Init()
    {
        if (!AppCoreConfig.Instance.debugMode) return;
        
        Debug.LogWarning($"[{GetType().Name}] 当前处于DEBUG模式，部分数据会随机生成且不会保存");
    }
    
    private void Update()
    {
        if(!AppCore.IsReady) return;
        
        if (Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame)
        {
            Global.Get<ProcedureModule>().BackToMainMenu();
        }
    }
}