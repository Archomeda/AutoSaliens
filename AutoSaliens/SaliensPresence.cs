using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using AutoSaliens.Api.Models;
using AutoSaliens.Console;
using DiscordRPC;

namespace AutoSaliens
{
    internal class SaliensPresence : IDisposable
    {
        private const int refreshRate = 60;

        private CancellationTokenSource checkerCancelSource;
        private Thread checkerThread;
        private DiscordPresence presence;


        public bool Active { get; private set; }

        public bool Started { get; private set; }

        public bool CheckPeriodically
        {
            get => this.checkerThread != null;
            set
            {
                if (value && this.checkerThread == null)
                {
                    this.checkerCancelSource = new CancellationTokenSource();
                    this.checkerThread = new Thread(this.Presence_Thread);
                    this.checkerThread.Start();
                }
                else if (!value && this.checkerThread != null)
                {
                    this.checkerCancelSource.Cancel();
                }
            }
        }

        private void Presence_OnEnable(object sender, EventArgs e)
        {
            this.Active = true;
            Shell.WriteLine("{inf}Discord presence is available.");
        }

        private void Presence_OnDisable(object sender, EventArgs e)
        {
            this.Active = false;
            Shell.WriteLine("{warn}Discord presence is unavailable.");
        }


        private async void Presence_Thread()
        {
            while (!this.checkerCancelSource.IsCancellationRequested)
            {
                if (string.IsNullOrWhiteSpace(Program.Settings.Token))
                    return;

                TimeSpan timeToWait = TimeSpan.FromSeconds(refreshRate);
                try
                {
                    var playerInfo = await SaliensApi.GetPlayerInfo(Program.Settings.Token);
                    this.SetSaliensPlayerState(playerInfo);
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
                    Shell.WriteLine(Shell.FormatExceptionOutput(ex));
                }

                this.checkerCancelSource.Token.WaitHandle.WaitOne(timeToWait);
            }
        }



        public Task Start()
        {
            if (this.Started)
                return Task.Run(() => { });

            this.Started = true;
            return Task.Run(() =>
            {
                this.presence = new DiscordPresence();
                this.presence.OnEnable += this.Presence_OnEnable;
                this.presence.OnDisable += this.Presence_OnDisable;
                this.presence.Initialize();
            });
        }

        public void Stop()
        {
            if (!this.Started)
                return;

            this.presence.Dispose();
            this.Started = false;
        }

        public void SetSaliensPlayerState(PlayerInfoResponse playerInfo)
        {
            string details = $"Level {playerInfo.Level}";
            if (long.TryParse(playerInfo.Score, out long xp))
                details += $" - {xp.ToString("#,##0", CultureInfo.InvariantCulture)} XP";

            string state = "Inactive";
            if (!string.IsNullOrWhiteSpace(playerInfo.ActivePlanet) && !string.IsNullOrWhiteSpace(playerInfo.ActiveZonePosition))
                state = $"Planet {playerInfo.ActivePlanet} - Zone {playerInfo.ActiveZonePosition}";
            else if (!string.IsNullOrWhiteSpace(playerInfo.ActivePlanet) && string.IsNullOrWhiteSpace(playerInfo.ActiveZonePosition))
                state = $"Planet {playerInfo.ActivePlanet}";

            Timestamps time = null;
            if (!string.IsNullOrWhiteSpace(playerInfo.ActiveZonePosition))
                time = new Timestamps { Start = (DateTime.Now - playerInfo.TimeInZone).ToUniversalTime() };
            else if (!string.IsNullOrWhiteSpace(playerInfo.ActivePlanet))
                time = new Timestamps { Start = (DateTime.Now - playerInfo.TimeOnPlanet).ToUniversalTime() };

            this.presence.SetPresence(new RichPresence
            {
                Details = details,
                State = state,
                Timestamps = time,
                Assets = new Assets
                {
                    LargeImageKey = "logo_large"
                }
            });
        }

        public void Dispose()
        {
            this.checkerCancelSource.Dispose();
            this.presence.Dispose();
        }
    }
}
