using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("bossdamagemin")]
    internal class BossDamageDealtMinCommand : CommandBase
    {
        public override async Task RunAsync(string parameters, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(parameters))
            {
                // Show the current minimum damage dealt to bosses
                this.Logger?.LogCommandOutput($"The minimum damage dealt to bosses is currently set to: {{value}}{Program.Settings.BossDamageDealtMin}{{reset}}.");

                this.Logger?.LogCommandOutput("You can change the damage by appending the it to this command: {command}bossdamagemin {param}<damage>");
                this.Logger?.LogCommandOutput("where {param}<damage>{reset} is replaced with the damage.");
            }
            else
            {
                // Set the minimum damage dealt to bosses
                if (!int.TryParse(parameters, out int damage))
                {
                    this.Logger?.LogCommandOutput("Invalid input, requires an integer.");
                    return;
                }
                if (damage < 1)
                {
                    this.Logger?.LogCommandOutput("Integer needs to be greater than 0.");
                    return;
                }
                if (damage > Program.Settings.BossDamageDealtMax)
                {
                    this.Logger?.LogCommandOutput("The damage needs to be smaller than your current maximum damage.");
                    return;
                }

                Program.Settings.BossDamageDealtMin.Value = damage;
                this.Logger?.LogCommandOutput("Your minimum damage dealt to bosses has been saved.");
            }
        }
    }
}
