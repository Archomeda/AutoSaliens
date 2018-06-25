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
            return $@"Open your browser, and go to: {{url}}{url}{{reset}}
After the page is opened, you'll be greeted with a bunch of text.
Go to the line that starts with {{value}}""token""{{reset}} and copy the text that's between the quotes after that." +
"Afterwards, run the command: {command}token {param}<your_token>" +
"where {param}<your_token>{reset} is replaced with your token.";
        }
    }
}
