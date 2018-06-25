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

        public static DiscordPresence Presence { get; } = new DiscordPresence();

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
            Shell.WriteLine($"Homepage: {{url}}{HomepageUrl}", false);
            Shell.WriteLine("", false);
            Shell.WriteLine("{inf}This console is interactive, type {command}\"help\"{/command}{inf} to get the list of available commands.", false);

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
                Shell.WriteLine("{verb}Read settings from settings.json");

                Presence.Initialize();
                Shell.WriteLine("{verb}Initialized Discord presence");

#if !DEBUG
                await Task.WhenAll(Shell.StartRead(), Saliens.Start());
#else
                Shell.WriteLine("{inf}Debug build: type {command}\"resume\"{/command}{inf} to start automation");
                await Shell.StartRead();
#endif
            }
            else
            {
                Presence.Initialize();
                Shell.WriteLine("{verb}Initialized Discord presence");

                Shell.WriteLine("{inf}It seems like that this is your first time running this application! Type {command}\"getstarted\"{/command}{inf} to get started.", false);
                Shell.WriteLine("", false);

                await Shell.StartRead();
            }
        }

        public static Task Stop()
        {
            return Task.Run(() =>
            {
                Saliens.Stop();
                Presence.Dispose();
                Shell.StopRead();
            });
        }
    }
}
