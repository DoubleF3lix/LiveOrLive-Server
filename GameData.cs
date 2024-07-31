using Newtonsoft.Json;

namespace liveorlive_server {
    public class GameData {
        public List<Player> players = [];
        public string? host = null;
        public bool gameStarted = false;
        public string? currentTurn = null;
        public readonly string gameID = Guid.NewGuid().ToString();

        private List<string> turnOrder = []; // Usernames
        private int currentTurnIndex;

        private ItemDeck itemDeck;
        private AmmoDeck ammoDeck;

        public GameData() {
            this.itemDeck = new ItemDeck(this.players.Count);
            this.ammoDeck = new AmmoDeck();
        }

        public void startGame() {
            this.turnOrder = this.players.Where(player => player.inGame == true).Select(player => player.username).ToList();
            this.currentTurnIndex = -1;
            this.gameStarted = true;
        }

        public List<int> newRound() {
            this.itemDeck.refresh();
            // Give all players their items
            foreach (Player player in this.players) {
                player.setItems(this.itemDeck.getSetForPlayer());
            }
            this.ammoDeck.refresh(); // Load the chamber
            return new List<int> { this.ammoDeck.liveCount, this.ammoDeck.blankCount }; // Used in the outgoing packet
        }

        public Player startNewTurn() {
            this.currentTurnIndex = (this.currentTurnIndex + 1) % this.turnOrder.Count;
            Player playerForTurn = this.getCurrentPlayerForTurn();
            this.currentTurn = playerForTurn.username;
            return playerForTurn;
        }

        public Player getCurrentPlayerForTurn() {
            return this.getPlayerByUsername(this.turnOrder[this.currentTurnIndex]);
        }

        public List<Player> getActivePlayers() {
            return this.players.Where(player => player.inGame == true && player.isSpectator == false).ToList();
        }

        private Player getPlayerByUsername(string username) {
            return this.players.Find(player => player.username == username);
        }

        // Remove player from the turnOrder list, adjusting the index backwards if necessary to avoid influencing order
        // Also marks as spectator
        public void eliminatePlayer(string username) {
            int index = this.turnOrder.IndexOf(username);
            if (index != -1) {
                this.turnOrder.RemoveAt(index);
                if (index < this.currentTurnIndex) {
                    this.currentTurnIndex--;
                }
                this.currentTurnIndex = this.currentTurnIndex % this.turnOrder.Count;
            }
            this.getPlayerByUsername(username).isSpectator = true;
        }
    }
}
