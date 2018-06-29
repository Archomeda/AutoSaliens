using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoSaliens.Api;

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("zones")]
    internal class ZonesCommand : CommandBase
    {
        public override async Task RunAsync(string parameters, CancellationToken cancellationToken)
        {
            var planet = await SaliensApi.GetPlanetAsync(parameters);
            if (planet == null)
            {
                this.Logger?.LogCommandOutput("{err}Unknown planet id.");
                return;
            }

            var active = planet.Zones.Where(z => !z.Captured);
            var captured = planet.Zones.Where(z => z.Captured);

            this.Logger?.LogCommandOutput($"Zones on {{planet}}planet {planet.Id} ({planet.State.Name}){{reset}}{Environment.NewLine}" +
                $"Captured zones:{Environment.NewLine}" +
                $"{string.Join(Environment.NewLine, captured.Select(z => z.ToConsoleLine()))}{Environment.NewLine}{Environment.NewLine}" +

                $"Active zones:{Environment.NewLine}" +
                $"{string.Join(Environment.NewLine, active.Select(z => z.ToConsoleLine()))}{Environment.NewLine}{Environment.NewLine}" +

                $"To see more information about a zone, use the command: {{command}}zone {{param}}<planet_id> <zone_pos>{{reset}}{Environment.NewLine}" +
                $"where {{param}}<planet_id>{{reset}} is replaced with the planet id,{Environment.NewLine}" +
                $"and {{param}}<zone_pos>{{reset}} is replaced with the zone position");
        }
    }
}
