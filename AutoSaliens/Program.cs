using System.IO;
using System.Threading.Tasks;
using AutoSaliens.Api.Converters;
using AutoSaliens.Console;
using Newtonsoft.Json;

namespace AutoSaliens
{
    internal static class Program
    {
        public const string HomepageUrl = "https://github.com/Archomeda/AutoSaliens";

#if DEBUG
        public static bool Debug { get; set; } = true;
#else
        public static bool Debug { get; set; } = false;
#endif

        public static Saliens Saliens { get; } = new Saliens();

        public static Settings Settings { get; private set; } = new Settings();


        static Program()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ContractResolver = new SnakeCasePropertyNamesContractResolver()
            };
        }

        public static Task Main(string[] args)
        {
            Shell.WriteLine("[c=White]Auto[/c][c=Purple]S[/c][c=Orange]a[/c][c=Fuchsia]l[/c][c=Lime]i[/c][c=Red]e[/c][c=Turquoise]n[/c][c=Yellow]s[/c] - A Saliens mini game automation tool", false);
            Shell.WriteLine("Author: Archomeda", false);
            Shell.WriteLine($"Homepage: {HomepageUrl}", false);
            Shell.WriteLine("", false);
            Shell.WriteLine("[c=Lime]This console is interactive, type \"help\" to get the list of available commands.[/c]", false);

            return Start();
        }

        public static async Task Start()
        {
            if (File.Exists("settings.json"))
            {
                var settingsJson = File.ReadAllText("settings.json");
                Settings = JsonConvert.DeserializeObject<Settings>(settingsJson);
                Debug = Settings.Debug;
                Saliens.GameTime = Settings.GameTime;
                if (Saliens.GameTime < 1)
                    Saliens.GameTime = 120;
                Saliens.Strategy = Settings.Strategy;
                if (Saliens.Strategy == 0)
                {
                     Saliens.Strategy =
                        AutomationStrategy.TopDown |
                        AutomationStrategy.MostCompletedPlanetsFirst |
                        AutomationStrategy.MostCompletedZonesFirst |
                        AutomationStrategy.MostDifficultPlanetsFirst |
                        AutomationStrategy.MostDifficultZonesFirst;
                }
                Saliens.OverridePlanetId = Settings.OverridePlanetId;
                Saliens.Token = Settings.Token;
                Shell.WriteLine("", false);
                Shell.WriteLine("Read settings from settings.json");

#if !DEBUG
                await Task.WhenAll(Shell.StartRead(), Saliens.Start());
#else
                Shell.WriteLine("[c=Lime]Debug build: type \"resume\" to start automation[/c]");
                await Shell.StartRead();
#endif
            }
            else
            {
                Shell.WriteLine("[c=Lime]It seems like that this is your first time running this application! Type \"getstarted\" to get started.[/c]", false);
                Shell.WriteLine("", false);

                await Shell.StartRead();
            }
        }

        public static Task Stop()
        {
            return Task.Run(() =>
            {
                Saliens.Stop();
                Shell.StopRead();
            });
        }
    }
}
