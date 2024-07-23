namespace liveorlive_server {
    // Internal server representation of player data
    public class Player {
        public string username;
        public bool inGame;
        public bool isSpectator;

        public int lives = 5;
        public List<Item> items = new List<Item>(4);
        public bool isSkipped = false;

        public readonly long joinTime = (long)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds;

        public Player(string username, bool inGame = false, bool isSpectator = false) {
            this.username = username;
            this.inGame = inGame; // This is necessary since player objects persist after their associated client disconnects
            this.isSpectator = isSpectator;
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
            return $"Player {{ username = \"{this.username}\", lives = {this.lives}, inGame = {this.inGame}, isSpectator = {this.isSpectator}, isSkipped = {this.isSkipped}, items = [{string.Join(", ", this.items)}] }}";
        }
    }
}
