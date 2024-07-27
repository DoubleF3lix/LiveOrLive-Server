namespace liveorlive_server {
    public class GameData {
        public List<Player> players = [];
        public string? host = null;
        public int turnCount = -1;
        public readonly string gameID = Guid.NewGuid().ToString();

        private List<string> turnOrder = []; // Usernames
        private bool gameStarted = false;

        private ItemDeck itemDeck;
        private List<AmmoType> ammoDeck = [];

        public GameData() {
            this.itemDeck = new ItemDeck(this.players.Count);
        }

        public void startGame() {
            this.newRound();
        }

        public void newRound() {

        }
    }
}
