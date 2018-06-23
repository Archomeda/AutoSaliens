using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace AutoSaliens.Api.Models
{
    internal class Zone
    {
        public int ZonePosition { get; set; }

        public Clan Leader { get; set; }

        public ZoneType Type { get; set; }

        [JsonProperty(PropertyName = "gameid")]
        public string GameId { get; set; }

        public Difficulty Difficulty { get; set; }

        public bool Captured { get; set; }

        public float CaptureProgress { get; set; }

        public List<ClanInfo> TopClans { get; set; }

        public string ToShortString()
        {
            return $@"Zone {this.ZonePosition}:
  Progress: {(this.Captured ? "Captured" : (this.CaptureProgress).ToString("0.00%"))}
  Difficulty: {this.Difficulty}";
        }

        public override string ToString()
        {
            return $@"Zone {this.ZonePosition}:
  GameId: {this.GameId}
  Progress: {(this.Captured ? "Captured" : (this.CaptureProgress).ToString("0.00%"))}
  Difficulty: {this.Difficulty}
  Type: {this.Type}
  Top clans: {(this.TopClans != null ? string.Join(", ", this.TopClans.Select(c => c.Name)) : "None")}";
        }
    }
}
