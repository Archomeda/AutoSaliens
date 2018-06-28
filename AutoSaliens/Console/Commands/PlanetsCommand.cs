using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoSaliens.Api.Models;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

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

            if (Program.Saliens.PlanetDetails == null)
            {
                this.Logger?.LogCommandOutput("No planet information available yet.");
                return;
            }

            var planetDetails = Program.Saliens.PlanetDetails.OrderBy(p => p.State.Priority);

            var active = planetDetails.Where(p => p.State.Running);
            var future = planetDetails.Where(p => !p.State.Active && !p.State.Captured);
            if (parameters == "all" || parameters == "live")
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
                await PrintPlanets(future.Take(1));
                this.Logger?.LogCommandOutput("");
            }

            this.Logger?.LogCommandOutput($"To get a list of all planets, use the command: {{command}}planets {{param}}all{{reset}}{Environment.NewLine}" +
                $"To fully refresh the list of planets, use the command: {{command}}planets {{param}}live{{reset}}{Environment.NewLine}{Environment.NewLine}" +

                $"To see more information about a planet, use the command: {{command}}planet {{param}}<id>{{reset}}{Environment.NewLine}" +
                $"where {{param}}<id>{{reset}} is replaced with the planet id.");

            async Task PrintPlanets(IEnumerable<Planet> planets)
            {
                var tasks = planets.Select(p => parameters == "live" || p.Zones == null ? SaliensApi.GetPlanetAsync(p.Id) : Task.FromResult(p));
                foreach (var task in tasks)
                {
                    var planet = await task;
                    var i = Program.Saliens.PlanetDetails.FindIndex(p => p.Id == planet.Id);
                    Program.Saliens.PlanetDetails[i] = planet;
                    this.Logger?.LogCommandOutput(planet.ToConsoleLine());
                }
            }
        }
    }
}
