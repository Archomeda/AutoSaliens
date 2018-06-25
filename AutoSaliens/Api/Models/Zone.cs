using System;
using System.Collections.Generic;
using System.Linq;
using AutoSaliens.Utils;
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

        public string ToConsoleLine()
        {
            var difficulty = MathUtils.ScaleColor((int)this.Difficulty - 1, (int)Difficulty.High - 1, new[] { "{svlow}", "{smed}", "{svhigh}" });
            var progress = this.CaptureProgress == 0 ? "" :
                MathUtils.ScaleColor((int)(this.CaptureProgress * 100), 100, new[] { "{svlow}", "{slow}", "{smed}", "{shigh}", "{svhigh}" });

            return $"{{zone}}Z {this.ZonePosition.ToString().PadLeft(3)}{{reset}} - " +
                $"{difficulty}{this.Difficulty.ToString().PadLeft(6)}{{reset}} - " +
                $"{progress}{(this.Captured ? 1 : this.CaptureProgress).ToString("0.##%").PadLeft(7)}{{reset}}";
        }

        public string ToConsoleBlock()
        {
            var difficulty = MathUtils.ScaleColor((int)this.Difficulty - 1, (int)Difficulty.High - 1, new[] { "{svlow}", "{smed}", "{svhigh}" });
            var progress = this.CaptureProgress == 0 ? "" :
                MathUtils.ScaleColor((int)(this.CaptureProgress * 100), 100, new[] { "{svlow}", "{slow}", "{smed}", "{shigh}", "{svhigh}" });

            return $"{{zone}}Zone {this.ZonePosition.ToString()}{{reset}}{Environment.NewLine}" +
                $"GameId: {this.GameId}{Environment.NewLine}" +
                $"Progress: {progress}{(this.Captured ? 1 : this.CaptureProgress).ToString("0.##%")}{{reset}}{Environment.NewLine}" +
                $"Difficulty: {difficulty}{this.Difficulty.ToString()}{{reset}}{Environment.NewLine}" +
                $"Type: {this.Type}{Environment.NewLine}" +
                $"Top clans: {(this.TopClans != null ? string.Join(", ", this.TopClans.Select(c => c.Name)) : "None")}";
        }
    }
}
