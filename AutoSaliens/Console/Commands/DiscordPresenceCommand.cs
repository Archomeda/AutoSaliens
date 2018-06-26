using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1998

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
                {
                    Program.Settings.EnableDiscordPresence = true;
                    if (!Program.Presence.Started)
                        await Program.Presence.Start();
                }
                else if (parameters == "disable")
                {
                    Program.Settings.EnableDiscordPresence = false;
                    if (Program.Presence.Started)
                        Program.Presence.Stop();
                }
                else
                    return "{err}Invalid input.";
                Program.Settings.Save();

                // Activate checking periodically when automation isn't active
                if (Program.Settings.EnableDiscordPresence)
                    Program.Presence.CheckPeriodically = !Program.Saliens.AutomationActive;

                return $"Discord presence has been {(Program.Settings.EnableDiscordPresence ? "enabled" : "disabled")}.";
            }
        }
    }
}
