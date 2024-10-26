using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RiskOfChaos.SaveHandling.DataContainers;
using System;
using System.Collections.Generic;

namespace RiskOfChaos.Serialization.Converters
{
    public sealed class ObjectFieldCollectionConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(SerializableObjectField[]) || objectType == typeof(List<SerializableObjectField>);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                reader.Read();
                return null;
            }

            JObject jObject = JObject.Load(reader);

            List<SerializableObjectField> fields = [];

            foreach (JProperty property in jObject.Properties())
            {
                fields.Add(new SerializableObjectField
                {
                    Name = property.Name,
                    Value = property.Value
                });
            }

            if (objectType == typeof(SerializableObjectField[]))
            {
                return fields.ToArray();
            }
            else if (objectType == typeof(List<SerializableObjectField>))
            {
                return fields;
            }
            else
            {
                throw new NotImplementedException($"Object type {objectType} is not implemented");
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            IEnumerable<SerializableObjectField> fields;
            if (value is SerializableObjectField[] or List<SerializableObjectField>)
            {
                fields = (IEnumerable<SerializableObjectField>)value;
            }
            else
            {
                throw new NotImplementedException($"Object type {value.GetType()} is not implemented");
            }

            writer.WriteStartObject();

            foreach (SerializableObjectField field in fields)
            {
                writer.WritePropertyName(field.Name);
                field.Value.WriteTo(writer);
            }

            writer.WriteEndObject();
        }
    }
}
