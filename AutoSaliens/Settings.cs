using System.IO;
using System.Threading.Tasks;
using AutoSaliens.Console;
using Newtonsoft.Json;

namespace AutoSaliens
{
    internal class Settings
    {
        public bool Debug { get; set; }

        public bool EnableNetworkTolerance { get; set; } = true;

        public int GameTime { get; set; } = 110;

        public string OverridePlanetId { get; set; }

        public AutomationStrategy Strategy { get; set; }

        public string Token { get; set; }

        public void Save()
        {
            File.WriteAllText("settings.json", JsonConvert.SerializeObject(this, Formatting.Indented));
            Shell.WriteLine("Saved settings to settings.json");
        }
    }
}
