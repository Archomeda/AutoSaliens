using System;
using AutoSaliens.Api.Models;
using DiscordRPC;

namespace AutoSaliens.Presence.Formatters
{
    internal class PresenceTimeNextLevelEstimationFormatter : PresenceGenericFormatter
    {
        public long LastXp { get; private set; }

        public DateTime MeasureStartTime { get; private set; }


        protected override Timestamps FormatTimestamps(PlayerInfoResponse playerInfo, DiscordPresence presence)
        {
            bool hasActivePlanet = !string.IsNullOrWhiteSpace(playerInfo.ActivePlanet);
            bool hasActiveZone = !string.IsNullOrWhiteSpace(playerInfo.ActiveZonePosition);

            Timestamps timestamps = null;

            if (hasActivePlanet && hasActiveZone &&
                long.TryParse(playerInfo.Score, out long xp) &&
                long.TryParse(playerInfo.NextLevelScore, out long nextLevelXp))
            {
                if (this.LastXp > 0 && xp > this.LastXp)
                {
                    int diffXp = (int)(xp - this.LastXp);
                    TimeSpan diffTime = DateTime.Now - this.MeasureStartTime - playerInfo.TimeInZone;
                    TimeSpan eta = TimeSpan.FromSeconds(diffTime.TotalSeconds * ((nextLevelXp - xp) / diffXp));
                    if (eta < TimeSpan.FromDays(1))
                    {
                        // Only show when it's less than a day: Discord doesn't show days
                        DateTime predictedLevelUpDate = DateTime.Now + eta - playerInfo.TimeInZone;
                        timestamps = new Timestamps { End = predictedLevelUpDate.ToUniversalTime() };
                    }
                }

                this.LastXp = xp;
            }

            if (timestamps == null && hasActivePlanet && hasActiveZone)
                timestamps = new Timestamps { Start = (DateTime.Now - playerInfo.TimeInZone).ToUniversalTime() };

            if (timestamps == null && hasActivePlanet)
                timestamps = new Timestamps { Start = (DateTime.Now - playerInfo.TimeOnPlanet).ToUniversalTime() };

            if (hasActivePlanet && hasActiveZone)
                this.MeasureStartTime = DateTime.Now - playerInfo.TimeInZone;

            return timestamps;
        }
    }
}
