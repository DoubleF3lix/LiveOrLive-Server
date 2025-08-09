using Tapper;

namespace LiveOrLiveServer.Models {
    [TranspilationSource]
    public class ChatMessage(string author, string message) {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Author { get; set; } = author;
        public string Content { get; set; } = message;
        public long Timestamp { get; set; } = (long)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds;
    }
}
