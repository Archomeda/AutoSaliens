using Newtonsoft.Json;

namespace AutoSaliens.Api.Models
{
    internal class JoinBossZoneResponse
    {
        public Zone ZoneInfo { get; set; }

        public bool WaitingForPlayers { get; set; }

        [JsonProperty(PropertyName = "gameid")]
        public string GameId { get; set; }
    }
}
