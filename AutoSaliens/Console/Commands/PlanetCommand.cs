using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("planet")]
    internal class PlanetCommand : CommandBase
    {
        public override async Task<string> Run(string parameters, CancellationToken cancellationToken)
        {
            if (Program.Saliens.PlanetDetails == null)
                return "No planet information available yet.";

            var planet = Program.Saliens.PlanetDetails.FirstOrDefault(p => p.Id == parameters);
            if (planet == null)
                return "Unknown planet id.";

            if (planet.Zones == null)
            {
                planet = await SaliensApi.GetPlanet(parameters);
                var index = Program.Saliens.PlanetDetails.FindIndex(p => p.Id == parameters);
                Program.Saliens.PlanetDetails[index] = planet;
            }

            return planet.ToString();
        }
    }
}
