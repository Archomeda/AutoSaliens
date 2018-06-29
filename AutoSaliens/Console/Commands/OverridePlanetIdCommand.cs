using System.Threading;
using System.Threading.Tasks;
using AutoSaliens.Api;

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
                if (!string.IsNullOrWhiteSpace(Program.Settings.OverridePlanetId))
                    this.Logger?.LogCommandOutput($"The planet id is currently overridden to: {{value}}{Program.Settings.OverridePlanetId}");
                else
                    this.Logger?.LogCommandOutput("You have currently no planet id override set.");

                this.Logger?.LogCommandOutput("You can override the planet id by appending the planet id to this command: {command}overrideplanetid {param}<id>");
                this.Logger?.LogCommandOutput("where {param}<id> is replaced with the planet id, or {param}remove{reset} if you wish to remove it.");
            }
            else
            {
                // Set the overridden planet id
                if (parameters == "remove")
                {
                    Program.Settings.OverridePlanetId.Value = null;
                    this.Logger?.LogCommandOutput("Your planet id override has been removed.");
                }
                else
                {
                    var planet = await SaliensApi.GetPlanetAsync(parameters);
                    if (planet == null)
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
}
