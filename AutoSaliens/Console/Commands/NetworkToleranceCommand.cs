using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1998

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("networktolerance")]
    internal class NetworkToleranceCommand : CommandBase
    {
        public override async Task<string> Run(string parameters, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(parameters))
            {
                // Show the current network tolerance
                this.WriteConsole($"The network tolerance is currently {{value}}{(Program.Saliens.EnableNetworkTolerance ? "enabled" : "disabled")}{{reset}}.");

                this.WriteConsole("You can change the network tolerance by appending either enable or disable to this command: {command}networktolerance {param}<toggle>");
                this.WriteConsole("where {param}<toggle>{reset} is replaced with either {value}enable{reset} or {value}disable{reset}.");

                return "";
            }
            else
            {
                // Set the game time
                if (parameters == "enable")
                    Program.Settings.EnableNetworkTolerance = true;
                else if (parameters == "disable")
                    Program.Settings.EnableNetworkTolerance = false;
                else
                    return "{err}Invalid input.";
                Program.Settings.Save();
                return $"Network tolerance has been {(Program.Settings.EnableNetworkTolerance ? "enabled" : "disabled")}.";
            }
        }
    }
}
