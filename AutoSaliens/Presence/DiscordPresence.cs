using System;
using System.Threading;
using AutoSaliens.Api.Models;
using AutoSaliens.Presence.Formatters;
using DiscordRPC;
using DiscordRPC.Logging;
using Timer = System.Timers.Timer;

namespace AutoSaliens.Presence
{
    internal class DiscordPresence : IDisposable, IPresence
    {
        private const string clientId = "460795723881906186";
        private const int refreshRate = 60;

        private IPresenceFormatter formatter;
        private IPresenceUpdateTrigger updateTrigger;

        private DiscordRpcClient rpcClient;
        private Thread rpcEventLoopThread;
        private readonly Timer rpcReconnectTimer;
        private bool stopRequested = false;

        private bool presenceStarted = false;


        public event EventHandler PresenceActivated;
        public event EventHandler PresenceDeactivated;


        public DiscordPresence()
        {
            this.rpcReconnectTimer = new Timer(2 * 60 * 1000);
            this.rpcReconnectTimer.Elapsed += (e, a) =>
            {
                if (!this.presenceStarted)
                    this.Start();
            };
        }


        public virtual IPresenceFormatter Formatter
        {
            get => this.formatter;
            set => this.formatter = value ?? throw new ArgumentNullException(nameof(value));
        }

        public virtual bool IsPresenceActive { get; private set; }

        public virtual ILogger Logger { get; set; }

        public virtual IPresenceUpdateTrigger UpdateTrigger
        {
            get => this.updateTrigger;
            set
            {
                this.updateTrigger = value;
                if (this.updateTrigger != null)
                    this.updateTrigger.SetPresence(this);
            }
        }


        public virtual void Start()
        {
            if (this.presenceStarted)
                return;

            this.stopRequested = false;
            this.presenceStarted = true;

            this.rpcClient = new DiscordRpcClient(clientId)
            {
                Logger = new DiscordShellLogger()
                {
                    Level = LogLevel.Warning,
                    Logger = this.Logger
                }
            };
            this.rpcClient.OnConnectionEstablished += this.RpcClient_OnConnectionEstablished;
            this.rpcClient.OnConnectionFailed += this.RpcClient_OnConnectionFailed;
            this.rpcClient.Initialize();

            this.rpcEventLoopThread = new Thread(this.RpcEventLoop);
            this.rpcEventLoopThread.Start();
            this.rpcReconnectTimer.Start();
        }

        public virtual void Stop()
        {
            if (!this.presenceStarted)
                return;

            this.rpcClient?.DequeueAll();
            this.rpcClient?.Dispose();
            this.rpcClient = null;
            this.rpcReconnectTimer.Stop();
            this.stopRequested = true;
            this.presenceStarted = false;
            this.IsPresenceActive = false;
            this.PresenceDeactivated?.Invoke(this, null);
        }

        public virtual void SetSaliensPlayerState(PlayerInfoResponse playerInfo)
        {
            if (playerInfo == null)
                throw new ArgumentNullException(nameof(playerInfo));

            if (!this.IsPresenceActive)
                return;

            bool hasActivePlanet = !string.IsNullOrWhiteSpace(playerInfo.ActivePlanet);
            bool hasActiveZone = !string.IsNullOrWhiteSpace(playerInfo.ActiveZonePosition);

            this.rpcClient.SetPresence(this.Formatter.FormatPresence(playerInfo, this));
        }


        private void RpcEventLoop()
        {
            while (!this.stopRequested)
            {
                if (this.rpcClient != null && this.rpcClient.IsInitialized && !this.rpcClient.Disposed)
                {
                    try
                    {
                        this.rpcClient.Invoke();
                    }
                    catch (Exception) { }
                }
                Thread.Sleep(10);
            }
            this.rpcClient?.Dispose();
            this.rpcClient = null;
        }

        private void RpcClient_OnConnectionEstablished(object sender, DiscordRPC.Message.ConnectionEstablishedMessage args)
        {
            this.IsPresenceActive = true;
            this.PresenceActivated?.Invoke(this, null);
        }

        private void RpcClient_OnConnectionFailed(object sende, DiscordRPC.Message.ConnectionFailedMessage args)
        {
            this.IsPresenceActive = false;
            this.PresenceDeactivated?.Invoke(this, null);
        }


        public void Dispose()
        {
            this.rpcClient?.Dispose();
        }
    }
}
