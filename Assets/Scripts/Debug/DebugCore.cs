using Core;
using GamePlay.Procedure;
using UnityEngine;
using UnityEngine.InputSystem;

public class DebugCore : MonoBehaviour
{
    private void Update()
    {
        if(!AppCore.IsReady) return;
        
        if (Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame)
        {
            ProcedureCore.Instance.BackToMainMenu();
        }
    }
}