using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("gametime")]
    internal class GameTimeCommand : CommandBase
    {
        public override async Task RunAsync(string parameters, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(parameters))
            {
                // Show the current game time
                this.Logger?.LogCommandOutput($"The game time is currently set to: {{value}}{Program.Settings.GameTime} seconds{{reset}}.");

                this.Logger?.LogCommandOutput("You can change the game time by appending the game time to this command: {command}gametime {param}<seconds>");
                this.Logger?.LogCommandOutput("where {param}<seconds>{reset} is replaced with the amount of seconds.");
            }
            else
            {
                // Set the game time
                if (!int.TryParse(parameters, out int time))
                {
                    this.Logger?.LogCommandOutput("Invalid input, requires an integer.");
                    return;
                }
                if (time < 1)
                {
                    this.Logger?.LogCommandOutput("Integer needs to be greater than 0.");
                    return;
                }

                Program.Settings.GameTime.Value = time;
                this.Logger?.LogCommandOutput("Your game time has been saved.");
            }
        }
    }
}
