using System;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("gettoken")]
    internal class GetTokenCommand : CommandBase
    {
        private const string url = "https://steamcommunity.com/saliengame/gettoken";

        public override async Task RunAsync(string parameters, CancellationToken cancellationToken)
        {
            this.Logger?.LogCommandOutput($"Open your browser, and go to: {{url}}{url}{{reset}}{Environment.NewLine}" +
                $"After the page is opened, you'll be greeted with a bunch of text.{Environment.NewLine}" +
                $"Go to the line that starts with {{value}}\"token\"{{reset}} and copy the text that's between the quotes after that.{Environment.NewLine}" +
                $"Afterwards, run the command: {{command}}token {{param}}<your_token>{Environment.NewLine}" +
                $"where {{param}}<your_token>{{reset}} is replaced with your token.");
        }
    }
}
