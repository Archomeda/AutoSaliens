using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1998

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("joinedzone")]
    internal class JoinedZoneCommand : CommandBase
    {
        public override async Task<string> Run(string parameters, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(Program.Saliens.Token))
                return "{{warn}}No token has been set.";

            if (Program.Saliens.PlanetDetails == null)
                return "No planet information available yet.";

            var zone = Program.Saliens.JoinedZone;
            if (zone == null)
                return "No zone has been joined.";

            return zone.ToConsoleBlock();
        }
    }
}
