using System;
using System.Collections.Generic;
using System.Linq;
using AutoSaliens.Utils;

namespace AutoSaliens.Api.Models
{
    internal class Planet
    {
        public string Id { get; set; }

        public PlanetState State { get; set; }

        public List<int> GiveawayApps { get; set; }

        public List<Clan> TopClans { get; set; }

        public List<Zone> Zones { get; set; }

        public string[] ZonesToDifficulityCapturedCountString(bool includeCaptured = true)
        {
            return this.Zones == null ? new string[0] : this.Zones
                .GroupBy(z => z.Difficulty)
                .ToDictionary(g => g.Key, g => g.ToList())
                .OrderBy(kvp => kvp.Key)
                .Where(kvp => includeCaptured || (!includeCaptured && kvp.Value.Count(z => z.Captured) < kvp.Value.Count))
                .Select(kvp => $"{kvp.Key}: {kvp.Value.Count - kvp.Value.Count(z => z.Captured)}/{kvp.Value.Count} free")
                .ToArray();
        }

        public string ToConsoleLine()
        {
            var difficulty = MathUtils.ScaleColor((int)this.State.Difficulty - 1, (int)Difficulty.High - 1, new[] { "{svlow}", "{smed}", "{svhigh}" });
            var progress = !this.State.Running ? "" :
                MathUtils.ScaleColor((int)(this.State.CaptureProgress * 100), 100, new[] { "{svlow}", "{slow}", "{smed}", "{shigh}", "{svhigh}" });
            var zones = new[] { Difficulty.Low, Difficulty.Medium, Difficulty.High }.Select(d =>
            {
                if (this.Zones == null)
                    return $"Z{d.ToString().Substring(0, 1)}  ?/ ?";

                var total = this.Zones.Count(z => z.Difficulty == d);
                var free = this.Zones.Count(z => z.Difficulty == d && !z.Captured);
                var color = !this.State.Running ? "" :
                   MathUtils.ScaleColor(total - free, total, new[] { "{svlow}", "{slow}", "{smed}", "{shigh}", "{svhigh}" });
                return $"{color}Z{d.ToString().Substring(0, 1)} {free.ToString().PadLeft(2)}/{total.ToString().PadLeft(2)}{{reset}}";
            });
            string bosses;
            if (this.Zones == null)
                bosses = "ZB  ?/ ?";
            else
            {
                var bossTotal = this.Zones.Count(z => z.Type == ZoneType.Boss);
                var bossFree = this.Zones.Count(z => z.Type == ZoneType.Boss && !z.Captured);
                var bossZones = !this.State.Running ? "" :
                    MathUtils.ScaleColor(bossTotal - bossFree, bossTotal, new[] { "{svlow}", "{slow}", "{smed}", "{shigh}", "{svhigh}" });
                bosses = $"{bossZones}ZB {bossFree.ToString().PadLeft(2)}/{bossTotal.ToString().PadLeft(2)}{{reset}}";
            }

            return $"{{planet}}P {this.Id.PadLeft(3)}{{reset}} - " +
                $"{difficulty}{this.State.Difficulty.ToString().PadLeft(6)}{{reset}} - " +
                $"{progress}{this.State.CaptureProgress.ToString("0.##%").PadLeft(7)}{{reset}} - " +
                string.Join(" - ", zones) + " - " +
                $"{bosses} - " +
                $"{{planet}}({this.State.Name})";
        }

        public string ToConsoleBlock()
        {
            var difficulty = MathUtils.ScaleColor((int)this.State.Difficulty - 1, (int)Difficulty.High - 1, new[] { "{svlow}", "{smed}", "{svhigh}" });
            var progress = !this.State.Active ? "" :
                MathUtils.ScaleColor((int)(this.State.CaptureProgress * 100), 100, new[] { "{svlow}", "{slow}", "{smed}", "{shigh}", "{svhigh}" });

            var zones = new[] { Difficulty.Low, Difficulty.Medium, Difficulty.High }.Select(d =>
            {
                if (this.Zones == null)
                    return $"{($"{d.ToString()}:").PadRight(7)} ?/ ?";

                var total = this.Zones.Count(z => z.Difficulty == d);
                var free = this.Zones.Count(z => z.Difficulty == d && !z.Captured);
                var color = !this.State.Running ? "" :
                    MathUtils.ScaleColor(total - free, total, new[] { "{svlow}", "{slow}", "{smed}", "{shigh}", "{svhigh}" });
                return $"{color}{($"{d.ToString()}:").PadRight(7)} {free.ToString().PadLeft(2)}/{total.ToString().PadLeft(2)}{{reset}}";
            });
            string bosses;
            if (this.Zones == null)
                bosses = "Boss:    ?/ ?";
            else
            {
                var bossTotal = this.Zones.Count(z => z.Type == ZoneType.Boss);
                var bossFree = this.Zones.Count(z => z.Type == ZoneType.Boss && !z.Captured);
                var bossZones = !this.State.Running ? "" :
                    MathUtils.ScaleColor(bossTotal - bossFree, bossTotal, new[] { "{svlow}", "{slow}", "{smed}", "{shigh}", "{svhigh}" });
                bosses = $"{bossZones}Boss:   {bossFree.ToString().PadLeft(2)}/{bossTotal.ToString().PadLeft(2)}{{reset}}";
            }

            return $"{{planet}}{this.Id}: {this.State.Name}{{reset}}{Environment.NewLine}" +
                $"Started: {(this.State.ActivationTime > new DateTime(1970, 1, 1) ? this.State.ActivationTime.ToString("yyyy-MM-dd HH:mm:ss") : "Not yet")}{Environment.NewLine}" +
                $"Captured: {(this.State.Captured ? this.State.CaptureTime.ToString("yyyy-MM-dd HH:mm:ss") : "Not yet")}{Environment.NewLine}" +
                $"Progress: {progress}{(this.State.CaptureProgress).ToString("0.00%")}{{reset}}{Environment.NewLine}" +
                $"Difficulty: {difficulty}{this.State.Difficulty}{{reset}}{Environment.NewLine}" +
                $"Priority: {this.State.Priority}{Environment.NewLine}" +
                $"Current players: {this.State.CurrentPlayers.ToString("#,##0")}{Environment.NewLine}" +
                $"Total joins: {this.State.TotalJoins.ToString("#,##0")}{Environment.NewLine}" +
                $"Top clans: {(this.TopClans != null ? string.Join(", ", this.TopClans.Select(c => $"{c.ClanInfo.Name} ({c.NumZonesControlled})")) : "None")}{Environment.NewLine}" +
                $"Zones:{Environment.NewLine}" +
                $"  {string.Join($"{Environment.NewLine}  ", zones)}{Environment.NewLine}" +
                $"  {bosses}";
        }
    }
}
