using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
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


        public static bool HasUpdate { get; private set; } = false;

        public static bool HasUpdateBranch { get; private set; } = false;

        static Program()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ContractResolver = new SnakeCasePropertyNamesContractResolver()
            };

            updateCheckerTimer.Elapsed += async (s, e) => await CheckForUpdates();
        }

        public static SaliensPresence Presence { get; } = new SaliensPresence();

        public static Saliens Saliens { get; } = new Saliens();

        public static Settings Settings { get; private set; } = new Settings();


        public static Task Main(string[] args)
        {
            Shell.WriteLine("AutoSaliens - A Saliens mini game automation tool", false);
            Shell.WriteLine("Author: Archomeda", false);
            Shell.WriteLine($"Homepage: {{url}}{HomepageUrl}", false);
            if (!string.IsNullOrWhiteSpace(UpdateChecker.AppVersion))
            {
                if (!string.IsNullOrWhiteSpace(UpdateChecker.AppBranch))
                    Shell.WriteLine($"Version: {{value}}{UpdateChecker.AppVersion}{{reset}} (not on stable branch)", false);
                else
                    Shell.WriteLine($"Version: {{value}}{UpdateChecker.AppVersion}", false);
            }
            if (!string.IsNullOrWhiteSpace(UpdateChecker.AppDate))
                Shell.WriteLine($"Date: {{value}}{UpdateChecker.AppDate}", false);
            Shell.WriteLine("", false);
            Shell.WriteLine("{inf}This console is interactive, type {command}help{inf} to get the list of available commands.", false);

            return Start();
        }

        public static async Task Start()
        {
            if (File.Exists("settings.json"))
            {
                var settingsJson = File.ReadAllText("settings.json");
                Settings = JsonConvert.DeserializeObject<Settings>(settingsJson);
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
                var tasks = new List<Task>() { CheckForUpdates() };

                if (Settings.EnableDiscordPresence)
                {
                    Shell.WriteLine("{verb}Initializing Discord presence...");
                    tasks.Add(Presence.Start());
                    if (!Settings.EnableBot)
                        Presence.CheckPeriodically = true;
                }
#if !DEBUG
                if (Settings.EnableBot)
                {
                    Shell.WriteLine("{verb}Initializing bot...");
                    tasks.Add(Saliens.Start());
                }
#else
                Shell.WriteLine("{inf}Debug build: type {command}resume{inf} to start automation");
#endif
                tasks.Add(Shell.StartRead());

                await Task.WhenAll(tasks);
            }
            else
            {
                Shell.WriteLine("{inf}It seems like that this is your first time running this application! Type {command}getstarted{inf} to get started.", false);
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
                Presence.Stop();
                Shell.StopRead();
            });
        }

        private static async Task CheckForUpdates()
        {
            if (UpdateChecker.AppBranch != "stable" && !HasUpdateBranch)
                HasUpdateBranch = await UpdateChecker.HasUpdateForBranch();
            if (!HasUpdate)
                HasUpdate = await UpdateChecker.HasUpdateForStable();

            if (HasUpdate && UpdateChecker.AppBranch == "stable")
                Shell.WriteLine($"{{inf}}An update is available");
            else if (UpdateChecker.AppBranch != "stable")
            {
                if (HasUpdateBranch)
                    Shell.WriteLine($"{{inf}}An update is available for your branch {{value}}{UpdateChecker.AppBranch}{{inf}}");
                if (HasUpdate)
                    Shell.WriteLine($"{{inf}}An update is available for the {{value}}stable{{inf}} branch. Check if it's worth going back from the {{value}}{UpdateChecker.AppBranch}{{inf}} branch");
            }
            if (HasUpdate || HasUpdateBranch)
                    Shell.WriteLine($"{{inf}}Visit the homepage at {{url}}{HomepageUrl}");
        }
    }
}
