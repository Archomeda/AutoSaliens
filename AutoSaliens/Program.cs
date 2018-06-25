using System;
using System.Collections.Generic;
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

        static Program()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ContractResolver = new SnakeCasePropertyNamesContractResolver()
            };
        }

        public static SaliensPresence Presence { get; } = new SaliensPresence();

        public static Saliens Saliens { get; } = new Saliens();

        public static Settings Settings { get; private set; } = new Settings();


        public static Task Main(string[] args)
        {
            Shell.WriteLine("AutoSaliens - A Saliens mini game automation tool", false);
            Shell.WriteLine("Author: Archomeda", false);
            Shell.WriteLine($"Homepage: {{url}}{HomepageUrl}", false);
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

                var tasks = new List<Task>();

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
                    task.Add(Saliens.Start());
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
                Saliens.Stop();
                Presence.Stop();
                Shell.StopRead();
            });
        }
    }
}
