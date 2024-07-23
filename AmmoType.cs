using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace liveorlive_server {
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AmmoType {
        Live,
        Blank
    }
}