using System.Threading;
using System.Threading.Tasks;

namespace AutoSaliens.Console
{
    internal interface ICommand
    {
        ILogger Logger { get; set; }

        string Verb { get; }

        Task RunAsync(string parameters, CancellationToken cancellationToken);
    }
}
