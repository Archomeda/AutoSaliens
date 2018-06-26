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

        private static readonly Timer updateCheckerTimer = new Timer(60 * 60 * 1000);


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
                if (!string.IsNullOrWhiteSpace(UpdateChecker.AppBranch) && UpdateChecker.AppBranch != "stable")
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
                        AutomationStrategy.MostDifficultPlanetsFirst |
                        AutomationStrategy.MostCompletedPlanetsFirst |
                        AutomationStrategy.MostDifficultZonesFirst |
                        AutomationStrategy.LeastCompletedZonesFirst |
                        AutomationStrategy.TopDown;
                }
                if (Settings.DiscordPresenceTimeType == 0)
                    Settings.DiscordPresenceTimeType = PresenceTimeType.TimeElapsed;
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
            var newUpdates = await UpdateChecker.CheckForNewUpdates();
            if (!newUpdates)
                return;

            if (UpdateChecker.AppBranch == "stable")
            {
#pragma warning disable CS0162 // Unreachable code detected: This code will run on AppVeyor builds
                if (UpdateChecker.HasUpdate && UpdateChecker.AppBranch == "stable")
                    Shell.WriteLine($"{{inf}}An update is available: {{value}}{UpdateChecker.UpdateVersion}");
#pragma warning restore CS0162 // Unreachable code detected
            }
            else
            {
                if (UpdateChecker.HasUpdateBranch)
                    Shell.WriteLine($"{{inf}}An update is available for your branch {{value}}{UpdateChecker.AppBranch}{{inf}}: {{value}}{UpdateChecker.UpdateVersionBranch}");
                if (UpdateChecker.HasUpdate)
                    Shell.WriteLine($"{{inf}}An update is available for the {{value}}stable{{inf}} branch: {{value}}{UpdateChecker.UpdateVersion}{{inf}}. Check if it's worth going back from the {{value}}{UpdateChecker.AppBranch}{{inf}} branch");
            }
            Shell.WriteLine($"{{inf}}Visit the homepage at {{url}}{HomepageUrl}");
        }
    }
}
