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
                return "{{warn}}No token has been set.";

            if (Program.Saliens.PlanetDetails == null)
                return "No planet information available yet.";

            var planet = Program.Saliens.JoinedPlanet;
            if (planet == null)
                return "No planet has been joined.";

            if (planet.Zones == null)
            {
                planet = await SaliensApi.GetPlanetAsync(parameters);
                var index = Program.Saliens.PlanetDetails.FindIndex(p => p.Id == parameters);
                Program.Saliens.PlanetDetails[index] = planet;
            }

            return planet.ToConsoleBlock();
        }
    }
}
