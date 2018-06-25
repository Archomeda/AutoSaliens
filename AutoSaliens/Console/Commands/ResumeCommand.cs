using System.Threading;
using System.Threading.Tasks;

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("resume")]
    internal class ResumeCommand : CommandBase
    {
        public override async Task<string> Run(string parameters, CancellationToken cancellationToken)
        {
            if (Program.Saliens.AutomationActive)
                return "Automation is already running.";

            await Program.Saliens.Start();

            // Deactivate checking periodically
            if (Program.Settings.EnableDiscordPresence)
                Program.Presence.CheckPeriodically = false;

            return "Automation has been resumed.";
        }
    }
}
