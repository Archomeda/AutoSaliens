using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1998

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
                this.WriteConsole($"The game time is currently set to: {Program.Saliens.GameTime} seconds");

                this.WriteConsole("You can change the game time by appending the game time to this command: gametime <seconds>");
                this.WriteConsole("where <seconds> is replaced with the amount of seconds.");

                return "";
            }
            else
            {
                // Set the game time
                if (!int.TryParse(parameters, out int time))
                    return "Invalid input, requires an integer.";
                if (time < 1)
                    return "Integer needs to be greater than 0.";

                Program.Saliens.GameTime = time;
                Program.Settings.GameTime = time;
                Program.Settings.Save();
                return "Your game time has been saved.";
            }
        }
    }
}
