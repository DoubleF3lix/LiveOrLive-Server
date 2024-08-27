using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace liveorlive_server {
    public class PacketJSONConverter : JsonConverter<ClientPacket> {
        /*
        private Player dataToPlayer(JObject data, string nullConditionField) {
            if (nullConditionField != null && data[nullConditionField] == null) return null;
            return new Player(null, (string)data["player"]["username"]);
        }
        */

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
                    AmmoType ammoType;
                    bool success = Enum.TryParse((string)data["ammoType"], true, out ammoType);
                    if (!success) {
                        throw new JsonSerializationException($"Invalid ammo type for useRebalancerItem packet: {data["ammoType"]}");
                    }
                    return new UseRebalancerItemPacket() { ammoType = ammoType };
                case "useAdrenalineItem":
                    return new UseAdrenalineItemPacket();
                case "useAddLifeItem":
                    return new UseAddLifeItemPacket();
                case "useQuickshotItem":
                    return new UseQuickshotItemPacket();
                case "useStealItem":
                    Item item;
                    success = Enum.TryParse((string)data["item"], true, out item);
                    if (!success) {
                        throw new JsonSerializationException($"Invalid item ID for useItem packet: {data["itemID"]}");
                    }
                    return new UseStealItemPacket() { item = item, target = (string)data["username"] };
                case "sendNewChatMessage":
                    return new SendNewChatMessagePacket() { content = (string)data["content"] };
                case "chatMessagesRequest":
                    return new ChatMessagesRequestPacket();
                case "gameLogMessagesRequest":
                    return new GameLogMessagesRequestPacket();
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
