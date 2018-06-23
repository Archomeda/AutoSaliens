using System.Threading;
using System.Threading.Tasks;

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("getstarted")]
    internal class GetStartedCommand : CommandBase
    {
        public override async Task<string> Run(string parameters, CancellationToken cancellationToken)
        {
            return @"Follow these steps in order to get started:

1. Set your Saliens token (for more information, run the command: gettoken)
2. Start the automation by running the command: resume
3. See your level rise once the planet and zone have been chosen";
        }
    }
}
