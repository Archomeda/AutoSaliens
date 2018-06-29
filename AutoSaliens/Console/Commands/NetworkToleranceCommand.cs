using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("networktolerance")]
    internal class NetworkToleranceCommand : CommandBase
    {
        public override async Task RunAsync(string parameters, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(parameters))
            {
                // Show the current network tolerance
                this.Logger?.LogCommandOutput($"The network tolerance is currently {{value}}{(Program.Settings.EnableNetworkTolerance ? "enabled" : "disabled")}{{reset}}.");

                this.Logger?.LogCommandOutput("You can change the network tolerance by appending either enable or disable to this command: {command}networktolerance {param}<toggle>");
                this.Logger?.LogCommandOutput("where {param}<toggle>{reset} is replaced with either {value}enable{reset} or {value}disable{reset}.");
            }
            else
            {
                // Set the game time
                if (parameters == "enable")
                    Program.Settings.EnableNetworkTolerance.Value = true;
                else if (parameters == "disable")
                    Program.Settings.EnableNetworkTolerance.Value = false;
                else
                {
                    this.Logger?.LogCommandOutput("{err}Invalid input.");
                    return;
                }
                this.Logger?.LogCommandOutput($"Network tolerance has been {(Program.Settings.EnableNetworkTolerance ? "enabled" : "disabled")}.");
            }
        }
    }
}
