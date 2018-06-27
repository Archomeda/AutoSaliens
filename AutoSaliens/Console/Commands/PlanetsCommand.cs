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
        public override async Task<string> Run(string parameters, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(parameters) && parameters != "all" && parameters != "live")
                return "{err}Invalid parameter.";

            if (Program.Saliens.PlanetDetails == null)
                return "No planet information available yet.";

            var planetDetails = Program.Saliens.PlanetDetails.OrderBy(p => p.State.Priority);

            var active = planetDetails.Where(p => p.State.Running);
            var future = planetDetails.Where(p => !p.State.Active && !p.State.Captured);
            if (parameters == "all" || parameters == "live")
            {
                var captured = planetDetails.Where(p => p.State.Captured);
                this.WriteConsole("Captured planets:");
                await PrintPlanets(captured);
                this.WriteConsole("");
                this.WriteConsole("Upcoming planets:");
                await PrintPlanets(future);
                this.WriteConsole("");
            }
            this.WriteConsole("Active planets:");
            await PrintPlanets(active);
            this.WriteConsole("");
            if (string.IsNullOrWhiteSpace(parameters))
            {
                this.WriteConsole("Upcoming planets:");
                await PrintPlanets(future.Take(1));
                this.WriteConsole("");
            }

            return $"To get a list of all planets, use the command: {{command}}planets {{param}}all{{reset}}{Environment.NewLine}" +
                $"To fully refresh the list of planets, use the command: {{command}}planets {{param}}live{{reset}}{Environment.NewLine}{Environment.NewLine}" +

                $"To see more information about a planet, use the command: {{command}}planet {{param}}<id>{{reset}}{Environment.NewLine}" +
                $"where {{param}}<id>{{reset}} is replaced with the planet id.";

            async Task PrintPlanets(IEnumerable<Planet> planets)
            {
                var tasks = planets.Select(p => parameters == "live" || p.Zones == null ? SaliensApi.GetPlanetAsync(p.Id) : Task.FromResult(p));
                foreach (var task in tasks)
                {
                    var planet = await task;
                    var i = Program.Saliens.PlanetDetails.FindIndex(p => p.Id == planet.Id);
                    Program.Saliens.PlanetDetails[i] = planet;
                    this.WriteConsole(planet.ToConsoleLine());
                }
            }
        }
    }
}
