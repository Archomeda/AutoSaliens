using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("joinedzone")]
    internal class JoinedZoneCommand : CommandBase
    {
        public override async Task RunAsync(string parameters, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(Program.Saliens.Token))
            {
                this.Logger?.LogCommandOutput("{{warn}}No token has been set.");
                return;
            }

            if (Program.Saliens.PlanetDetails == null)
            {
                this.Logger?.LogCommandOutput("No planet information available yet.");
                return;
            }

            var zone = Program.Saliens.JoinedZone;
            if (zone == null)
            {
                this.Logger?.LogCommandOutput("No zone has been joined.");
                return;
            }

            this.Logger?.LogCommandOutput(zone.ToConsoleBlock());
        }
    }
}
