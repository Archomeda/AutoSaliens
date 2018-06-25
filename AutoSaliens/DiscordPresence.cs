using System;
using System.Threading;
using DiscordRPC;
using DiscordRPC.Logging;
using DiscordRPC.Message;
using Timer = System.Timers.Timer;

namespace AutoSaliens
{
    internal class DiscordPresence : IDisposable
    {
        private const string clientId = "460795723881906186";

        private DiscordRpcClient rpcClient;
        private Thread loopThread;
        private Thread reconnectThread;
        private Timer reconnectTimer;
        private bool stopRequested = false;

        public event EventHandler OnEnable;
        public event EventHandler OnDisable;


        public void Initialize()
        {
            if (this.loopThread != null)
                return;

            this.rpcClient = new DiscordRpcClient(clientId)
            {
                Logger = new DiscordPresenceLogger() { Level = LogLevel.Warning }
            };
            this.rpcClient.OnConnectionEstablished += this.RpcClient_OnConnectionEstablished;
            this.rpcClient.OnConnectionFailed += this.RpcClient_OnConnectionFailed;
            this.rpcClient.Initialize();
            this.loopThread = new Thread(this.Loop);
            this.loopThread.Start();
            if (this.reconnectTimer == null)
            {
                this.reconnectTimer = new Timer(5 * 60 * 1000);
                this.reconnectTimer.Elapsed += (e, a) =>
                {
                    if (!this.Available)
                        this.Initialize();
                };
                this.reconnectTimer.Start();
            }
        }

        public bool Available { get; private set; }


        private void Loop()
        {
            this.Available = true;
            while (!this.stopRequested && this.Available)
            {
                if (this.rpcClient != null && this.rpcClient.IsInitialized && !this.rpcClient.Disposed)
                    this.rpcClient.Invoke();
                Thread.Sleep(10);
            }
            this.rpcClient.Dispose();
            this.rpcClient = null;
            this.loopThread = null;
        }


        private void RpcClient_OnConnectionEstablished(object sender, ConnectionEstablishedMessage args)
        {
            this.Available = true;
            this.OnEnable?.Invoke(this, null);
        }

        private void RpcClient_OnConnectionFailed(object sender, ConnectionFailedMessage args)
        {
            this.Available = false;
            this.OnDisable?.Invoke(this, null);
        }


        public void SetPresence(RichPresence presence)
        {
            if (this.Available)
                this.rpcClient.SetPresence(presence);
        }

        public void Dispose()
        {
            this.reconnectTimer.Dispose();
            this.stopRequested = true;
            this.Available = false;
        }
    }
}
