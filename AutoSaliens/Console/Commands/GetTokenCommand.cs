using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1998

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("gettoken")]
    internal class GetTokenCommand : CommandBase
    {
        private const string url = "https://steamcommunity.com/saliengame/gettoken";

        public override async Task<string> Run(string parameters, CancellationToken cancellationToken)
        {
            this.WriteConsole($"Open your browser, and go to: {url}");
            this.WriteConsole("After the page is opened, you'll be greeted with a bunch of text.");
            this.WriteConsole("Go to the line that starts with \"token\" and copy the text that's between the quotes after that.");
            this.WriteConsole("Afterwards, run the command: token <your_token>");
            this.WriteConsole("where <your_token> is replaced with your token.");
            return "";
        }
    }
}
