using Newtonsoft.Json;

namespace liveorlive_server {
    public class GameLog {
        [JsonProperty]
        readonly List<GameLogMessage> messages = [];

        public void AddMessage(GameLogMessage message) {
            this.messages.Add(message);
        }

        public List<GameLogMessage> GetMessages() {  
            return this.messages; 
        }

        public void Clear() {
            this.messages.Clear();
        }
    }
}
