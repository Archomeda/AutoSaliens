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

            return $@"Level: {{level}}{info.Level}{{reset}}
XP: {{xp}}{long.Parse(info.Score).ToString("#,##0")}{{reset}} (required for next level: {{reqxp}}{long.Parse(info.NextLevelScore).ToString("#,##0")}{{reset}})
Clan: {info.ClanInfo.Name}
Active planet: {{planet}}{info.ActivePlanet} {{reset}}
Time spent on planet: {info.TimeOnPlanet.ToString()}
Active zone: {{zone}}{info.ActiveZonePosition} ({info.ActiveZoneGame}){{reset}}
Time spent in zone: {info.TimeInZone.TotalSeconds} seconds";
        }
    }
}
