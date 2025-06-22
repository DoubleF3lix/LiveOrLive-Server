using System.Text.Json.Serialization;
using Tapper;

namespace liveorlive_server.Models {
    [TranspilationSource]
    public class ConnectedClient(string username, string? connectionId) {
        public string Username { get; set; } = username;
        [JsonIgnore]
        public string? ConnectionId { get; set; } = connectionId;

        public readonly long joinTime = (long)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds;

        public override bool Equals(object? obj) {
            return obj is ConnectedClient other && Equals(other);
        }

        public bool Equals(ConnectedClient client) {
            return Username == client.Username;
        }

        // Stop VS warnings since we override Equals()
        public override int GetHashCode() {
            return HashCode.Combine(Username, ConnectionId ?? "", joinTime);
        }
    }
}
