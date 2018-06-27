using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("resume")]
    internal class ResumeCommand : CommandBase
    {
        public override async Task<string> Run(string parameters, CancellationToken cancellationToken)
        {
            if (Program.Saliens.AutomationActive)
                return "Automation is already running.";

            Program.Settings.EnableBot.Value = true;

            return "Automation has been resumed.";
        }
    }
}
