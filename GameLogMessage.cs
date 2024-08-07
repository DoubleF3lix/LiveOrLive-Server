namespace liveorlive_server {
    public class GameLogMessage {
        public readonly string message;
        public readonly long timestamp = (long)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds; // Current UNIX time in seconds

        public GameLogMessage(string message) {
            this.message = message;
        }
    }
}
