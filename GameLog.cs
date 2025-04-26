using Tapper;

namespace liveorlive_server {
    [TranspilationSource]
    public class GameLog {
        public List<GameLogMessage> Messages { get; } = [];

        public void AddMessage(GameLogMessage message) {
            this.Messages.Add(message);
        }

        public GameLogMessage AddMessage(string content) {
            var newMessage = new GameLogMessage(content);
            this.Messages.Add(newMessage);
            return newMessage;
        }

        public List<GameLogMessage> GetLastMessages(int count) {  
            return this.Messages.Slice(this.Messages.Count - count, this.Messages.Count); 
        }

        public void Clear() {
            this.Messages.Clear();
        }
    }
}
