using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("overrideplanetid")]
    internal class OverridePlanetIdCommand : CommandBase
    {
        public override async Task RunAsync(string parameters, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(parameters))
            {
                // Show the current overridden planet id
                if (!string.IsNullOrWhiteSpace(Program.Saliens.OverridePlanetId))
                    this.Logger?.LogCommandOutput($"The planet id is currently overridden to: {{value}}{Program.Saliens.OverridePlanetId}");
                else
                    this.Logger?.LogCommandOutput("You have currently no planet id override set.");

                this.Logger?.LogCommandOutput("You can override the planet id by appending the planet id to this command: {command}overrideplanetid {param}<id>");
                this.Logger?.LogCommandOutput("where {param}<id> is replaced with the planet id.");
            }
            else
            {
                // Set the overridden planet id
                if (Program.Saliens.PlanetDetails.FirstOrDefault(p => p.Id == parameters) == null)
                    this.Logger?.LogCommandOutput("{err}Invalid planet id. Check the planets for ids.");
                else
                {
                    Program.Settings.OverridePlanetId.Value = parameters;
                    this.Logger?.LogCommandOutput("Your planet id override has been saved.");
                }
            }
        }
    }
}
