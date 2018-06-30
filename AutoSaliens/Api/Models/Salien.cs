using Newtonsoft.Json;

namespace AutoSaliens.Api.Models
{
    internal class Salien
    {
        public int BodyType { get; set; }

        public int Mouth { get; set; }

        public int Eyes { get; set; }

        public int Arms { get; set; }

        public int Legs { get; set; }

        [JsonProperty(PropertyName = "hat_itemid")]
        public string HatItemId { get; set; }

        [JsonProperty(PropertyName = "shirt_itemid")]
        public string ShirtItemId { get; set; }

        public string HatImage { get; set; }

        public string ShirtImage { get; set; }
    }
}
