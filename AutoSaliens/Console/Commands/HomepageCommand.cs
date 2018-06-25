using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1998

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("homepage")]
    internal class HomepageCommand : CommandBase
    {
        public override async Task<string> Run(string parameters, CancellationToken cancellationToken)
        {
            return $"You can visit the homepage at {{url}}{Program.HomepageUrl}";
        }
    }
}
