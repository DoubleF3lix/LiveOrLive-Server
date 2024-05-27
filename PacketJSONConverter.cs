using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace backend_server {
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
                case "sendNewChatMessage":
                    return new SendNewChatMessagePacket() { content = (string)data["content"] };
                case "joinGame":
                    return new JoinGamePacket() { username = (string)data["username"] };
                case "getGameInfo":
                    return new GetGameInfoPacket();
                    /*
                case "fireGun":
                    return new FireGunPacket() { target = this.dataToPlayer(data, null) };
                case "useItem":
                    Item item;
                    // We do this since if it fails to cast, it will default to 0 (double damage)
                    bool success = Enum.TryParse((string)data["item"], true, out item);
                    if (!success) {
                        throw new JsonSerializationException($"Invalid item ID for useItem packet: {data["itemID"]}");
                    }
                    return new UseItemPacket() { item = item, target = this.dataToPlayer(data, "target") };
                    */
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
