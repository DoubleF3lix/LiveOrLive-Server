using Newtonsoft.Json;

namespace backend_server {
    public class Chat {
        [JsonProperty]
        List<ChatMessage> messages = new List<ChatMessage>();

        public void addMessage(ChatMessage message) {
            this.messages.Add(message);
        }

        public ChatMessage addMessage(Player author, string content) {
            ChatMessage newMessage = new ChatMessage(author, content);
            this.messages.Add(newMessage);
            return newMessage;
        }

        public List<ChatMessage> getMessages() {  
            return this.messages; 
        }
    }
}
