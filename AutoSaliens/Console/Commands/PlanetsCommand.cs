using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoSaliens.Api;
using AutoSaliens.Api.Models;

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("planets")]
    internal class PlanetsCommand : CommandBase
    {
        public override async Task RunAsync(string parameters, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(parameters) && parameters != "all" && parameters != "live")
            {
                this.Logger?.LogCommandOutput("{err}Invalid parameter.");
                return;
            }

            var planetDetails = (await SaliensApi.GetPlanetsAsync()).Values.OrderBy(p => p.State.Priority);

            var active = planetDetails.Where(p => p.State.Running);
            var future = planetDetails.Where(p => !p.State.Active && !p.State.Captured);
            if (parameters == "all")
            {
                var captured = planetDetails.Where(p => p.State.Captured);
                this.Logger?.LogCommandOutput("Captured planets:");
                await PrintPlanets(captured);
                this.Logger?.LogCommandOutput("");
                this.Logger?.LogCommandOutput("Upcoming planets:");
                await PrintPlanets(future);
                this.Logger?.LogCommandOutput("");
            }
            this.Logger?.LogCommandOutput("Active planets:");
            await PrintPlanets(active);
            this.Logger?.LogCommandOutput("");
            if (string.IsNullOrWhiteSpace(parameters))
            {
                this.Logger?.LogCommandOutput("Upcoming planets:");
                await PrintPlanets(future.Take(2));
                this.Logger?.LogCommandOutput("");
            }

            this.Logger?.LogCommandOutput($"To get a list of all planets, use the command: {{command}}planets {{param}}all{{reset}}{Environment.NewLine}{Environment.NewLine}" +

                $"To see more information about a planet, use the command: {{command}}planet {{param}}<id>{{reset}}{Environment.NewLine}" +
                $"where {{param}}<id>{{reset}} is replaced with the planet id.");

            async Task PrintPlanets(IEnumerable<Planet> planets)
            {
                var results = await Task.WhenAll(planets.Select(p => p.Zones == null ? SaliensApi.GetPlanetAsync(p.Id) : Task.FromResult(p)));
                foreach (var planet in results)
                    this.Logger?.LogCommandOutput(planet.ToConsoleLine());
            }
        }
    }
}
