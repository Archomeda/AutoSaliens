using System;
using AutoSaliens.Api.Models;
using DiscordRPC;

namespace AutoSaliens.Presence.Formatters
{
    internal class PresenceTimeNextLevelEstimationFormatter : PresenceGenericFormatter
    {
        public long LastXp { get; private set; }

        public DateTime MeasureStartTime { get; private set; }

        public DateTime PredictedLevelUpDate { get; private set; }


        protected override Timestamps FormatTimestamps(PlayerInfoResponse playerInfo, DiscordPresence presence)
        {
            bool hasActivePlanet = !string.IsNullOrWhiteSpace(playerInfo.ActivePlanet);
            bool hasActiveZone = !string.IsNullOrWhiteSpace(playerInfo.ActiveZonePosition);
            bool hasActiveBossZone = !string.IsNullOrWhiteSpace(playerInfo.ActiveBossGame);

            Timestamps timestamps = null;

            if (!hasActiveBossZone)
            {
                if (hasActivePlanet && hasActiveZone &&
                    long.TryParse(playerInfo.Score, out long xp) &&
                    long.TryParse(playerInfo.NextLevelScore, out long nextLevelXp))
                {
                    if (xp > this.LastXp)
                    {
                        if (this.LastXp > 0 && this.MeasureStartTime.Ticks > 0)
                        {
                            int diffXp = (int)(xp - this.LastXp);
                            var diffTime = DateTime.Now - this.MeasureStartTime - playerInfo.TimeInZone;
                            var eta = TimeSpan.FromSeconds(diffTime.TotalSeconds * ((double)(nextLevelXp - xp) / diffXp));
                            this.PredictedLevelUpDate = DateTime.Now + eta - playerInfo.TimeInZone;
                        }
                        this.MeasureStartTime = DateTime.Now - playerInfo.TimeInZone;
                    }
                    this.LastXp = xp;
                }

                // Only show when it's less than a day: Discord doesn't show days
                if (this.PredictedLevelUpDate > DateTime.Now && this.PredictedLevelUpDate < DateTime.Now.AddDays(1))
                    timestamps = new Timestamps { End = this.PredictedLevelUpDate.ToUniversalTime() };
            }

            // Fallbacks
            if (timestamps == null && hasActivePlanet && (hasActiveZone || hasActiveBossZone))
                timestamps = new Timestamps { Start = (DateTime.Now - playerInfo.TimeInZone).ToUniversalTime() };

            if (timestamps == null && hasActivePlanet)
                timestamps = new Timestamps { Start = (DateTime.Now - playerInfo.TimeOnPlanet).ToUniversalTime() };

            if (hasActivePlanet && hasActiveZone)
                this.MeasureStartTime = DateTime.Now - playerInfo.TimeInZone;

            return timestamps;
        }
    }
}
