using Newtonsoft.Json;
using System;
using UnityEngine.Networking;

namespace RiskOfChaos.Serialization.Converters
{
    public sealed class NetworkHash128Converter : JsonConverter<NetworkHash128>
    {
        public override void WriteJson(JsonWriter writer, NetworkHash128 value, JsonSerializer serializer)
        {
            byte[] bytes = [value.i0, value.i1, value.i2, value.i3, value.i4, value.i5, value.i6, value.i7, value.i8, value.i9, value.i10, value.i11, value.i12, value.i13, value.i14, value.i15];

            writer.WriteValue(Convert.ToBase64String(bytes));
        }

        public override NetworkHash128 ReadJson(JsonReader reader, Type objectType, NetworkHash128 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.String)
                throw new JsonReaderException($"Cannot read NetworkHash128, expected string, got {reader.TokenType}");

            string hash = (string)reader.Value;
            
            byte[] bytes = Convert.FromBase64String(hash);

            if (bytes.Length != 128 / 8)
                throw new JsonReaderException($"Invalid hash bytes, expected 16, got {bytes.Length}");

            NetworkHash128 result = new NetworkHash128
            {
                i0 = bytes[0],
                i1 = bytes[1],
                i2 = bytes[2],
                i3 = bytes[3],
                i4 = bytes[4],
                i5 = bytes[5],
                i6 = bytes[6],
                i7 = bytes[7],
                i8 = bytes[8],
                i9 = bytes[9],
                i10 = bytes[10],
                i11 = bytes[11],
                i12 = bytes[12],
                i13 = bytes[13],
                i14 = bytes[14],
                i15 = bytes[15]
            };

            return result;
        }
    }
}
