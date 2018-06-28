using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("token")]
    internal class TokenCommand : CommandBase
    {
        public override async Task RunAsync(string parameters, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(parameters))
            {
                // Show the current token
                if (!string.IsNullOrWhiteSpace(Program.Saliens.Token))
                    this.Logger?.LogCommandOutput($"Your token is currently set to: {{value}}{Program.Saliens.Token}{{reset}}.");
                else
                    this.Logger?.LogCommandOutput("You have currently no token set.");

                this.Logger?.LogCommandOutput("You can change the token by appending the token to this command: {command}token {param}<your_token>");
                this.Logger?.LogCommandOutput("where {param}<your_token>{reset} is replaced with your token.");
            }
            else
            {
                // Set the token
                try
                {
                    Program.Saliens.PlayerInfo = await SaliensApi.GetPlayerInfoAsync(parameters);
                    Program.Settings.Token.Value = parameters;
                    this.Logger?.LogCommandOutput("Your token has been saved.");
                }
                catch (WebException ex)
                {
                    this.Logger?.LogCommandOutput($"{{err}}Invalid response. {ex.Message}");
                }
            }
        }
    }
}
