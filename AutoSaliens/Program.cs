using System.IO;
using System.Threading.Tasks;
using System.Timers;
using AutoSaliens.Api.Converters;
using AutoSaliens.Console;
using Newtonsoft.Json;

namespace AutoSaliens
{
    internal static class Program
    {
        public const string HomepageUrl = "https://github.com/Archomeda/AutoSaliens";

        private static readonly Timer updateCheckerTimer = new Timer(10 * 60 * 1000);


#if DEBUG
        public static bool Debug { get; set; } = true;
#else
        public static bool Debug { get; set; } = false;
#endif

        public static bool HasUpdate { get; private set; } = false;

        public static bool HasUpdateBranch { get; private set; } = false;

        public static Saliens Saliens { get; } = new Saliens();

        public static Settings Settings { get; private set; } = new Settings();


        static Program()
        {
            updateCheckerTimer.Elapsed += UpdateCheckerTimer_Elapsed;
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
            if (!string.IsNullOrWhiteSpace(UpdateChecker.AppVersion))
            {
                if (!string.IsNullOrWhiteSpace(UpdateChecker.AppBranch))
                    Shell.WriteLine($"Version: {{value}}{UpdateChecker.AppVersion} (not on master branch)");
                else
                    Shell.WriteLine($"Version: {{value}}{UpdateChecker.AppVersion}");
            }
            if (!string.IsNullOrWhiteSpace(UpdateChecker.AppDate))
                Shell.WriteLine($"Date: {{value}}{UpdateChecker.AppDate}");
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

                updateCheckerTimer.Start();

#if !DEBUG
                await Task.WhenAll(Shell.StartRead(), Saliens.Start());
#else
                Shell.WriteLine("{inf}Debug build: type {command}\"resume\"{/command}{inf} to start automation");
                await Shell.StartRead();
#endif
            }
            else
            {
                Shell.WriteLine("{inf}It seems like that this is your first time running this application! Type {command}\"getstarted\"{/command}{inf} to get started.", false);
                Shell.WriteLine("", false);

                await Shell.StartRead();
            }
        }

        public static Task Stop()
        {
            return Task.Run(() =>
            {
                updateCheckerTimer.Stop();
                Saliens.Stop();
                Shell.StopRead();
            });
        }

        private static async void UpdateCheckerTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (UpdateChecker.AppBranch != "master" && !HasUpdateBranch)
                HasUpdateBranch = await UpdateChecker.HasUpdateForBranch();
            if (!HasUpdate)
                HasUpdate = await UpdateChecker.HasUpdateForMaster();

            if (HasUpdate && UpdateChecker.AppBranch == "master")
                Shell.WriteLine($"{{inf}}An update is available. Visit the homepage at {{url}}{HomepageUrl}", false);
            else if (UpdateChecker.AppBranch != "master")
            {
                if (HasUpdateBranch)
                    Shell.WriteLine($"{{inf}}An update is available for your branch {{val}}{UpdateChecker.AppBranch}{{inf}}. Visit the homepage at {{url}}{HomepageUrl}", false);
                if (HasUpdate)
                    Shell.WriteLine($"{{inf}}An update is available for the {{val}}master{{inf}} branch. Check if it's worth going back from the {{val}}{UpdateChecker.AppBranch}{{inf}} branch. Visit the homepage at {{url}}{HomepageUrl}", false);
            }
        }
    }
}
