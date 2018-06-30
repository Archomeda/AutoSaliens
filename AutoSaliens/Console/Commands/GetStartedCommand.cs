using System;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("getstarted")]
    internal class GetStartedCommand : CommandBase
    {
        public override async Task RunAsync(string parameters, CancellationToken cancellationToken)
        {
            this.Logger?.LogCommandOutput($"Welcome! This application supports two modes: Bot and Discord presence.{Environment.NewLine}" +
                $"You can use both modes, but if desired they can also work independently.{Environment.NewLine}{Environment.NewLine}" +

                $"To enable the bot:{Environment.NewLine}" +
                $"1. Set your Saliens token (for more information, run the command: {{command}}gettoken{{reset}}){Environment.NewLine}" +
                $"2. Start automating by running the command: {{command}}bot {{param}}enable{{reset}}{Environment.NewLine}" +
                $"3. See your level rise once the planet and zone have been chosen{Environment.NewLine}{Environment.NewLine}" +

                $"To enable Discord presence:{Environment.NewLine}" +
                $"1. Set your Saliens token (for more information, run the command: {{command}}gettoken{{reset}}){Environment.NewLine}" +
                $"2. Run the command: {{command}}presence {{param}}enable{{reset}}{Environment.NewLine}" +
                $"3. Discord presence should now be working{Environment.NewLine}{Environment.NewLine}");
        }
    }
}
