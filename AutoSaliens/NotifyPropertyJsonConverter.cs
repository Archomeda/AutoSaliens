using System;
using System.Reflection;
using Newtonsoft.Json;

namespace AutoSaliens
{
    internal class NotifyPropertyJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            Type innerType = objectType.GenericTypeArguments[0];
            var obj = serializer.Deserialize(reader, innerType);
            if (existingValue != null)
            {
                objectType.GetField("value", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(existingValue, obj);
                return existingValue;
            }
            return Activator.CreateInstance(objectType, obj);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var obj = value.GetType().GetField("value", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(value);
            serializer.Serialize(writer, obj);
        }
    }
}
