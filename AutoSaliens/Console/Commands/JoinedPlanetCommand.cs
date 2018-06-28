using System.Threading;
using System.Threading.Tasks;

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("joinedplanet")]
    internal class JoinedPlanetCommand : CommandBase
    {
        public override async Task RunAsync(string parameters, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(Program.Saliens.Token))
            {
                this.Logger?.LogCommandOutput("{{warn}}No token has been set.");
                return;
            }

            if (Program.Saliens.PlanetDetails == null)
            {
                this.Logger?.LogCommandOutput("No planet information available yet.");
                return;
            }

            var planet = Program.Saliens.JoinedPlanet;
            if (planet == null)
            {
                this.Logger?.LogCommandOutput("No planet has been joined.");
                return;
            }

            if (planet.Zones == null)
            {
                planet = await SaliensApi.GetPlanetAsync(parameters);
                var index = Program.Saliens.PlanetDetails.FindIndex(p => p.Id == parameters);
                Program.Saliens.PlanetDetails[index] = planet;
            }

            this.Logger?.LogCommandOutput(planet.ToConsoleBlock());
        }
    }
}
