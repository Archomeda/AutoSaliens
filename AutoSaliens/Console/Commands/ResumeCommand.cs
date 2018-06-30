using System.Threading;
using System.Threading.Tasks;

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("resume")]
    internal class ResumeCommand : CommandBase
    {
        public override async Task RunAsync(string parameters, CancellationToken cancellationToken)
        {
            this.Logger?.LogCommandOutput("Starting tasks...");
            await Program.Start();
        }
    }
}
