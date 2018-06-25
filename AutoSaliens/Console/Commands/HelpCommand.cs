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
{command}joinedplanet{reset}                - Shows the current joined planet
{command}joinedzone{reset}                  - Shows the current joined zone
{command}overrideplanetid {param}[id]{reset}       - Shows or sets the current overridden planet id;
                              Overridden planets will always be joined, unless it's fully captured
{command}networktolerance {param}[toggle]{reset}   - Shows or sets whether the network tolerance is enabled or not;
                              Enabling this will cause the application to try to send certain API
                              calls earlier to account for network latency (default: enable)
{command}planet {param}<id>{reset}                 - Shows information about a planet
{command}planets {param}[option]{reset}            - Shows the list of all planets
{command}strategy {param}[strategy]{reset}         - Shows or sets the current active strategy (to reset to default, use: 0)
{command}zone {param}<planet_id> <zone_pos>{reset} - Shows information about a zone
{command}zones {param}<planet_id>{reset}           - Shows the list of the zones of a planet

Informative commands:
{command}getstarted{reset} - Shows information about how and where to start
{command}gettoken{reset}   - Shows information about where you can get your Saliens token
{command}help{reset}       - Shows this help message
{command}homepage{reset}   - Shows the homepage URL of this application

Application commands:
{command}exit{reset}               - Exits the program
{command}gametime {param}[seconds]{reset} - Shows or sets the game time in seconds (default: 110)
{command}pause{reset}              - Pauses the automation after completing one loop cycle
{command}presence {param}[toggle]{reset}  - Shows or sets whether Discord presence is enabled or not
{command}resume{reset}             - Resumes the automation
{command}token {param}[token]{reset}      - Shows or sets the token";
        }
    }
}
