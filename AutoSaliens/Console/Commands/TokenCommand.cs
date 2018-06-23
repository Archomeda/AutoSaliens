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
                    this.WriteConsole($"Your token is currently set to: {Program.Saliens.Token}");
                else
                    this.WriteConsole("You have currently no token set.");

                this.WriteConsole("You can change the token by appending the token to this command: token <your_token>");
                this.WriteConsole("where <your_token> is replaced with your token.");

                return "";
            }
            else
            {
                // Set the token
                try
                {
                    Program.Saliens.PlayerInfo = await SaliensApi.GetPlayerInfo(parameters);
                    Program.Saliens.Token = parameters;
                    Program.Settings.Token = Program.Saliens.Token;
                    Program.Settings.Save();
                    return "Your token has been saved.";
                }
                catch (WebException ex)
                {
                    return $"Invalid response. {ex.Message}";
                }
            }
        }
    }
}
