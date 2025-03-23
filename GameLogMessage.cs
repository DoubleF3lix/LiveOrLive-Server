using Tapper;

namespace liveorlive_server {
    [TranspilationSource]
    public class GameLogMessage(string message) {
        public readonly string message = message;
        public readonly long timestamp = (long)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds; // Current UNIX time in seconds
    }
}
