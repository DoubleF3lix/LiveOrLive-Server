using Newtonsoft.Json;

namespace liveorlive_server {
    // Internal server representation of player data
    public class Player {
        public string username;
        public bool inGame;

        [JsonProperty]
        int lives = 5;
        [JsonProperty]
        List<Item> items = new List<Item>(); // Max of 4 items

        public Player(string username, bool inGame = false) {
            this.username = username;
            this.inGame = inGame; // This is necessary since player objects persist after their associated client disconnects
        }

        public void setItems(List<Item> items) {
            this.items = items;
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
