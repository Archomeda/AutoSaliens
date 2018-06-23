using Newtonsoft.Json;

namespace AutoSaliens.Api.Models
{
    internal class Clan
    {
        public ClanInfo ClanInfo { get; set; }

        [JsonProperty(PropertyName = "num_zones_controled")]
        public int NumZonesControlled { get; set; }
    }
}
