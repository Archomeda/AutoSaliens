using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace AutoSaliens.Console
{
    internal abstract class CommandBase : ICommand
    {
        private string verb;

        public string Verb
        {
            get
            {
                if (this.verb == null)
                    this.verb = this.GetType().GetCustomAttribute<CommandVerbAttribute>(false).Verb;
                return this.verb;
            }
        }

        public abstract Task<string> Run(string parameters, CancellationToken cancellationToken);

        protected void WriteConsole(string format, params string[] args) =>
            Shell.WriteLine(string.Join(Environment.NewLine, format
                .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => $"> {l}")), false, args);
    }
}
