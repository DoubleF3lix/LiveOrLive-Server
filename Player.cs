using Newtonsoft.Json;

namespace backend_server {
    // Internal server representation of player data
    public class Player {
        public string username;
        public bool inGame;

        int lives = 0;
        [JsonProperty]
        List<Item> items = new List<Item>();

        public Player(string username, bool inGame = false) {
            this.username = username;
            this.inGame = inGame;
        }

        public override bool Equals(object? obj) {
            return obj is Player other && this.Equals(other);
        }

        public bool Equals(Player player) {
            return this.username == player.username;
        }

        public string ToString() {
            return $"Player {{ username = \"{this.username}\", lives = {this.lives}, items = [{string.Join(", ", this.items)}] }}";
        }
    }
}
