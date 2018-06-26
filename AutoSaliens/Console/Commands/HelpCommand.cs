using System;
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
            return $"The following commands are available:{Environment.NewLine}{Environment.NewLine}" +

                $"Salien commands:{Environment.NewLine}" +
                $"{{command}}joinedplanet{{reset}}                - Shows the current joined planet{Environment.NewLine}" +
                $"{{command}}joinedzone{{reset}}                  - Shows the current joined zone{Environment.NewLine}" +
                $"{{command}}overrideplanetid {{param}}[id]{{reset}}       - Shows or sets the current overridden planet id;{Environment.NewLine}" +
                $"                              Overridden planets will always be joined, unless it's fully captured{Environment.NewLine}" +
                $"{{command}}networktolerance {{param}}[toggle]{{reset}}   - Shows or sets whether the network tolerance is enabled or not;{Environment.NewLine}" +
                $"                              Enabling this will cause the application to try to send certain API{Environment.NewLine}" +
                $"                              calls earlier to account for network latency (default: enable){Environment.NewLine}" +
                $"{{command}}planet {{param}}<id>{{reset}}                 - Shows information about a planet{Environment.NewLine}" +
                $"{{command}}planets {{param}}[option]{{reset}}            - Shows the list of all planets{Environment.NewLine}" +
                $"{{command}}strategy {{param}}[strategy]{{reset}}         - Shows or sets the current active strategy (to reset to default, use: 0){Environment.NewLine}" +
                $"{{command}}zone {{param}}<planet_id> <zone_pos>{{reset}} - Shows information about a zone{Environment.NewLine}" +
                $"{{command}}zones {{param}}<planet_id>{{reset}}           - Shows the list of the zones of a planet{Environment.NewLine}{Environment.NewLine}" +

                $"Informative commands:{Environment.NewLine}" +
                $"{{command}}getstarted{{reset}} - Shows information about how and where to start{Environment.NewLine}" +
                $"{{command}}gettoken{{reset}}   - Shows information about where you can get your Saliens token{Environment.NewLine}" +
                $"{{command}}help{{reset}}       - Shows this help message{Environment.NewLine}" +
                $"{{command}}homepage{{reset}}   - Shows the homepage URL of this application{Environment.NewLine}{Environment.NewLine}" +

                $"Application commands:{Environment.NewLine}" +
                $"{{command}}checkupdate{{reset}}        - Checks if an update was available during the last check{Environment.NewLine}" +
                $"{{command}}exit{{reset}}               - Exits the program{Environment.NewLine}" +
                $"{{command}}gametime {{param}}[seconds]{{reset}} - Shows or sets the game time in seconds (default: 110){Environment.NewLine}" +
                $"{{command}}pause{{reset}}              - Pauses the automation after completing one loop cycle{Environment.NewLine}" +
                $"{{command}}presence {{param}}[toggle]{{reset}}  - Shows or sets whether Discord presence is enabled or not{Environment.NewLine}" +
                $"{{command}}token {{param}}[token]{{reset}}      - Shows or sets the token";
        }
    }
}
