using System.Threading;
using System.Threading.Tasks;
using AutoSaliens.Utils;

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("gettoken")]
    internal class GetTokenCommand : CommandBase
    {
        private const string url = "https://steamcommunity.com/saliengame/gettoken";

        public override async Task<string> Run(string parameters, CancellationToken cancellationToken)
        {
            this.WriteConsole($"Your default browser will be opened and redirected to {url}");
            Browser.OpenDefault(url);
            this.WriteConsole("If your browser didn't open, please open it manually and visit that page.");
            this.WriteConsole("After the page is opened, you'll be greeted with a bunch of text.");
            this.WriteConsole("Go to the line that starts with \"token\" and copy the text that's between the quotes after that.");
            this.WriteConsole("Afterwards, run the command: token <your_token>");
            this.WriteConsole("where <your_token> is replaced with your token.");
            return "";
        }
    }
}
