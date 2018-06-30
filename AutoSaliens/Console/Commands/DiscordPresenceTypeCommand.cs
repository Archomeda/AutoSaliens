using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoSaliens.Presence;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("presencetype")]
    internal class DiscordPresenceTypeCommand : CommandBase
    {
        public override async Task RunAsync(string parameters, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(parameters))
            {
                // Show the current Discord presence type
                var allValues = Enum.GetValues(typeof(PresenceFormatterType)) as PresenceFormatterType[];
                this.Logger?.LogCommandOutput($"The Discord presence type is currently {{value}}{(Program.Settings.DiscordPresenceTimeType.ToString())}{{reset}}.");

                this.Logger?.LogCommandOutput("You can change the Discord presence type by appending an option this command: {command}presencetype {param}<option>");
                this.Logger?.LogCommandOutput("where {param}<option>{reset} is replaced with one of the following options:");
                this.Logger?.LogCommandOutput(string.Join(Environment.NewLine, allValues.Select(v => $"  {{value}}{v.ToString()}{{reset}}")));
            }
            else
            {
                // Set the Discord presence type
                try
                {
                    var presenceType = (PresenceFormatterType)Enum.Parse(typeof(PresenceFormatterType), parameters);
                    if (presenceType == 0)
                        presenceType = PresenceFormatterType.TimeZoneElapsed;
                    Program.Settings.DiscordPresenceTimeType.Value = presenceType;
                    this.Logger?.LogCommandOutput("Your Discord presence type has been saved.");
                }
                catch (ArgumentException)
                {
                    this.Logger?.LogCommandOutput("{err}Invalid input.");
                }
            }
        }
    }
}
