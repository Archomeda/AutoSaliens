using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("presence")]
    internal class DiscordPresenceCommand : CommandBase
    {
        public override async Task RunAsync(string parameters, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(parameters))
            {
                // Show the current Discord presence setting
                this.Logger?.LogCommandOutput($"The Discord presence is currently {{value}}{(Program.Settings.EnableDiscordPresence ? "enabled" : "disabled")}{{reset}}.");

                this.Logger?.LogCommandOutput("You can change the Discord presence by appending either enable or disable to this command: {command}presence {param}<toggle>");
                this.Logger?.LogCommandOutput("where {param}<toggle>{reset} is replaced with either {value}enable{reset} or {value}disable{reset}.");
            }
            else
            {
                // Set the Discord presence
                if (parameters == "enable")
                    Program.Settings.EnableDiscordPresence.Value = true;
                else if (parameters == "disable")
                    Program.Settings.EnableDiscordPresence.Value = false;
                else
                {
                    this.Logger?.LogCommandOutput("{err}Invalid input.");
                    return;
                }

                this.Logger?.LogCommandOutput($"Discord presence has been {(Program.Settings.EnableDiscordPresence ? "enabled" : "disabled")}.");
            }
        }
    }
}
