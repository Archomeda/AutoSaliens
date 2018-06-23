using System.Threading;
using System.Threading.Tasks;
using AutoSaliens.Utils;

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("homepage")]
    internal class HomepageCommand : CommandBase
    {
        public override async Task<string> Run(string parameters, CancellationToken cancellationToken)
        {
            this.WriteConsole($"Your default browser will be opened and redirected to {Program.HomepageUrl}");
            Browser.OpenDefault(Program.HomepageUrl);
            this.WriteConsole("If your browser didn't open, please open it manually and visit that page.");
            return "";
        }
    }
}
