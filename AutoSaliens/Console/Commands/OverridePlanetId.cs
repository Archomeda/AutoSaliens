using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("overrideplanetid")]
    internal class OverridePlanetIdCommand : CommandBase
    {
        public override async Task<string> Run(string parameters, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(parameters))
            {
                // Show the current overridden planet id
                if (!string.IsNullOrWhiteSpace(Program.Saliens.OverridePlanetId))
                    this.WriteConsole($"The planet id is currently overridden to: {Program.Saliens.OverridePlanetId}");
                else
                    this.WriteConsole("You have currently no planet id override set.");

                this.WriteConsole("You can override the planet id by appending the planet id to this command: overrideplanetid <id>");
                this.WriteConsole("where <id> is replaced with the planet id.");

                return "";
            }
            else
            {
                // Set the overridden planet id
                if (Program.Saliens.PlanetDetails.FirstOrDefault(p => p.Id == parameters) == null)
                    return "Invalid planet id. Check the planets for ids.";
                Program.Saliens.OverridePlanetId = parameters;
                Program.Settings.OverridePlanetId = parameters;
                Program.Settings.Save();
                return "Your planet id override has been saved.";
            }
        }
    }
}
