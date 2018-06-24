using System.Net;
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
                return "No token has been set.";

            try
            {
                await Program.Saliens.UpdatePlayerInfo();
            }
            catch (WebException ex)
            {
                return $"Invalid response. {ex.Message}";
            }

            var info = Program.Saliens.PlayerInfo;

            return $@"Level: {info.Level}
Score: {long.Parse(info.Score).ToString("#,##0")}
Next level score: {long.Parse(info.NextLevelScore).ToString("#,##0")}
Clan: {info.ClanInfo.Name}
Active planet: {info.ActivePlanet}
Time spent on planet: {info.TimeOnPlanet.ToString()}
Active zone position: {info.ActiveZonePosition}
Active zone game: {info.ActiveZoneGame}
Time spent in zone: {info.TimeInZone.TotalSeconds} seconds";
        }
    }
}
