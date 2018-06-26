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
            if (!UpdateChecker.HasUpdate && !UpdateChecker.HasUpdateBranch)
                return "No update was available during the last check.";

            if (UpdateChecker.AppBranch == "stable")
            {
#pragma warning disable CS0162 // Unreachable code detected: This code will run on AppVeyor builds
                if (UpdateChecker.HasUpdate && UpdateChecker.AppBranch == "stable")
                    this.WriteConsole($"An update is available: {{value}}{UpdateChecker.UpdateVersion}.");
#pragma warning restore CS0162 // Unreachable code detected
            }
            else
            {
                if (UpdateChecker.HasUpdateBranch)
                    this.WriteConsole($"An update is available for your branch {{value}}{UpdateChecker.AppBranch}{{reset}}: {{value}}{UpdateChecker.UpdateVersionBranch}.");
                if (UpdateChecker.HasUpdate)
                    this.WriteConsole($"An update is available for the {{value}}stable{{reset}} branch: {{value}}{UpdateChecker.UpdateVersion}{{reset}}. Check if it's worth going back from the {{value}}{UpdateChecker.AppBranch}{{inf}} branch.");
            }
            this.WriteConsole($"Visit the homepage at {{url}}{Program.HomepageUrl}");
            return "";
        }
    }
}
