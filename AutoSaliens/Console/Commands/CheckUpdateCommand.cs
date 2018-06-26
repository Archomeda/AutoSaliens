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
            if (string.IsNullOrWhiteSpace(Program.UpdateVersion) && string.IsNullOrWhiteSpace(Program.UpdateVersionBranch))
                return "No update was available during the last check.";

            if (!string.IsNullOrWhiteSpace(Program.UpdateVersion) && UpdateChecker.AppBranch == "stable")
                this.WriteConsole($"An update is available: {{value}}{Program.UpdateVersion}{{reset}}: {{value}}{Program.UpdateVersionBranch}");
            else if (UpdateChecker.AppBranch != "stable")
            {
                if (!string.IsNullOrWhiteSpace(Program.UpdateVersionBranch))
                    this.WriteConsole($"An update is available for your branch {{value}}{UpdateChecker.AppBranch}");
                if (!string.IsNullOrWhiteSpace(Program.UpdateVersion))
                    this.WriteConsole($"An update is available for the {{value}}stable{{reset}} branch: {{value}}{Program.UpdateVersion}{{inf}}. Check if it's worth going back from the {{value}}{UpdateChecker.AppBranch}{{reset}} branch");
            }
            if (!string.IsNullOrWhiteSpace(Program.UpdateVersion) || !string.IsNullOrWhiteSpace(Program.UpdateVersionBranch))
                this.WriteConsole($"Visit the homepage at {{url}}{Program.HomepageUrl}");
            return "";
        }
    }
}
