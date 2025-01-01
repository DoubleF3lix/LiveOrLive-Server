namespace liveorlive_server {
    // Internal server representation of player data
    public class Player(string username, bool inGame = false, bool isSpectator = false) {
        public const int DEFAULT_LIVES = 5;
        // TODO random amount of items per round
        public const int ITEM_COUNT = 4;

        public string username = username;
        public bool inGame = inGame;
        public bool isSpectator = isSpectator;

        public int lives = DEFAULT_LIVES;
        public List<Item> items = new List<Item>(ITEM_COUNT);
        public bool isSkipped = false;

        public readonly long joinTime = (long)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds;

        public void SetItems(List<Item> items) {
            this.items = items;
        }

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
