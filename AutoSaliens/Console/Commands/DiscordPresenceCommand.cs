using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("presence")]
    internal class DiscordPresenceCommand : CommandBase
    {
        public override async Task<string> Run(string parameters, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(parameters))
            {
                // Show the current Discord presence setting
                this.WriteConsole($"The Discord presence is currently {{value}}{(Program.Settings.EnableDiscordPresence ? "enabled" : "disabled")}{{reset}}.");

                this.WriteConsole("You can change the Discord presence by appending either enable or disable to this command: {command}presence {param}<toggle>");
                this.WriteConsole("where {param}<toggle>{reset} is replaced with either {value}enable{reset} or {value}disable{reset}.");

                return "";
            }
            else
            {
                // Set the Discord presence
                if (parameters == "enable")
                    Program.Settings.EnableDiscordPresence.Value = true;
                else if (parameters == "disable")
                    Program.Settings.EnableDiscordPresence.Value = false;
                else
                    return "{err}Invalid input.";

                return $"Discord presence has been {(Program.Settings.EnableDiscordPresence ? "enabled" : "disabled")}.";
            }
        }
    }
}
