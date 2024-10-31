using Newtonsoft.Json;
using RoR2;
using System;

namespace RiskOfChaos.Serialization.Converters
{
    public abstract class CatalogValueConverter<TValue> : JsonConverter<TValue>
    {
        readonly TValue _invalidValue;

        protected CatalogValueConverter(TValue invalidValue)
        {
            _invalidValue = invalidValue;
        }

        protected abstract string getCatalogName(TValue value);

        protected abstract TValue findFromCatalog(string catalogName);

        public override void WriteJson(JsonWriter writer, TValue value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteValue(string.Empty);
                return;
            }
            
            if (!SystemInitializerAttribute.hasExecuted)
            {
                Log.Error("Cannot write catalog value before catalogs are initialized");
                writer.WriteValue(string.Empty);
                return;
            }

            writer.WriteValue(getCatalogName(value));
        }

        public override TValue ReadJson(JsonReader reader, Type objectType, TValue existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (!SystemInitializerAttribute.hasExecuted)
            {
                Log.Error("Cannot read catalog value before catalogs are initialized");
                return _invalidValue;
            }

            string catalogName = string.Empty;
            if (reader.TokenType == JsonToken.String)
            {
                catalogName = (string)reader.Value;
            }

            if (string.IsNullOrEmpty(catalogName))
                return _invalidValue;

            return findFromCatalog(catalogName);
        }
    }
}
