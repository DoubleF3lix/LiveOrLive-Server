using Tapper;

namespace LiveOrLiveServer.Models {
    [TranspilationSource]
    public class GameLogMessage(string message) {
        public string Message { get; } = message;
        public long Timestamp { get; } = (long)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds;
    }
}
