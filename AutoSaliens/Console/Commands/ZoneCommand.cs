using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoSaliens.Api;

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("zone")]
    internal class ZoneCommand : CommandBase
    {
        public override async Task RunAsync(string parameters, CancellationToken cancellationToken)
        {
            var split = parameters.Split(' ');
            if (split.Length != 2)
            {
                this.Logger?.LogCommandOutput("{err}Invalid amount of parameters.");
                return;
            }

            var planet = await SaliensApi.GetPlanetAsync(split[0]);
            if (planet == null)
            {
                this.Logger?.LogCommandOutput("{err}Unknown planet id.");
                return;
            }

            if (!int.TryParse(split[1], out int zonePos))
            {
                this.Logger?.LogCommandOutput("{err}Invalid zone position.");
                return;
            }

            if (zonePos >= planet.Zones.Count)
            {
                this.Logger?.LogCommandOutput("{err}Unknown zone position.");
                return;
            }

            this.Logger?.LogCommandOutput(planet.Zones[zonePos].ToConsoleBlock());
        }
    }
}
