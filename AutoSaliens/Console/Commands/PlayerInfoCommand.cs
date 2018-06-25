using System;
using System.Threading;
using System.Threading.Tasks;

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("playerinfo")]
    internal class PlayerInfoCommand : CommandBase
    {
        public override async Task<string> Run(string parameters, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(Program.Saliens.Token))
                return "{warn}No token has been set.";

            var info = await SaliensApi.GetPlayerInfo(Program.Settings.Token);
            Program.Saliens.PlayerInfo = info;

            return $"Level: {{level}}{info.Level}{{reset}}{Environment.NewLine}" +
                $"XP: {{xp}}{long.Parse(info.Score).ToString("#,##0")}{{reset}} (required for next level: {{reqxp}}{long.Parse(info.NextLevelScore).ToString("#,##0")}{{reset}}){Environment.NewLine}" +
                $"Clan: {info.ClanInfo.Name}{Environment.NewLine}" +
                $"Active planet: {{planet}}{info.ActivePlanet} {{reset}}{Environment.NewLine}" +
                $"Time spent on planet: {info.TimeOnPlanet.ToString()}{Environment.NewLine}" +
                $"Active zone: {{zone}}{info.ActiveZonePosition} ({info.ActiveZoneGame}){{reset}}{Environment.NewLine}" +
                $"Time spent in zone: {info.TimeInZone.TotalSeconds} seconds";
        }
    }
}
