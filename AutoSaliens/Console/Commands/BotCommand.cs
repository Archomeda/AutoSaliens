using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("bot")]
    internal class BotCommand : CommandBase
    {
        public override async Task RunAsync(string parameters, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(parameters))
            {
                // Show the current Discord presence setting
                this.Logger?.LogCommandOutput($"The bot is currently {{value}}{(Program.Settings.EnableBot ? "enabled" : "disabled")}{{reset}}.");

                this.Logger?.LogCommandOutput("You can change the bot by appending either enable or disable to this command: {command}bot {param}<toggle>");
                this.Logger?.LogCommandOutput("where {param}<toggle>{reset} is replaced with either {value}enable{reset} or {value}disable{reset}.");
            }
            else
            {
                // Set the Discord presence
                if (parameters == "enable")
                    Program.Settings.EnableBot.Value = true;
                else if (parameters == "disable")
                    Program.Settings.EnableBot.Value = false;
                else
                {
                    this.Logger?.LogCommandOutput("{err}Invalid input.");
                    return;
                }

                this.Logger?.LogCommandOutput($"Bot has been {(Program.Settings.EnableBot ? "enabled" : "disabled")}.");
            }
        }
    }
}
