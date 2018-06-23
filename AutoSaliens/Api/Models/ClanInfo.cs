using Newtonsoft.Json;

namespace AutoSaliens.Api.Models
{
    internal class ClanInfo
    {
        [JsonProperty(PropertyName = "accountid")]
        public int AccountId { get; set; }

        public string Name { get; set; }

        public string Avatar { get; set; }

        public string Url { get; set; }
    }
}
