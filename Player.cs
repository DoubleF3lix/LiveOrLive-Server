using liveorlive_server.Enums;
using System.Text.Json.Serialization;
using Tapper;

namespace liveorlive_server
{
    [TranspilationSource]
    public class Player(Config config, string username, string connectionId, bool isSpectator = false) {
        public string username = username;
        [JsonIgnore]
        public string connectionId = connectionId;

        // Needed to keep track of players who have left without kicking them for disconnects
        public bool inGame = true;
        public bool isSpectator = isSpectator;

        public int lives = config.DefaultLives;
        public List<Item> items = new(config.MaxItems);
        public bool isSkipped = false;

        public readonly long joinTime = (long)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds;

        public override bool Equals(object? obj) {
            return obj is Player other && this.Equals(other);
        }

        public bool Equals(Player player) {
            return this.username == player.username;
        }

        public override string ToString() {
            return $"Player {{ username = \"{this.username}\", lives = {this.lives}, inGame = {this.inGame}, isSpectator = {this.isSpectator}, isSkipped = {this.isSkipped}, items = [{string.Join(", ", this.items)}] }}";
        }

        public override int GetHashCode() {
            throw new NotImplementedException();
        }
    }
}
