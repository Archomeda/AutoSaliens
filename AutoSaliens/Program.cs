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
            Shell.WriteLine("AutoSaliens - A Saliens mini game automation tool", false);
            Shell.WriteLine("Author: Archomeda", false);
            Shell.WriteLine($"Homepage: {HomepageUrl}", false);
            Shell.WriteLine("", false);
            Shell.WriteLine("This console is interactive, type \"help\" to get the list of available commands.", false);

            return Start();
        }

        public static async Task Start()
        {
            if (File.Exists("settings.json"))
            {
                var settingsJson = File.ReadAllText("settings.json");
                Settings = JsonConvert.DeserializeObject<Settings>(settingsJson);
                Debug = Settings.Debug;
                if (Settings.GameTime < 1)
                    Settings.GameTime = 110;
                if (Settings.Strategy == 0)
                {
                     Settings.Strategy =
                        AutomationStrategy.TopDown |
                        AutomationStrategy.MostCompletedPlanetsFirst |
                        AutomationStrategy.MostCompletedZonesFirst |
                        AutomationStrategy.MostDifficultPlanetsFirst |
                        AutomationStrategy.MostDifficultZonesFirst;
                }
                Shell.WriteLine("", false);
                Shell.WriteLine("Read settings from settings.json");

#if !DEBUG
                await Task.WhenAll(Shell.StartRead(), Saliens.Start());
#else
                Shell.WriteLine("Debug build: type \"resume\" to start automation");
                await Shell.StartRead();
#endif
            }
            else
            {
                Shell.WriteLine("It seems like that this is your first time running this application! Type \"getstarted\" to get started.", false);
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
