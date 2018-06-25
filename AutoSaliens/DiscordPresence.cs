using System;
using System.Globalization;
using AutoSaliens.Api.Models;
using DiscordRPC;
using DiscordRPC.Logging;

namespace AutoSaliens
{
    internal class DiscordPresence : IDisposable
    {
        private const string clientId = "460795723881906186";

        private DiscordRpcClient rpcClient;

        public void Initialize()
        {
            this.rpcClient = new DiscordRpcClient(clientId)
            {
                Logger = new DiscordPresenceLogger() { Level = LogLevel.Warning }
            };
            this.rpcClient.Initialize();
        }

        public void SetPresence(RichPresence presence)
        {
            if (this.rpcClient.IsInitialized)
                this.rpcClient.SetPresence(presence);
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

            this.SetPresence(new RichPresence
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

        public void Dispose() => this.rpcClient.Dispose();

    }
}
