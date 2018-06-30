using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace AutoSaliens.Console
{
    internal abstract class CommandBase : ICommand
    {
        private string verb;

        public ILogger Logger { get; set; }

        public string Verb
        {
            get
            {
                if (this.verb == null)
                    this.verb = this.GetType().GetCustomAttribute<CommandVerbAttribute>(false).Verb;
                return this.verb;
            }
        }

        public abstract Task RunAsync(string parameters, CancellationToken cancellationToken);
    }
}
