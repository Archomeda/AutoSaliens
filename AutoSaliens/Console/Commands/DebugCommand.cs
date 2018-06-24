using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1998

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("debug")]
    internal class DebugCommand : CommandBase
    {
        public override async Task<string> Run(string parameters, CancellationToken cancellationToken)
        {
            Program.Debug = !Program.Debug;
            Program.Settings.Debug = Program.Debug;
            Program.Settings.Save();
            return Program.Debug ?
                "Debug mode has been turned on, exceptions will be redirected to the debugger." :
                "Debug mode has been turned off, exceptions will not be caught by the debugger.";
        }
    }
}
