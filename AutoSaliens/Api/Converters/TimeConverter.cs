using System;
using Newtonsoft.Json;

namespace AutoSaliens.Api.Converters
{
    internal class TimeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => typeof(TimeSpan).IsAssignableFrom(objectType);

        public override bool CanWrite => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            int seconds = serializer.Deserialize<int>(reader);
            return TimeSpan.FromSeconds(seconds);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => throw new NotImplementedException();
    }
}
