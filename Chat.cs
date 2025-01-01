using Newtonsoft.Json;

namespace liveorlive_server {
    public class Chat {
        [JsonProperty]
        readonly List<ChatMessage> messages = [];

        public void AddMessage(ChatMessage message) {
            this.messages.Add(message);
        }

        public ChatMessage AddMessage(Player author, string content) {
            ChatMessage newMessage = new(author, content);
            this.messages.Add(newMessage);
            return newMessage;
        }

        public List<ChatMessage> GetMessages() {  
            return this.messages; 
        }
    }
}
