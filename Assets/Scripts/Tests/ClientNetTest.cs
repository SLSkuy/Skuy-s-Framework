using Core;
using Network;
using UnityEngine;

public class ClientNetTest : MonoBehaviour
{
    public void Start()
    {
        var client = GameCore.Instance.SystemMgr.RegisterSystem<NetClient>();
    }
}
