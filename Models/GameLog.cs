using Tapper;

namespace LiveOrLiveServer.Models {
    [TranspilationSource]
    public class GameLog {
        public List<GameLogMessage> Messages { get; } = [];

        public GameLogMessage AddMessage(string content) {
            var newMessage = new GameLogMessage(content);
            Messages.Add(newMessage);
            return newMessage;
        }

        public void Clear() {
            Messages.Clear();
        }
    }
}
