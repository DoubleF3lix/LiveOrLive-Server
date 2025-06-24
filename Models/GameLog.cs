using Tapper;

namespace liveorlive_server.Models {
    [TranspilationSource]
    public class GameLog {
        public List<GameLogMessage> Messages { get; } = [];

        public GameLogMessage AddMessage(string content) {
            var newMessage = new GameLogMessage(content);
            this.Messages.Add(newMessage);
            return newMessage;
        }

        public void Clear() {
            this.Messages.Clear();
        }
    }
}
