using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace liveorlive_server {
    public class PacketJSONConverter : JsonConverter<ClientPacket> {
        public static T? tryParseEnum<T>(string input) where T : struct {
            if (input == null) {
                return null;
            }
            if (Enum.TryParse(input, true, out T parsedEnum)) {
                return parsedEnum;
            }
            return null;
        }

        public override ClientPacket? ReadJson(JsonReader reader, Type objectType, ClientPacket? existingValue, bool hasExistingValue, JsonSerializer serializer) {
            JObject data = JObject.Load(reader);
            string action = (string)data["packetType"];

            // We don't need to bother parsing server packets, because they come from the server
            // That happens on the client (TypeScript) side
            switch (action) {
                case "joinGame":
                    return new JoinGamePacket() { username = (string)data["username"] };
                case "setHost":
                    return new SetHostPacket() { username = (string)data["username"] };
                case "gameDataRequest":
                    return new GameDataRequestPacket();
                case "startGame":
                    return new StartGamePacket();
                case "shootPlayer":
                    return new ShootPlayerPacket() { target = (string)data["target"] };
                case "useSkipItem":
                    return new UseSkipItemPacket() { target = (string)data["target"] };
                case "useDoubleDamageItem":
                    return new UseDoubleDamageItemPacket();
                case "useCheckBulletItem":
                    return new UseCheckBulletItemPacket();
                case "useRebalancerItem":
                    AmmoType? ammoType = tryParseEnum<AmmoType>((string)data["ammoType"]);
                    if (ammoType == null) {
                        throw new JsonSerializationException($"Invalid ammo type for useRebalancerItem packet: {data["ammoType"]}");
                    }
                    return new UseRebalancerItemPacket() { ammoType = (AmmoType)ammoType };
                case "useAdrenalineItem":
                    return new UseAdrenalineItemPacket();
                case "useAddLifeItem":
                    return new UseAddLifeItemPacket();
                case "useQuickshotItem":
                    return new UseQuickshotItemPacket();
                case "useStealItem":
                    Item? item = tryParseEnum<Item>((string)data["item"]);
                    if (item == null) {
                        throw new JsonSerializationException($"Invalid item ID for useItem packet: {data["itemID"]}");
                    }

                    if (item == Item.Rebalancer && data["ammoType"] == null) {
                        throw new JsonSerializationException("Stolen item was Rebalancer but ammoType was null");
                    } else if (item == Item.SkipPlayerTurn && data["skipTarget"] == null) {
                        throw new JsonSerializationException("Stolen item was SkipPlayerTurn but skipTarget was null");
                    }

                    // If it's supposed to be set, parse it
                    ammoType = null;
                    Console.WriteLine(data["ammoType"]);
                    if (!string.IsNullOrEmpty((string?)data["ammoType"])) { 
                        ammoType = tryParseEnum<AmmoType>((string)data["ammoType"]);
                        if (ammoType == null) {
                            throw new JsonSerializationException($"Invalid ammo type for useStealItem packet: {data["ammoType"]}");
                        }
                    }

                    return new UseStealItemPacket() { 
                        target = (string)data["target"], 
                        item = (Item)item,
                        ammoType = ammoType,
                        skipTarget = (string)data["skipTarget"]
                    };
                case "sendNewChatMessage":
                    return new SendNewChatMessagePacket() { content = (string)data["content"] };
                case "chatMessagesRequest":
                    return new ChatMessagesRequestPacket();
                case "gameLogMessagesRequest":
                    return new GameLogMessagesRequestPacket();
                case "kickPlayer":
                    return new KickPlayerPacket { username = (string)data["username"] };
                default:
                    throw new JsonSerializationException($"Invalid packet type (action not valid): {data.ToString()}");
            }
        }

        public override bool CanWrite { get { return false; } }

        public override void WriteJson(JsonWriter writer, ClientPacket? value, JsonSerializer serializer) {
            throw new NotImplementedException();
        }
    }
}
