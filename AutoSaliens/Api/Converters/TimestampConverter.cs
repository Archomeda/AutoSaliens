using System;
using Newtonsoft.Json;

namespace AutoSaliens.Api.Converters
{
    internal class TimestampConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => typeof(DateTime).IsAssignableFrom(objectType);

        public override bool CanWrite => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            int timestamp = serializer.Deserialize<int>(reader);
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timestamp).ToLocalTime();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => throw new NotImplementedException();
    }
}
