using System;
using AutoSaliens.Api.Converters;
using Newtonsoft.Json;

namespace AutoSaliens.Api.Models
{
    internal class PlanetState
    {
        public string Name { get; set; }

        public string ImageFilename { get; set; }

        public string MapFilename { get; set; }

        public string CloudFilename { get; set; }

        public string LandFilename { get; set; }

        public Difficulty Difficulty { get; set; }

        public string GiveawayId { get; set; }

        public bool Active { get; set; }

        [JsonConverter(typeof(TimestampConverter))]
        public DateTime ActivationTime { get; set; }

        public int Position { get; set; }

        public bool Captured { get; set; }

        public float CaptureProgress { get; set; }

        [JsonConverter(typeof(TimestampConverter))]
        public DateTime CaptureTime { get; set; }

        public int TotalJoins { get; set; }

        public int CurrentPlayers { get; set; }

        public int Priority { get; set; }

        public string TagIds { get; set; }

        public int? BossZonePosition { get; set; }

        [JsonIgnore]
        public bool Running => this.Active && !this.Captured;
    }
}
