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
            return @"Welcome! This application supports two modes: Automation and Discord Presence.

To enable automation:
1. Set your Saliens token (for more information, run the command: {command}gettoken{reset})
2. Start automating by running the command: {command}resume{reset}
3. See your level rise once the planet and zone have been chosen

To enable Discord presence:
1. Set your Saliens token (for more information, run the command: {command}gettoken{reset})
2. Run the command: {command}presence{reset} {param}enable{reset}
3. Discord presence should now be working

You can use both modes. Just follow all the steps and you're set!";
        }
    }
}
