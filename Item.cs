using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace liveorlive_server {
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Item {
        SkipPlayerTurn,
        DoubleDamage,
        CheckBullet,
        Rebalancer,
        Adrenaline,
        StealItem,
        AddLife,
        Quickshot
    }
}
