using Tapper;

namespace liveorlive_server.Models {
    [TranspilationSource]
    public class Chat {
        public List<ChatMessage> Messages { get; private set; }  = [];

        public ChatMessage AddMessage(string author, string content) {
            var newMessage = new ChatMessage(author, content);
            this.Messages.Add(newMessage);
            return newMessage;
        }
    }
}
