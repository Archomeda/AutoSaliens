using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1998

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("checkupdate")]
    internal class CheckUpdateCommand : CommandBase
    {
        public override async Task<string> Run(string parameters, CancellationToken cancellationToken)
        {
            if (!Program.HasUpdate && !Program.HasUpdateBranch)
                return "No update was available during the last check.";

            if (Program.HasUpdate && UpdateChecker.AppBranch == "stable")
                this.WriteConsole($"An update is available.");
            else if (UpdateChecker.AppBranch != "stable")
            {
                if (Program.HasUpdateBranch)
                    this.WriteConsole($"An update is available for your branch {{value}}{UpdateChecker.AppBranch}.");
                if (Program.HasUpdate)
                    this.WriteConsole($"An update is available for the {{value}}stable{{reset}} branch. Check if it's worth going back from the {{value}}{UpdateChecker.AppBranch}{{reset}} branch.");
            }
            if (Program.HasUpdate || Program.HasUpdateBranch)
                this.WriteConsole($"Visit the homepage at {{url}}{Program.HomepageUrl}");
            return "";
        }
    }
}
