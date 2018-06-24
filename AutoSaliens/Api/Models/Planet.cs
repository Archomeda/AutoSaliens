using System;
using System.Collections.Generic;
using System.Linq;

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

        public string ToShortString()
        {
            return $@"{this.State.Name} ({this.Id}), {this.State.Difficulty} - {(this.State.CaptureProgress).ToString("0.00%")} - {this.State.CurrentPlayers.ToString("#,##0")}/{this.State.TotalJoins.ToString("#,##0")}:
  {string.Join("\n  ", this.ZonesToDifficulityCapturedCountString(false))}";
        }

        public override string ToString()
        {
            return $@"{this.State.Name} ({this.Id}):
  Started: {(this.State.ActivationTime > new DateTime(1970, 1,1) ? this.State.ActivationTime.ToString("yyyy-MM-dd HH:mm:ss zzz") : "Not yet")}
  Captured: {(this.State.Captured ? this.State.CaptureTime.ToString("yyyy-MM-dd HH:mm:ss zzz") : "Not yet")}
  Progress: {(this.State.CaptureProgress).ToString("0.00%")}
  Difficulty: {this.State.Difficulty}
  Priority: {this.State.Priority}
  Current players: {this.State.CurrentPlayers.ToString("#,##0")}
  Total joins: {this.State.TotalJoins.ToString("#,##0")}
  Top clans: {(this.TopClans != null ? string.Join(", ", this.TopClans.Select(c => $"{c.ClanInfo.Name} ({c.NumZonesControlled})")) : "None")}
  Zones:
    {string.Join("\n    ", this.ZonesToDifficulityCapturedCountString())}";

        }
    }
}
