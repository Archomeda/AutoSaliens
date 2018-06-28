using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

            if (Program.Saliens.PlanetDetails == null)
            {
                this.Logger?.LogCommandOutput("No planet information available yet.");
                return;
            }

            var planet = Program.Saliens.PlanetDetails.FirstOrDefault(p => p.Id == split[0]);
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

            if (planet.Zones == null || planet.Zones.Count == 0)
            {
                var index = Program.Saliens.PlanetDetails.FindIndex(p => p.Id == planet.Id);
                planet = await SaliensApi.GetPlanetAsync(planet.Id);
                Program.Saliens.PlanetDetails[index] = planet;
            }

            var zone = planet.Zones.FirstOrDefault(z => z.ZonePosition == zonePos);
            if (zone == null)
            {
                this.Logger?.LogCommandOutput("{err}Unknown zone position.");
                return;
            }

            this.Logger?.LogCommandOutput(zone.ToConsoleBlock());
        }
    }
}
