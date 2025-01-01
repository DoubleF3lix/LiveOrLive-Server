using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace liveorlive_server {
    public class PacketJSONConverter : JsonConverter<IClientPacket> {
        public static T? TryParseEnum<T>(string input) where T : struct {
            if (input == null) {
                return null;
            }
            if (Enum.TryParse(input, true, out T parsedEnum)) {
                return parsedEnum;
            }
            return null;
        }

        public override IClientPacket? ReadJson(JsonReader reader, Type objectType, IClientPacket? existingValue, bool hasExistingValue, JsonSerializer serializer) {
            JObject data = JObject.Load(reader);
            string action = data.Value<string>("packetType") ?? throw new JsonSerializationException("Missing packetType field");

            // We don't need to bother parsing server packets, because they come from the server
            // That happens on the client (TypeScript) side
            switch (action) {
                case "joinGame":
                    return new JoinGamePacket() { username = data.Value<string>("username") ?? throw new ArgumentException("Missing username field") };
                case "setHost":
                    return new SetHostPacket() { username = data.Value<string>("username") ?? throw new ArgumentException("Missing username field") };
                case "gameDataRequest":
                    return new GameDataRequestPacket();
                case "startGame":
                    return new StartGamePacket();
                case "shootPlayer":
                    return new ShootPlayerPacket() { target = data.Value<string>("target") ?? throw new JsonSerializationException("Missing target field") };
                case "useSkipItem":
                    return new UseSkipItemPacket() { target = data.Value<string>("target") ?? throw new JsonSerializationException("Missing target field") };
                case "useDoubleDamageItem":
                    return new UseDoubleDamageItemPacket();
                case "useCheckBulletItem":
                    return new UseCheckBulletItemPacket();
                case "useRebalancerItem":
                    string ammoTypeArg = data.Value<string>("ammoType") ?? throw new JsonSerializationException("Missing ammoType field");
                    AmmoType ammoType = TryParseEnum<AmmoType>(ammoTypeArg) ?? throw new JsonSerializationException($"Invalid ammo type for useRebalancerItem packet: {data["ammoType"]}");
                    return new UseRebalancerItemPacket() { ammoType = ammoType };
                case "useAdrenalineItem":
                    return new UseAdrenalineItemPacket();
                case "useAddLifeItem":
                    return new UseAddLifeItemPacket();
                case "useQuickshotItem":
                    return new UseQuickshotItemPacket();
                case "useStealItem":
                    string itemArg = data.Value<string>("item") ?? throw new JsonSerializationException("Missing item field");
                    Item item = TryParseEnum<Item>(itemArg) ?? throw new JsonSerializationException($"Invalid item ID for useItem packet: {data["item"]}");

                    if (item == Item.Rebalancer && data["ammoType"] == null) {
                        throw new JsonSerializationException("Stolen item was Rebalancer but ammoType was null");
                    } else if (item == Item.SkipPlayerTurn && data["skipTarget"] == null) {
                        throw new JsonSerializationException("Stolen item was SkipPlayerTurn but skipTarget was null");
                    }

                    // If it's supposed to be set, parse it
                    AmmoType? ammoType2 = null;
                    if (!string.IsNullOrEmpty((string?)data["ammoType"])) { 
                        string ammoTypeArg2 = data.Value<string>("ammoType") ?? throw new JsonSerializationException("Missing ammoType field");
                        ammoType2 = TryParseEnum<AmmoType>(ammoTypeArg2) ?? throw new JsonSerializationException($"Invalid ammo type for useStealItem packet: {data["ammoType"]}");
                    }

                    return new UseStealItemPacket() { 
                        target = data.Value<string>("target") ?? throw new JsonSerializationException("Missing target field"), 
                        item = item,
                        ammoType = ammoType2,
                        skipTarget = data.Value<string>("skipTarget")
                    };
                case "sendNewChatMessage":
                    return new SendNewChatMessagePacket() { content = data.Value<string>("content") ?? throw new JsonSerializationException("Missing content field") };
                case "chatMessagesRequest":
                    return new ChatMessagesRequestPacket();
                case "gameLogMessagesRequest":
                    return new GameLogMessagesRequestPacket();
                case "kickPlayer":
                    return new KickPlayerPacket { username = data.Value<string>("username") ?? throw new JsonSerializationException("Missing username field") };
                default:
                    throw new JsonSerializationException($"Invalid packet type (action not valid): {data}");
            }
        }

        public override bool CanWrite { get { return false; } }

        public override void WriteJson(JsonWriter writer, IClientPacket? value, JsonSerializer serializer) {
            throw new NotImplementedException();
        }
    }
}
