using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Timers;
using AutoSaliens.Api.Converters;
using AutoSaliens.Console;
using AutoSaliens.Presence;
using AutoSaliens.Presence.Formatters;
using Newtonsoft.Json;

namespace AutoSaliens
{
    internal static class Program
    {
        public const string HomepageUrl = "https://github.com/Archomeda/AutoSaliens";

        private static readonly Timer updateCheckerTimer = new Timer(60 * 60 * 1000);
        private static readonly DiscordPresence presence = new DiscordPresence();


        public static Saliens Saliens { get; } = new Saliens();

        public static Settings Settings { get; private set; }


        private static void DiscordPresenceTimeType_Changed(object sender, PropertyChangedEventArgs<PresenceFormatterType> e)
        {
            SetDiscordPresenceFormatter(e.NewValue);
        }

        private static void EnableBot_Changed(object sender, PropertyChangedEventArgs<bool> e)
        {
            if (Settings.EnableDiscordPresence)
                SetDiscordPresence(e.NewValue ? PresenceActivationType.EnabledWithBot : PresenceActivationType.EnabledPresenceOnly);
        }

        private static void EnableDiscordPresence_Changed(object sender, PropertyChangedEventArgs<bool> e)
        {
            if (e.NewValue)
                SetDiscordPresence(Settings.EnableBot ? PresenceActivationType.EnabledWithBot : PresenceActivationType.EnabledPresenceOnly);
            else
                SetDiscordPresence(PresenceActivationType.Disabled);
        }

        private static void Token_Changed(object sender, PropertyChangedEventArgs<string> e)
        {
            if (presence.UpdateTrigger is ApiIntervalUpdateTrigger)
                ((ApiIntervalUpdateTrigger)presence.UpdateTrigger).ApiToken = e.NewValue;
        }


        private static void SetDiscordPresence(PresenceActivationType type)
        {
            if (type != PresenceActivationType.Disabled)
                SetDiscordPresenceFormatter(Settings.DiscordPresenceTimeType);

            switch (type)
            {
                case PresenceActivationType.Disabled:
                    presence.Stop();
                    (presence.UpdateTrigger as ApiIntervalUpdateTrigger)?.Stop();
                    Saliens.PresenceUpdateTrigger = null;
                    break;
                case PresenceActivationType.EnabledPresenceOnly:
                    {
                        var trigger = new ApiIntervalUpdateTrigger(Settings.Token);
                        presence.UpdateTrigger = trigger;
                        presence.Start();
                        Saliens.PresenceUpdateTrigger = null;
                        trigger.Start();
                    }
                    break;
                case PresenceActivationType.EnabledWithBot:
                    {
                        var trigger = new BotUpdateTrigger();
                        presence.UpdateTrigger = trigger;
                        presence.Start();
                        Saliens.PresenceUpdateTrigger = trigger;
                    }
                    break;
            }
        }

        private static void SetDiscordPresenceFormatter(PresenceFormatterType type)
        {
            switch (type)
            {
                case PresenceFormatterType.TimePlanetElapsed:
                    presence.Formatter = new PresenceTimePlanetElapsedFormatter();
                    break;
                case PresenceFormatterType.NextLevelEstimation:
                    presence.Formatter = new PresenceTimeNextLevelEstimationFormatter();
                    break;
                case PresenceFormatterType.TimeZoneElapsed:
                default:
                    presence.Formatter = new PresenceTimeZoneElapsedFormatter();
                    break;
            }
        }


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


            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ContractResolver = new SnakeCasePropertyNamesContractResolver()
            };
            updateCheckerTimer.Elapsed += async (s, e) => await CheckForUpdates();


            if (File.Exists("settings.json"))
            {
                var settingsJson = File.ReadAllText("settings.json");
                Settings = JsonConvert.DeserializeObject<Settings>(settingsJson);

#if DEBUG
                Settings.EnableBot.Value = false;
                Settings.EnableDiscordPresence.Value = false;
#endif

                if (Settings.GameTime < 1)
                    Settings.GameTime.Value = 110;
                if (Settings.Strategy == (AutomationStrategy)0)
                {
                    Settings.Strategy.Value =
                       AutomationStrategy.MostDifficultPlanetsFirst |
                       AutomationStrategy.MostCompletedPlanetsFirst |
                       AutomationStrategy.MostDifficultZonesFirst |
                       AutomationStrategy.LeastCompletedZonesFirst |
                       AutomationStrategy.TopDown;
                }
                if (Settings.DiscordPresenceTimeType == (PresenceFormatterType)0)
                    Settings.DiscordPresenceTimeType.Value = PresenceFormatterType.TimeZoneElapsed;
            }
            else
            {
                Shell.WriteLine("{inf}It seems like that this is your first time running this application! Type {command}getstarted{inf} to get started.", false);
                Shell.WriteLine("", false);
            }

#if DEBUG
            Shell.WriteLine("{inf}Debug build: type {command}resume{inf} to start automation or {command}presence {param}enable{inf} to start Discord presence");
#endif

            Settings.EnableBot.Changed += EnableBot_Changed;
            Settings.EnableDiscordPresence.Changed += EnableDiscordPresence_Changed;
            Settings.DiscordPresenceTimeType.Changed += DiscordPresenceTimeType_Changed;
            Settings.Token.Changed += Token_Changed;

            return Start();
        }

        public static Task Start()
        {
            updateCheckerTimer.Start();
            var tasks = new List<Task>() { CheckForUpdates() };

            if (Settings.EnableBot)
            {
                Shell.WriteLine("{verb}Initializing bot...");
                tasks.Add(Saliens.Start());
            }

            if (Settings.EnableDiscordPresence)
            {
                Shell.WriteLine("{verb}Initializing Discord presence...");
                SetDiscordPresence(Settings.EnableBot ? PresenceActivationType.EnabledWithBot : PresenceActivationType.EnabledPresenceOnly);
            }

            tasks.Add(Shell.StartRead());
            return Task.WhenAll(tasks);
        }

        public static Task Stop()
        {
            return Task.Run(() =>
            {
                updateCheckerTimer.Stop();
                Saliens.Stop();
                presence.Stop();
                (presence.UpdateTrigger as ApiIntervalUpdateTrigger)?.Stop();
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
