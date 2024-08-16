using Newtonsoft.Json;

namespace liveorlive_server {
    public class GameLog {
        [JsonProperty]
        List<GameLogMessage> messages = new List<GameLogMessage>();

        public void addMessage(GameLogMessage message) {
            this.messages.Add(message);
        }

        public List<GameLogMessage> getMessages() {  
            return this.messages; 
        }

        public void clear() {
            this.messages.Clear();
        }
    }
}
