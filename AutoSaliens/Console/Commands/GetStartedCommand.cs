using System;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1998

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("getstarted")]
    internal class GetStartedCommand : CommandBase
    {
        public override async Task<string> Run(string parameters, CancellationToken cancellationToken)
        {
            return $"Follow these steps in order to get started:{Environment.NewLine}{Environment.NewLine}" +

                $"1. Set your Saliens token (for more information, run the command {{command}}\"gettoken\"{{/command}}){Environment.NewLine}" +
                $"2. Start automating by running the command {{command}}\"resume\"{{/command}}{Environment.NewLine}" +
                $"3. See your level rise once the planet and zone have been chosen";
        }
    }
}
