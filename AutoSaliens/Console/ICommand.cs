using System.Threading;
using System.Threading.Tasks;

namespace AutoSaliens.Console
{
    internal interface ICommand
    {
        string Verb { get; }

        Task<string> Run(string parameters, CancellationToken cancellationToken);
    }
}
