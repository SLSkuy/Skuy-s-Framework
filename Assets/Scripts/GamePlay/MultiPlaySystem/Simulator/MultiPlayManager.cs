using System;
using Framework;
using Network;

namespace GamePlay.MultiPlaySystem
{
    public enum MultiPlayMode
    {
        None,
        Server,
        Client
    }

    public sealed class MultiPlayManager : SubSystemBase
    {
        public override int Priority => (int)SubSystemPriority.NetSyncManager;

        public MultiPlayMode Mode { get; private set; }
        public ServerSimulator Server { get; private set; }
        public ClientSimulator Client { get; private set; }
        public bool IsRunning => Mode != MultiPlayMode.None;

        public event Action<MultiPlayMode> ModeChanged;

        /// <summary>
        /// 启动同步测试服务端。
        /// </summary>

        public bool StartServer()
        {
            if (Mode == MultiPlayMode.Server) return true;
            if (Mode != MultiPlayMode.None) return false;

            NetServer netServer = GetOrRegister<NetServer>();
            if (netServer == null) return false;

            try
            {
                Server = new ServerSimulator(netServer);
                Server.Start();
                netServer.StartServer();
            }
            catch
            {
                Server?.Dispose();
                Server = null;
                throw;
            }

            SetMode(MultiPlayMode.Server);
            return true;
        }

        public bool StartClient()
        {
            if (Mode == MultiPlayMode.Client) return true;
            if (Mode != MultiPlayMode.None) return false;

            NetClient netClient = GetOrRegister<NetClient>();
            if (netClient == null) return false;

            try
            {
                Client = new ClientSimulator(netClient);
                Client.Start();
                netClient.StartReliableConnect();
            }
            catch
            {
                Client?.Dispose();
                Client = null;
                throw;
            }

            SetMode(MultiPlayMode.Client);
            return true;
        }

        /// <summary>
        /// 停止当前同步测试端并清理测试实体。
        /// </summary>
        public void Stop()
        {
            if (Mode == MultiPlayMode.Server)
            {
                Server?.Dispose();
                Global.Get<NetServer>()?.StopServer();
                Server = null;
            }
            else if (Mode == MultiPlayMode.Client)
            {
                Client?.Dispose();
                Global.Get<NetClient>()?.StopClient();
                Client = null;
            }

            SetMode(MultiPlayMode.None);
        }

        public override void Update(float deltaTime)
        {
            Server?.Update(deltaTime);
            Client?.Update(deltaTime);
        }

        public override void Destroy()
        {
            Stop();
        }

        private static T GetOrRegister<T>() where T : class, ISubSystem, new()
        {
            SystemManager systemManager = Global.Get<SystemManager>();
            if (systemManager == null) return null;
            return systemManager.GetSystem<T>() ?? systemManager.RegisterSystem<T>();
        }

        private void SetMode(MultiPlayMode mode)
        {
            if (Mode == mode) return;
            Mode = mode;
            ModeChanged?.Invoke(mode);
        }
    }
}
