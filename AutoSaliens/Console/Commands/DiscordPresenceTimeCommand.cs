using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoSaliens.Presence;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("presencetime")]
    internal class DiscordPresenceTimeCommand : CommandBase
    {
        public override async Task<string> Run(string parameters, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(parameters))
            {
                // Show the current Discord presence time type
                var allValues = Enum.GetValues(typeof(PresenceFormatterType)) as PresenceFormatterType[];
                this.WriteConsole($"The Discord presence time type is currently {{value}}{(Program.Settings.DiscordPresenceTimeType.ToString())}{{reset}}.");

                this.WriteConsole("You can change the Discord presence time type by appending an option this command: {command}presencetime {param}<option>");
                this.WriteConsole("where {param}<option>{reset} is replaced with one of the following options:");
                this.WriteConsole(string.Join(Environment.NewLine, allValues.Select(v => $"  {{value}}{v.ToString()}{{reset}}")));

                return "";
            }
            else
            {
                // Set the Discord presence time type
                try
                {
                    var presenceType = (PresenceFormatterType)Enum.Parse(typeof(PresenceFormatterType), parameters);
                    if (presenceType == 0)
                        presenceType = PresenceFormatterType.TimeZoneElapsed;
                    Program.Settings.DiscordPresenceTimeType.Value = presenceType;
                    return "Your Discord presence time type has been saved.";
                }
                catch (ArgumentException)
                {
                    return "{err}Invalid input.";
                }
            }
        }
    }
}
