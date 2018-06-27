using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("gametime")]
    internal class GameTimeCommand : CommandBase
    {
        public override async Task<string> Run(string parameters, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(parameters))
            {
                // Show the current game time
                this.WriteConsole($"The game time is currently set to: {{value}}{Program.Saliens.GameTime} seconds{{reset}}.");

                this.WriteConsole("You can change the game time by appending the game time to this command: {command}gametime {param}<seconds>");
                this.WriteConsole("where {param}<seconds>{reset} is replaced with the amount of seconds.");

                return "";
            }
            else
            {
                // Set the game time
                if (!int.TryParse(parameters, out int time))
                    return "Invalid input, requires an integer.";
                if (time < 1)
                    return "Integer needs to be greater than 0.";

                Program.Settings.GameTime.Value = time;
                return "Your game time has been saved.";
            }
        }
    }
}
