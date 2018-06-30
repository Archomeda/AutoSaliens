using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using AutoSaliens.Api.Converters;
using AutoSaliens.Bot;
using AutoSaliens.Console;
using AutoSaliens.Presence;
using AutoSaliens.Presence.Formatters;
using Newtonsoft.Json;
using Timer = System.Timers.Timer;

namespace AutoSaliens
{
    internal static class Program
    {
        public const string HomepageUrl = "https://github.com/Archomeda/AutoSaliens";

        private static readonly Timer updateCheckerTimer = new Timer(60 * 60 * 1000);
        private static readonly SaliensBot bot = new SaliensBot();
        private static readonly DiscordPresence presence = new DiscordPresence();


        public static ILogger Logger { get; } = new ConsoleLogger();

        public static bool Paused { get; private set; } = true;

        public static Settings Settings { get; private set; } = new Settings();


        private static void DiscordPresenceTimeType_Changed(object sender, PropertyChangedEventArgs<PresenceFormatterType> e) =>
            SetDiscordPresenceFormatter(e.NewValue);

        private static void EnableBot_Changed(object sender, PropertyChangedEventArgs<bool> e)
        {
            if (Paused)
                return;

            if (Settings.EnableDiscordPresence)
                SetDiscordPresence(e.NewValue ? PresenceActivationType.EnabledWithBot : PresenceActivationType.EnabledPresenceOnly);

            SetSaliensBot(e.NewValue);
        }

        private static void EnableDiscordPresence_Changed(object sender, PropertyChangedEventArgs<bool> e)
        {
            if (Paused)
                return;

            if (e.NewValue)
                SetDiscordPresence(Settings.EnableBot ? PresenceActivationType.EnabledWithBot : PresenceActivationType.EnabledPresenceOnly);
            else
                SetDiscordPresence(PresenceActivationType.Disabled);
        }

        private static void EnableNetworkTolerance_Changed(object sender, PropertyChangedEventArgs<bool> e) =>
            bot.EnableNetworkTolerance = e.NewValue;

        private static void GameTime_Changed(object sender, PropertyChangedEventArgs<int> e) =>
            bot.GameTime = e.NewValue;

        private static void OverridePlanetId_Changed(object sender, PropertyChangedEventArgs<string> e) =>
            bot.OverridePlanetId = e.NewValue;

        private static void Strategy_Changed(object sender, PropertyChangedEventArgs<BotStrategy> e) =>
            bot.Strategy = e.NewValue;

        private static void Token_Changed(object sender, PropertyChangedEventArgs<string> e)
        {
            if (presence.UpdateTrigger is ApiIntervalUpdateTrigger)
                ((ApiIntervalUpdateTrigger)presence.UpdateTrigger).ApiToken = e.NewValue;
            bot.Token = e.NewValue;
        }


        private static void SetSaliensBot(bool enabled)
        {
            if (enabled)
            {
                Logger?.LogMessage("{verb}Enabling bot...");
                bot.EnableNetworkTolerance = Settings.EnableNetworkTolerance;
                bot.GameTime = Settings.GameTime;
                bot.OverridePlanetId = Settings.OverridePlanetId;
                bot.Strategy = Settings.Strategy;
                bot.Token = Settings.Token;
                bot.Start();
            }
            else
            {
                Logger?.LogMessage("{verb}Disabling bot...");
                bot.Stop();
            }

            if (Settings.EnableDiscordPresence)
                SetDiscordPresence(enabled ? PresenceActivationType.EnabledWithBot : PresenceActivationType.EnabledPresenceOnly);
        }

