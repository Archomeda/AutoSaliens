using System.Globalization;
using AutoSaliens.Api;
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
            bool hasActiveBossZone = !string.IsNullOrWhiteSpace(playerInfo.ActiveBossGame);

            var state = "Inactive";
            if (hasActivePlanet && (hasActiveZone || hasActiveBossZone))
            {
                state = $"Planet {playerInfo.ActivePlanet} - ";
                if (hasActiveBossZone)
                    state += $"Boss Zone";
                else if (hasActiveZone)
                    state += $"Zone {playerInfo.ActiveZonePosition}";
                var planet = SaliensApi.GetPlanet(playerInfo.ActivePlanet);
                if (hasActiveZone && int.TryParse(playerInfo.ActiveZonePosition, out int zonePos))
                {
                    var zone = planet.Zones[zonePos];
                    if (zone != null)
                        state += $" ({zone.RealDifficulty.ToString().Substring(0, 1)})";
                }
            }
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
            string smallImageKey = null;
            string smallImageText = null;
            switch (playerInfoResponse.Level)
            {
                case var level when level > 0 && level < 6:
                    smallImageKey = "badge1";
                    smallImageText = "Rank 1";
                    break;
                case var level when level >= 6 && level < 9:
                    smallImageKey = "badge2";
                    smallImageText = "Rank 2";
                    break;
                case var level when level >= 9 && level < 11:
                    smallImageKey = "badge3";
                    smallImageText = "Rank 3";
                    break;
                case var level when level >= 11 && level < 16:
                    smallImageKey = "badge4";
                    smallImageText = "Rank 4";
                    break;
                case var level when level >= 16 && level < 21:
                    smallImageKey = "badge5";
                    smallImageText = "Rank 5";
                    break;
                case var level when level >= 21:
                    smallImageKey = "badge6";
                    smallImageText = "Rank 6";
                    break;
            }
            return new Assets
            {
                SmallImageKey = smallImageKey,
                SmallImageText = smallImageText,
                LargeImageKey = !string.IsNullOrWhiteSpace(playerInfoResponse.ActiveBossGame) ? "logo_boss" : "logo_large",
                LargeImageText = "Steam Summer Saliens"
            };
        }
    }
}
