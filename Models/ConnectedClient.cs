using LiveOrLiveServer.Enums;
using LiveOrLiveServer.Models.Dto;

namespace LiveOrLiveServer.Models {
    public abstract class ConnectedClient(string username, string? connectionId) {
        public string Username { get; set; } = username;
        // Null if not connected
        public string? ConnectionId { get; set; } = connectionId;

        public long JoinTime { get; } = (long)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds;
        public ClientType ClientType => Enum.TryParse<ClientType>(GetType().Name, true, out var result) ? result : default;

        public override bool Equals(object? obj) {
            return obj is ConnectedClient other && Username == other.Username;
        }

        // Stop VS warnings since we override Equals()
        public override int GetHashCode() {
            return HashCode.Combine(Username, ConnectionId ?? "", JoinTime);
        }

        public ConnectedClientDto ToDto() {
            return new ConnectedClientDto { 
                Username = Username, 
                JoinTime = JoinTime,
                ClientType = ClientType, 
            };
        }
    }
}
