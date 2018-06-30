using System;
using AutoSaliens.Api.Converters;
using Newtonsoft.Json;

namespace AutoSaliens.Api.Models
{
    internal class BossPlayer
    {
        [JsonProperty(PropertyName = "accountid")]
        public int AccountId { get; set; }

        public ClanInfo ClanInfo { get; set; }

        [JsonConverter(typeof(TimestampConverter))]
        public DateTime TimeJoined { get; set; }

        [JsonConverter(typeof(TimestampConverter))]
        public DateTime TimeLastSeen { get; set; }

        public string Name { get; set; }

        public int Hp { get; set; }

        public int MaxHp { get; set; }

        public Salien Salien { get; set; }

        public string ScoreOnJoin { get; set; }

        public int LevelOnJoin { get; set; }

        public long XpEarned { get; set; }

        public int NewLevel { get; set; }

        public string NextLevelScore { get; set; }

        [JsonConverter(typeof(TimestampConverter))]
        public DateTime TimeLastHeal { get; set; }

        public bool WitnessedBossDefeat { get; set; }
    }
}
