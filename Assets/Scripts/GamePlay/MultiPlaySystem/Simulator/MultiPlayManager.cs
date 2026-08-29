using System;
using Framework;
using GamePlay.Simulator;
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
        public override int Priority => 100;

        public MultiPlayMode Mode { get; private set; }
        public ServerSimulationHost Server { get; private set; }
        public ClientSimulationHost Client { get; private set; }
        public bool IsRunning => Mode != MultiPlayMode.None;

        public event Action<MultiPlayMode> ModeChanged;

        /// <summary>
        /// 启动快照同步服务端。
        /// </summary>
        public bool StartServer()
        {
            if (Mode == MultiPlayMode.Server) return true;
            if (Mode != MultiPlayMode.None) return false;
            if (IsLocalSessionRunning()) return false;

            NetServer netServer = GetOrRegister<NetServer>();
            Server = GetOrRegister<ServerSimulationHost>();
            if (netServer == null || Server == null) return false;

            try
            {
                if (!Server.StartSession()) return false;
                netServer.StartServer();
            }
            catch
            {
                Server.StopSession();
                throw;
            }

            SetMode(MultiPlayMode.Server);
            return true;
        }

        public bool StartClient()
        {
            if (Mode == MultiPlayMode.Client) return true;
            if (Mode != MultiPlayMode.None) return false;
            if (IsLocalSessionRunning()) return false;

            NetClient netClient = GetOrRegister<NetClient>();
            Client = GetOrRegister<ClientSimulationHost>();
            if (netClient == null || Client == null) return false;

            try
            {
                if (!Client.StartSession()) return false;
                netClient.StartReliableConnect();
            }
            catch
            {
                Client.StopSession();
                throw;
            }

            SetMode(MultiPlayMode.Client);
            return true;
        }

        /// <summary>
        /// 停止当前联机端点并清理会话实体。
        /// </summary>
        public void Stop()
        {
            if (Mode == MultiPlayMode.Server)
            {
                Server?.StopSession();
                Global.Get<NetServer>()?.StopServer();
            }
            else if (Mode == MultiPlayMode.Client)
            {
                Client?.StopSession();
                Global.Get<NetClient>()?.StopClient();
            }

            SetMode(MultiPlayMode.None);
        }

        public override void Destroy()
        {
            Stop();
        }

        private static bool IsLocalSessionRunning()
        {
            LocalSimulationHost localHost = Global.Get<LocalSimulationHost>();
            return localHost != null && localHost.IsSessionRunning;
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
