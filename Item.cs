using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace liveorlive_server {
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Item {
        DoubleDamage,
        CheckBullet,
        SkipPlayerTurn,
        ShootAir,
        RerollCounts,
        AddLife
    }
}
