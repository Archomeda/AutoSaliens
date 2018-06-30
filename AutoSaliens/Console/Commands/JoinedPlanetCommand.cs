using System.Threading;
using System.Threading.Tasks;
using AutoSaliens.Api;

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("joinedplanet")]
    internal class JoinedPlanetCommand : CommandBase
    {
        public override async Task RunAsync(string parameters, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(Program.Settings.Token))
            {
                this.Logger?.LogCommandOutput("{{warn}}No token has been set.");
                return;
            }

            var playerInfo = await SaliensApi.GetPlayerInfoAsync(Program.Settings.Token);
            if (string.IsNullOrWhiteSpace(playerInfo?.ActivePlanet))
            {
                this.Logger?.LogCommandOutput("No planet has been joined.");
                return;
            }

            var planet = await SaliensApi.GetPlanetAsync(playerInfo.ActivePlanet);
            this.Logger?.LogCommandOutput(planet.ToConsoleBlock());
        }
    }
}
