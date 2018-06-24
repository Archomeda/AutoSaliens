using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1998

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("planets")]
    internal class PlanetsCommand : CommandBase
    {
        public override async Task<string> Run(string parameters, CancellationToken cancellationToken)
        {
            if (Program.Saliens.PlanetDetails == null)
                return "No planet information available yet.";

            var active = Program.Saliens.PlanetDetails.Where(p => p.State.Running);
            var captured = Program.Saliens.PlanetDetails.Where(p => p.State.Captured);
            var future = Program.Saliens.PlanetDetails.Where(p => !p.State.Active && !p.State.Captured);

            return $@"Captured:
{string.Join("\n", captured.Select(p => $"{p.Id} - {p.State.Name}"))}

Future:
{string.Join("\n", future.Select(p => $"{p.Id} - {p.State.Name}"))}

Active:
{string.Join("\n", active.Select(p => $"{p.Id} - {p.State.Name}"))}

To see more information about a planet, use the command: planet <id>
where <id> is replaced with the planet id";
        }
    }
}
