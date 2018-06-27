using System.IO;
using AutoSaliens.Presence;
using Newtonsoft.Json;

namespace AutoSaliens
{
    internal class Settings
    {
        public Settings()
        {
            this.EnableBot.Changed += (s, e) => this.Save();
            this.EnableDiscordPresence.Changed += (s, e) => this.Save();
            this.DiscordPresenceTimeType.Changed += (s, e) => this.Save();
            this.EnableNetworkTolerance.Changed += (s, e) => this.Save();
            this.GameTime.Changed += (s, e) => this.Save();
            this.OverridePlanetId.Changed += (s, e) => this.Save();
            this.Strategy.Changed += (s, e) => this.Save();
            this.Token.Changed += (s, e) => this.Save();
        }


        [JsonProperty]
        public NotifyProperty<bool> EnableBot { get; private set; } = false;

        [JsonProperty]
        public NotifyProperty<bool> EnableDiscordPresence { get; private set; } = false;

        [JsonProperty]
        public NotifyProperty<PresenceFormatterType> DiscordPresenceTimeType { get; private set; } = PresenceFormatterType.TimeZoneElapsed;

        [JsonProperty]
        public NotifyProperty<bool> EnableNetworkTolerance { get; private set; } = true;

        [JsonProperty]
        public NotifyProperty<int> GameTime { get; private set; } = 110;

        [JsonProperty]
        public NotifyProperty<string> OverridePlanetId { get; private set; } = "";

        [JsonProperty]
        public NotifyProperty<AutomationStrategy> Strategy { get; private set; } =
            AutomationStrategy.MostDifficultPlanetsFirst |
            AutomationStrategy.MostCompletedPlanetsFirst |
            AutomationStrategy.MostDifficultZonesFirst |
            AutomationStrategy.LeastCompletedZonesFirst |
            AutomationStrategy.TopDown;

        [JsonProperty]
        public NotifyProperty<string> Token { get; private set; } = "";


        public void Save()
        {
            File.WriteAllText("settings.json", JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }
}
