using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace AutoSaliens.Console.Commands
{
    [CommandVerb("token")]
    internal class TokenCommand : CommandBase
    {
        public override async Task<string> Run(string parameters, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(parameters))
            {
                // Show the current token
                if (!string.IsNullOrWhiteSpace(Program.Saliens.Token))
                    this.WriteConsole($"Your token is currently set to: {{value}}{Program.Saliens.Token}{{reset}}.");
                else
                    this.WriteConsole("You have currently no token set.");

                this.WriteConsole("You can change the token by appending the token to this command: {command}token {param}<your_token>");
                this.WriteConsole("where {param}<your_token>{reset} is replaced with your token.");

                return "";
            }
            else
            {
                // Set the token
                try
                {
                    Program.Saliens.PlayerInfo = await SaliensApi.GetPlayerInfoAsync(parameters);
                    Program.Settings.Token.Value = parameters;
                    return "Your token has been saved.";
                }
                catch (WebException ex)
                {
                    return $"{{err}}Invalid response. {ex.Message}";
                }
            }
        }
    }
}
