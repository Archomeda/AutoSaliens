using System;
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
                return "{err}Unknown planet id.";

            if (planet.Zones == null || planet.Zones.Count == 0)
            {
                // Zones not available yet, request them manually
                var index = Program.Saliens.PlanetDetails.FindIndex(p => p.Id == planet.Id);
                planet = await SaliensApi.GetPlanet(planet.Id);
                Program.Saliens.PlanetDetails[index] = planet;
            }

            var active = planet.Zones.Where(z => !z.Captured);
            var captured = planet.Zones.Where(z => z.Captured);

            return $@"Zones on {{planet}}planet {planet.Id} ({planet.State.Name}){{reset}}
Captured zones:
{string.Join(Environment.NewLine, captured.Select(z => z.ToConsoleLine()))}

Active zones:
{string.Join(Environment.NewLine, active.Select(z => z.ToConsoleLine()))}

To see more information about a zone, use the command: {{command}}zone {{param}}<planet_id> <zone_pos>{{reset}}
where {{param}}<planet_id>{{reset}} is replaced with the planet id,
and {{param}}<zone_pos>{{reset}} is replaced with the zone position";
        }
    }
}
