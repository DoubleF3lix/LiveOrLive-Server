using Tapper;

namespace liveorlive_server {
    [TranspilationSource]
    public class Chat {
        public List<ChatMessage> Messages { get; private set; }  = [];

        public void AddMessage(ChatMessage message) {
            this.Messages.Add(message);
        }

        public ChatMessage AddMessage(string author, string content) {
            var newMessage = new ChatMessage(author, content);
            this.Messages.Add(newMessage);
            return newMessage;
        }

        public List<ChatMessage> GetMessages() {  
            return this.Messages; 
        }
    }
}
