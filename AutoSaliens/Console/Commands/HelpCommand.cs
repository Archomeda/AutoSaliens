using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1998

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("help")]
    internal class HelpCommand : CommandBase
    {
        public override async Task<string> Run(string parameters, CancellationToken cancellationToken)
        {
            return @"The following commands are available:

Salien commands:
joinedplanet                - Shows the current joined planet
joinedzone                  - Shows the current joined zone
overrideplanetid [id]       - Shows or sets the current overridden planet id
                              Overridden planets will always be joined, unless it's fully captured
planet <id>                 - Shows information about a planet
planets                     - Shows the list of all planets
strategy [strategy]         - Shows or sets the current active strategy
zone <planet_id> <zone_pos> - Shows information about a zone
zones <planet_id>           - Shows the list of the zones of a planet

Informative commands:
getstarted - Shows information about how and where to start
gettoken   - Shows information about where you can get your Saliens token
help       - Shows this help message
homepage   - Shows the homepage URL of this application

Application commands:
debug              - Toggles exception handling by the debugger
exit               - Exits the program
gametime [seconds] - Shows or sets the game time in seconds
pause              - Pauses the automation after completing one loop cycle
resume             - Resumes the automation
token [token]      - Shows or sets the token";
        }
    }
}
