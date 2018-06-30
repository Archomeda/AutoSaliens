using System;
using System.Threading;
using AutoSaliens.Api;
using AutoSaliens.Console;

namespace AutoSaliens.Presence
{
    internal class ApiIntervalUpdateTrigger : IPresenceUpdateTrigger, IDisposable
    {
        private const int fallbackRefreshRate = 60;

        private CancellationTokenSource cancelSource;
        private IPresence presence;
        private Thread thread;

        public ApiIntervalUpdateTrigger(string token)
        {
            this.ApiToken = token;
        }

        public virtual string ApiToken { get; set; }


        public virtual void Start()
        {
            this.cancelSource = new CancellationTokenSource();
            this.thread = new Thread(this.PresenceLoopThread);
            this.thread.Start();
        }

        public virtual void Stop()
        {
            this.cancelSource.Cancel();
        }

        public virtual void SetPresence(IPresence presence) =>
            this.presence = presence ?? throw new ArgumentNullException(nameof(presence));


        private void PresenceLoopThread()
        {
            while (!this.cancelSource.Token.IsCancellationRequested)
            {
                if (string.IsNullOrWhiteSpace(this.ApiToken))
                    return;

                TimeSpan timeToWait = TimeSpan.FromSeconds(fallbackRefreshRate);
                try
                {
                    var playerInfo = SaliensApi.GetPlayerInfo(this.ApiToken);
                    this.presence.Logger?.LogMessage($"Updating Discord presence with {playerInfo.Score} XP");
                    this.presence.SetSaliensPlayerState(playerInfo);
                    if (!string.IsNullOrWhiteSpace(playerInfo.ActiveZonePosition))
                    {
                        if (playerInfo.TimeInZone < TimeSpan.FromSeconds(110))
                            timeToWait = TimeSpan.FromSeconds(110) - playerInfo.TimeInZone;
                        else if (playerInfo.TimeInZone < TimeSpan.FromSeconds(120))
                            timeToWait = TimeSpan.FromSeconds(120) - playerInfo.TimeInZone;
                        timeToWait += TimeSpan.FromSeconds(5);
                    }
                }
                catch (Exception ex)
                {
                    this.presence.Logger?.LogException(ex);
                }

                this.cancelSource.Token.WaitHandle.WaitOne(timeToWait);
            }
        }

        public void Dispose() => this.cancelSource.Dispose();
    }
}
