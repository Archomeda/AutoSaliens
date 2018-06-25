using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1998

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
                    this.WriteConsole($"The planet id is currently overridden to: {{value}}{Program.Saliens.OverridePlanetId}");
                else
                    this.WriteConsole("You have currently no planet id override set.");

                this.WriteConsole("You can override the planet id by appending the planet id to this command: {command}overrideplanetid {param}<id>");
                this.WriteConsole("where {param}<id> is replaced with the planet id.");

                return "";
            }
            else
            {
                // Set the overridden planet id
                if (Program.Saliens.PlanetDetails.FirstOrDefault(p => p.Id == parameters) == null)
                    return "{err}Invalid planet id. Check the planets for ids.";
                Program.Settings.OverridePlanetId = parameters;
                Program.Settings.Save();
                return "Your planet id override has been saved.";
            }
        }
    }
}
