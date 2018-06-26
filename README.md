# AutoSaliens
[![](https://ci.appveyor.com/api/projects/status/78eoqoe0cn4muu9g/branch/master?svg=true&passingText=master%20-%20OK&failingText=master%20-%20failure&pendingText=master%20-%20pending)](https://ci.appveyor.com/project/Archomeda/AutoSaliens/branch/master)
[![](https://ci.appveyor.com/api/projects/status/78eoqoe0cn4muu9g/branch/stable?svg=true&passingText=stable%20-%20OK&failingText=stable%20-%20failure&pendingText=stable%20-%20pending)](https://ci.appveyor.com/project/Archomeda/AutoSaliens/branch/stable)
*Automating the Steam Summer Sale 2018 game with overkill.*

**Note:** This is a hobby project, so expect bugs. Want to have them fixed? Please report an issue or submit a pull request.

## Why another automation tool?
This tool is simply way too overkill for such a small mini game. However, I thought it would be a nice experience for me to get back into C# again. So here it is, another one.

### Features
This application automates the mini game, just like most other tools do. But let's list all the features:
- Interactive console with various commands that can show the game status or change the settings
- Can automate the mini game and supports updating Discord Rich Presence
- Automation:
  - Automatically joins planets and zones based on a configurable strategy
  - Supports overriding the joining planet, in case you want to spend as much time as possible there
  - Configurable game time, which is by default 110 seconds (keep in mind that setting this too low might cause failures)
  - Adjusts the game time slightly based on network latency
- Discord:
  - Supports Discord Rich Presence (automation is not required)

### Strategy
There's a couple of strategies that you can use. You can manipulate the strategy with the `strategy` command. Without parameters, this command will return the current used strategy. In order to set the strategy, use the names below *(it's case sensitive!)*. You can use multiple names, just be sure to separate them with a comma or a space.

Some strategies are incompatible with each other, in that case, the top-most one will take priority.
- FocusCurrentPlanet
- FocusRandomPlanet
- MostDifficultPlanetsFirst
- LeastDifficultPlanetsFirst
- MostCompletedPlanetsFirst
- LeastCompletedPlanetsFirst
- MostDifficultZonesFirst
- LeastDifficultZonesFirst
- MostCompletedZonesFirst
- LeastCompletedZonesFirst
- TopDown
- BottomUp

The default strategy is: MostDifficultPlanetsFirst, MostCompletedPlanetsFirst, MostDifficultZonesFirst, MostCompletedZonesFirst, TopDown.

## How to use
### Windows
1. [Download and install .NET Framework](https://www.microsoft.com/net/download/dotnet-framework-runtime) if you don't have it already (4.5.1 is required)
2. [Download the latest Windows build from AppVeyor](https://ci.appveyor.com/project/Archomeda/AutoSaliens/branch/stable/artifacts)
3. Extract the archive to a separate folder
4. Run AutoSaliens.exe
5. Follow instructions in the console

### Other operating systems
1. Download and install .NET Core Runtime if you don't have it already (2.0 is required): [Linux](https://docs.microsoft.com/en-us/dotnet/core/linux-prerequisites?tabs=netcore2x), [macOS](https://docs.microsoft.com/en-us/dotnet/core/macos-prerequisites?tabs=netcore2x)
2. [Download the latest Portable build from AppVeyor](https://ci.appveyor.com/project/Archomeda/AutoSaliens/branch/stable/artifacts)
3. Extract the archive to a separate folder
4. Run `dotnet AutoSaliens.dll`
5. Follow instructions in the console

## Commands
The list below might not be up-to-date. You can find the up-to-date command list of your version by executing the `help` command.

```
Salien commands:
joinedplanet                - Shows the current joined planet
joinedzone                  - Shows the current joined zone
overrideplanetid [id]       - Shows or sets the current overridden planet id;
                              Overridden planets will always be joined, unless it's fully captured
networktolerance [toggle]   - Shows or sets whether the network tolerance is enabled or not;
                              Enabling this will cause the application to try to send certain API
                              calls earlier to account for network latency (default: enable)
planet <id>                 - Shows information about a planet
planets [option]            - Shows the list of all planets
strategy [strategy]         - Shows or sets the current active strategy (to reset to default, use: 0)
zone <planet_id> <zone_pos> - Shows information about a zone
zones <planet_id>           - Shows the list of the zones of a planet

Informative commands:
getstarted - Shows information about how and where to start
gettoken   - Shows information about where you can get your Saliens token
help       - Shows this help message
homepage   - Shows the homepage URL of this application

Application commands:
checkupdate        - Checks if an update was available during the last check
exit               - Exits the program
gametime [seconds] - Shows or sets the game time in seconds (default: 110)
pause              - Pauses the automation after completing one loop cycle
presence [toggle]  - Shows or sets whether Discord presence is enabled or not
resume             - Starts/Resumes the automation
token [token]      - Shows or sets the token
```

## Compiling
This project uses Visual Studio 2017 and .NET Framework 4.5.1 or .NET Core 2.0. There are no specific build instructions.
