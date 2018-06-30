using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("checkupdate")]
    internal class CheckUpdateCommand : CommandBase
    {
        public override async Task RunAsync(string parameters, CancellationToken cancellationToken)
        {
            if (!UpdateChecker.HasUpdate && !UpdateChecker.HasUpdateBranch)
            {
                this.Logger?.LogCommandOutput($"No update was available during the last check.");
                return;
            }

            if (UpdateChecker.AppBranch == "stable")
            {
#pragma warning disable CS0162 // Unreachable code detected: This code will run on AppVeyor builds
                if (UpdateChecker.HasUpdate && UpdateChecker.AppBranch == "stable")
                    this.Logger?.LogCommandOutput($"An update is available: {{value}}{UpdateChecker.UpdateVersion}.");
#pragma warning restore CS0162 // Unreachable code detected
            }
            else
            {
                if (UpdateChecker.HasUpdateBranch)
                    this.Logger?.LogCommandOutput($"An update is available for your branch {{value}}{UpdateChecker.AppBranch}{{reset}}: {{value}}{UpdateChecker.UpdateVersionBranch}{{reset}}.");
                if (UpdateChecker.HasUpdate)
                    this.Logger?.LogCommandOutput($"An update is available for the {{value}}stable{{reset}} branch: {{value}}{UpdateChecker.UpdateVersion}{{reset}}. Check if it's worth going back from the {{value}}{UpdateChecker.AppBranch}{{reset}} branch.");
            }
            this.Logger?.LogCommandOutput($"Visit the homepage at {{url}}{Program.HomepageUrl}");
        }
    }
}
