using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoSaliens.Api;

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("planet")]
    internal class PlanetCommand : CommandBase
    {
        public override async Task RunAsync(string parameters, CancellationToken cancellationToken)
        {
            var planet = await SaliensApi.GetPlanetAsync(parameters);
            if (planet == null)
            {
                this.Logger?.LogCommandOutput("{err}Unknown planet id.");
                return;
            }
            this.Logger?.LogCommandOutput(planet.ToConsoleBlock());
        }
    }
}
