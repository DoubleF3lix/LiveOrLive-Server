namespace backend_server {
    public class ChatMessage {
        public readonly Player author;
        public readonly string message;
        public readonly long timestamp = (long)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds; // Current UNIX time in seconds

        // May add other properties later, like style
        public ChatMessage(Player author, string message) {
            this.author = author;
            this.message = message;
        }
    }
}
