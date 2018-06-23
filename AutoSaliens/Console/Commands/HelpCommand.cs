using System.Threading;
using System.Threading.Tasks;

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
                              Overridden planets will always be joined, unless it's
                              fully captured
planet <id>                 - Shows information about a planet
planets                     - Shows the list of all planets
strategy [strategy]         - Shows or sets the current active strategy
zone <planet_id> <zone_pos> - Shows information about a zone
zones <planet_id>           - Shows the list of the zones of a planet

Informative commands:
getstarted - Shows information about how and where to start
gettoken   - Opens the browser where the token can be retrieved
             and shows information about how to set the token
help       - Shows this help message
homepage   - Opens the homepage of this application in your browser

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
