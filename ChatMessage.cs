namespace liveorlive_server {
    public class ChatMessage(Player author, string message) {
        public readonly Player author = author;
        public readonly string message = message;
        public readonly long timestamp = (long)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds; // Current UNIX time in seconds
    }
}
