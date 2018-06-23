using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("zones")]
    internal class ZonesCommand : CommandBase
    {
        public override async Task<string> Run(string parameters, CancellationToken cancellationToken)
        {
            if (Program.Saliens.PlanetDetails == null)
                return "No planet information available yet.";

            var planet = Program.Saliens.PlanetDetails.FirstOrDefault(p => p.Id == parameters);
            if (planet == null)
                return "Unknown planet id.";

            if (planet.Zones == null || planet.Zones.Count == 0)
            {
                // Zones not available yet, request them manually
                var index = Program.Saliens.PlanetDetails.FindIndex(p => p.Id == planet.Id);
                planet = await SaliensApi.GetPlanet(planet.Id);
                Program.Saliens.PlanetDetails[index] = planet;
            }

            var active = planet.Zones.Where(z => !z.Captured);
            var captured = planet.Zones.Where(z => z.Captured);

            return $@"Active:
{string.Join("\n", active.Select(z => $"{z.ZonePosition} - {z.Difficulty}, {z.Type}, {z.CaptureProgress.ToString("0.00%")}"))}

Captured:
{string.Join("\n", captured.Select(z => $"{z.ZonePosition} - {z.Difficulty}, {z.Type}"))}

To see more information about a zone, use the command: zone <planet_id> <zone_pos>
where <planet_id> is replaced with the planet id,
and <zone_pos> is replaced with the zone position";
        }
    }
}