        private static void SetDiscordPresence(PresenceActivationType type)
        {
            if (type != PresenceActivationType.Disabled)
                SetDiscordPresenceFormatter(Settings.DiscordPresenceTimeType);
            if (type != PresenceActivationType.EnabledPresenceOnly)
                (presence.UpdateTrigger as ApiIntervalUpdateTrigger)?.Stop();

            switch (type)
            {
                case PresenceActivationType.Disabled:
                    if (!presence.HasPresenceStarted)
                        break;
                    Logger?.LogMessage("{verb}Disabling Discord presence...");
                    presence.Stop();
                    break;
                case PresenceActivationType.EnabledPresenceOnly:
                    if (presence.HasPresenceStarted)
                        break;
                    Logger?.LogMessage("{verb}Initializing Discord presence separately...");
                    if (presence.UpdateTrigger == null)
                    {
                        presence.UpdateTrigger = new ApiIntervalUpdateTrigger(Settings.Token);
                        (presence.UpdateTrigger as ApiIntervalUpdateTrigger).Start();
                    }
                    presence.Start();
                    break;
                case PresenceActivationType.EnabledWithBot:
                    if (presence.HasPresenceStarted)
                        break;
                    Logger?.LogMessage("{verb}Initializing Discord presence through bot...");
                    presence.UpdateTrigger = bot.PresenceUpdateTrigger;
                    presence.Start();
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


        public static async Task Main(string[] args)
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
            Shell.WriteLine("{warn}Hey there! If you're running the bot, please be advised that dealing with boss levels is experimental and untested.", false);
            Shell.WriteLine("{warn}If either the bot and/or Discord presence is too unstable in this version, please revert to 1.0.103-stable.", false);
            Shell.WriteLine("{inf}This console is interactive, type {command}help{inf} to get the list of available commands.", false);


            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ContractResolver = new SnakeCasePropertyNamesContractResolver()
            };
            updateCheckerTimer.Elapsed += async (s, e) => await CheckForUpdates();

            bot.BotActivated += Bot_BotActivated;
            bot.BotDeactivated += Bot_BotDeactivated;
            bot.Logger = Logger;
            presence.PresenceActivated += Presence_PresenceActivated;
            presence.PresenceDeactivated += Presence_PresenceDeactivated;
            presence.Logger = Logger;

            if (File.Exists("settings.json"))
            {
                var settingsJson = File.ReadAllText("settings.json");
                Settings = JsonConvert.DeserializeObject<Settings>(settingsJson);

                if (Settings.GameTime < 110)
                    Settings.GameTime.Value = 110;
                if (Settings.Strategy == (BotStrategy)0)
                {
                    Settings.Strategy.Value =
                       BotStrategy.MostDifficultPlanetsFirst |
                       BotStrategy.MostCompletedPlanetsFirst |
                       BotStrategy.MostDifficultZonesFirst |
                       BotStrategy.LeastCompletedZonesFirst |
                       BotStrategy.TopDown;
                }
                if (Settings.DiscordPresenceTimeType == (PresenceFormatterType)0)
                    Settings.DiscordPresenceTimeType.Value = PresenceFormatterType.TimeZoneElapsed;
            }
            else
            {
                Shell.WriteLine("{inf}It seems that this is your first time running this application! Type {command}getstarted{inf} to get started.", false);
                Shell.WriteLine("", false);
            }

            Settings.DiscordPresenceTimeType.Changed += DiscordPresenceTimeType_Changed;
            Settings.EnableBot.Changed += EnableBot_Changed;
            Settings.EnableDiscordPresence.Changed += EnableDiscordPresence_Changed;
            Settings.EnableNetworkTolerance.Changed += EnableNetworkTolerance_Changed;
            Settings.GameTime.Changed += GameTime_Changed;
            Settings.OverridePlanetId.Changed += OverridePlanetId_Changed;
            Settings.Strategy.Changed += Strategy_Changed;
            Settings.Token.Changed += Token_Changed;

#if DEBUG
            Shell.WriteLine("{inf}Debug build: type {command}resume{inf} to resume tasks");
#else
            await Start();
#endif

            await Shell.StartRead();
        }

        private static void Bot_BotActivated(object sender, EventArgs e)
        {
            Logger.LogMessage("{inf}Bot activated");
        }

        private static void Bot_BotDeactivated(object sender, EventArgs e)
        {
            Logger.LogMessage("{inf}Bot deactivated");

        }

        private static void Presence_PresenceActivated(object sender, EventArgs e)
        {
            Logger.LogMessage("{inf}Discord presence activated");

        }

        private static void Presence_PresenceDeactivated(object sender, EventArgs e)
        {
            Logger.LogMessage("{inf}Discord presence deactivated");

        }

        public static async Task Start()
        {
            if (!Paused)
                return;
            Paused = false;

            updateCheckerTimer.Start();
            var tasks = new List<Task>() { CheckForUpdates() };

            SetSaliensBot(Settings.EnableBot);
            if (Settings.EnableDiscordPresence)
                SetDiscordPresence(Settings.EnableBot ? PresenceActivationType.EnabledWithBot : PresenceActivationType.EnabledPresenceOnly);

            await Task.WhenAll(tasks);
        }

        public static void Stop()
        {
            if (Paused)
                return;
            Paused = true;

            updateCheckerTimer.Stop();
            bot.Stop();
            presence.Stop();
            (presence.UpdateTrigger as ApiIntervalUpdateTrigger)?.Stop();
        }

        public static void Exit()
        {
            Stop();
            Shell.StopRead();
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
