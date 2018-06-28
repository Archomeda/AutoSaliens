using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("planet")]
    internal class PlanetCommand : CommandBase
    {
        public override async Task RunAsync(string parameters, CancellationToken cancellationToken)
        {
            if (Program.Saliens.PlanetDetails == null)
            {
                this.Logger?.LogCommandOutput("No planet information available yet.");
                return;
            }

            var planet = Program.Saliens.PlanetDetails.FirstOrDefault(p => p.Id == parameters);
            if (planet == null)
            {
                this.Logger?.LogCommandOutput("{err}Unknown planet id.");
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
