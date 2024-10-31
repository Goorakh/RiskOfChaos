using Newtonsoft.Json;
using System;

namespace RiskOfChaos.Serialization.Converters
{
    public class XoroshiroRngConverter : JsonConverter<Xoroshiro128Plus>
    {
        public override void WriteJson(JsonWriter writer, Xoroshiro128Plus value, JsonSerializer serializer)
        {
            byte[] state0bytes = BitConverter.GetBytes(value.state0);
            byte[] state1Bytes = BitConverter.GetBytes(value.state1);

            writer.WriteValue(Convert.ToBase64String([.. state0bytes, .. state1Bytes]));
        }

        public override Xoroshiro128Plus ReadJson(JsonReader reader, Type objectType, Xoroshiro128Plus existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            if (reader.TokenType != JsonToken.String)
                throw new JsonReaderException($"Cannot read Xoroshiro128Plus, expected string, got {reader.TokenType}");

            string rngStateString = (string)reader.Value;

            byte[] stateBytes = Convert.FromBase64String(rngStateString);

            ulong state0 = BitConverter.ToUInt64(new ArraySegment<byte>(stateBytes, sizeof(ulong) * 0, sizeof(ulong)));
            ulong state1 = BitConverter.ToUInt64(new ArraySegment<byte>(stateBytes, sizeof(ulong) * 1, sizeof(ulong)));

            Xoroshiro128Plus result = new Xoroshiro128Plus(0)
            {
                state0 = state0,
                state1 = state1
            };

            return result;
        }
    }
}
