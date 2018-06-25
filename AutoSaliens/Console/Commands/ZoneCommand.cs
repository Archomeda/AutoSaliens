using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("zone")]
    internal class ZoneCommand : CommandBase
    {
        public override async Task<string> Run(string parameters, CancellationToken cancellationToken)
        {
            var split = parameters.Split(' ');
            if (split.Length != 2)
                return "{err}Invalid amount of parameters.";

            if (Program.Saliens.PlanetDetails == null)
                return "No planet information available yet.";

            var planet = Program.Saliens.PlanetDetails.FirstOrDefault(p => p.Id == split[0]);
            if (planet == null)
                return "{err}Unknown planet id.";

            if (!int.TryParse(split[1], out int zonePos))
                return "{err}Invalid zone position.";

            if (planet.Zones == null || planet.Zones.Count == 0)
            {
                var index = Program.Saliens.PlanetDetails.FindIndex(p => p.Id == planet.Id);
                planet = await SaliensApi.GetPlanet(planet.Id);
                Program.Saliens.PlanetDetails[index] = planet;
            }

            var zone = planet.Zones.FirstOrDefault(z => z.ZonePosition == zonePos);
            if (zone == null)
                return "{err}Unknown zone position.";

            return zone.ToConsoleBlock();
        }
    }
}
