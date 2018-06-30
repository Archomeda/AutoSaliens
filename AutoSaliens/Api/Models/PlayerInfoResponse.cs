using System;
using AutoSaliens.Api.Converters;
using Newtonsoft.Json;

namespace AutoSaliens.Api.Models
{
    internal class PlayerInfoResponse
    {
        public string ActivePlanet { get; set; }

        public string ActiveBossGame { get; set; }

        public string ActiveZoneGame { get; set; }

        public string ActiveZonePosition { get; set; }

        public ClanInfo ClanInfo { get; set; }

        public int Level { get; set; }

        public string NextLevelScore { get; set; }

        public string Score { get; set; }

        [JsonConverter(typeof(TimeConverter))]
        public TimeSpan TimeInZone { get; set; }

        [JsonConverter(typeof(TimeConverter))]
        public TimeSpan TimeOnPlanet { get; set; }
    }

    internal static class PlayerInfoExtensions
    {
        public static bool HasActivePlanet(this PlayerInfoResponse playerInfo) => !string.IsNullOrWhiteSpace(playerInfo.ActivePlanet);

        public static bool HasActiveZone(this PlayerInfoResponse playerInfo) => !string.IsNullOrWhiteSpace(playerInfo.ActiveZoneGame);
    }
}
