using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace src
{
    public class AdminProcessDto
    {
        [JsonConverter(typeof(DictionaryStringStringConverter))]
        public Dictionary<string, string> Inputs { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public AdminProcessDto()
        {
        }
    }

    public class DictionaryStringStringConverter : JsonConverter<Dictionary<string, string>>
    {
        public override Dictionary<string, string> ReadJson(JsonReader reader, Type objectType, Dictionary<string, string> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var dictionary = serializer.Deserialize<Dictionary<string, string>>(reader);
            return new Dictionary<string, string>(dictionary, StringComparer.OrdinalIgnoreCase);
        }

        public override void WriteJson(JsonWriter writer, Dictionary<string, string> value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}
