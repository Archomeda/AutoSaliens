using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("joinedplanet")]
    internal class JoinedPlanetCommand : CommandBase
    {
        public override async Task<string> Run(string parameters, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(Program.Saliens.Token))
                return "No token has been set.";

            if (Program.Saliens.PlanetDetails == null)
                return "No planet information available yet.";

            try
            {
                await Program.Saliens.UpdatePlayerInfo(cancellationToken);
            }
            catch (WebException ex)
            {
                return $"Invalid response. {ex.Message}";
            }

            var planet = Program.Saliens.JoinedPlanet;
            if (planet == null)
                return "No planet has been joined.";

            return planet.ToString();
        }
    }
}
