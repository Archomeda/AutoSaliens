using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("pause")]
    internal class PauseCommand : CommandBase
    {
        public override async Task RunAsync(string parameters, CancellationToken cancellationToken)
        {
            this.Logger?.LogCommandOutput("Halting tasks...");
            Program.Stop();
        }
    }
}
