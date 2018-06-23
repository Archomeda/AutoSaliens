# AutoSaliens
*Automating the Steam Summer Sale 2018 game with overkill.*

**Note:** This is a hobby project, so expect bugs. Want to have them fixed? Report it as an issue or submit a pull request.


## Why another automation tool?
This tool is simply way too overkill for such a small mini game. However, I thought it would be a nice experience for me to get back into C# again. So here it is, another one.

Besides automating the mini game, just like the other tools do, this one includes the following features:
- Interactive console with various commands
- Customizable settings, like strategy, planet override, game time

## How to use
### Windows
1. [Download and install .NET Framework](https://www.microsoft.com/net/download/dotnet-framework-runtime) if you don't have it already (4.5.1 is required)
2. [Download the latest Windows build from AppVeyor](https://ci.appveyor.com/project/Archomeda/AutoSaliens/branch/master/artifacts)
3. Extract the archive to a separate folder
4. Run AutoSaliens.exe
5. Follow instructions in the console

### Other operating systems
1. Download and install .NET Core Runtime if you don't have it already (2.0 is required): [Linux](https://docs.microsoft.com/en-us/dotnet/core/linux-prerequisites?tabs=netcore2x), [macOS](https://docs.microsoft.com/en-us/dotnet/core/macos-prerequisites?tabs=netcore2x)
2. [Download the latest Portable build from AppVeyor](https://ci.appveyor.com/project/Archomeda/AutoSaliens/branch/master/artifacts)
3. Extract the archive to a separate folder
4. Run `dotnet AutoSaliens.dll`
5. Follow instructions in the console

## Commands
The list below might not be up-to-date. You can find the up-to-date command list of your version by executing the `help` command.

```
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
token [token]      - Shows or sets the token
```

## Compiling
This project uses Visual Studio 2017 and .NET Framework 4.5.1 or .NET Core 2.0. There are no specific build instructions.
