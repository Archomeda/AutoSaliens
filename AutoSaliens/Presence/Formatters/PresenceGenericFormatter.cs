using System.Globalization;
using AutoSaliens.Api.Models;
using DiscordRPC;

namespace AutoSaliens.Presence.Formatters
{
    internal class PresenceGenericFormatter : PresenceFormatterBase
    {
        protected override string FormatDetails(PlayerInfoResponse playerInfo, DiscordPresence presence)
        {
            var details = $"Level {playerInfo.Level}";
            if (long.TryParse(playerInfo.Score, out long xp))
                details += $" - {xp.ToString("#,##0", CultureInfo.InvariantCulture)} XP";
            return details;
        }

        protected override string FormatState(PlayerInfoResponse playerInfo, DiscordPresence presence)
        {
            bool hasActivePlanet = !string.IsNullOrWhiteSpace(playerInfo.ActivePlanet);
            bool hasActiveZone = !string.IsNullOrWhiteSpace(playerInfo.ActiveZonePosition);

            var state = "Inactive";
            if (hasActivePlanet && hasActiveZone)
                state = $"Planet {playerInfo.ActivePlanet} - Zone {playerInfo.ActiveZonePosition}";
            else if (hasActivePlanet && !hasActiveZone)
                state = $"Planet {playerInfo.ActivePlanet}";
            return state;
        }

        protected override Timestamps FormatTimestamps(PlayerInfoResponse playerInfo, DiscordPresence presence)
        {
            return null;
        }

        protected override Assets GetAssets(PlayerInfoResponse playerInfoResponse, DiscordPresence presence)
        {
            return new Assets
            {
                LargeImageKey = "logo_large"
            };
        }
    }
}
