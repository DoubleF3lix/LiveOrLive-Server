namespace liveorlive_server {
    public class GameData {
        public List<Player> players = [];
        public string? host = null;
        public bool gameStarted = false;
        public readonly string gameID = Guid.NewGuid().ToString();

        public string currentTurn { get { return this.turnOrderManager.currentTurn; } }
        public int damageForShot = 1;

        private TurnOrderManager turnOrderManager;
        private ItemDeck itemDeck;
        private AmmoDeck ammoDeck;

        public GameData() {
            this.turnOrderManager = new TurnOrderManager();
            this.itemDeck = new ItemDeck(this.players.Count);
            this.ammoDeck = new AmmoDeck();
        }

        public void startGame() {
            this.turnOrderManager.populate(this.players);
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
            this.turnOrderManager.advance();
            return this.getCurrentPlayerForTurn();
        }

        public AmmoType pullAmmoFromChamber() {
            return this.ammoDeck.pop();
        }

        public int getAmmoInChamberCount() {
            return this.ammoDeck.Count;
        }

        public Player getCurrentPlayerForTurn() {
            return this.getPlayerByUsername(this.turnOrderManager.currentTurn);
        }

        public List<Player> getActivePlayers() {
            return this.players.Where(player => player.inGame == true && player.isSpectator == false).ToList();
        }

        public Player getPlayerByUsername(string username) {
            return this.players.Find(player => player.username == username);
        }

        // Remove player from the turnOrder list, adjusting the index backwards if necessary to avoid influencing order
        // Also marks as spectator
        public void eliminatePlayer(Player player) {
            this.turnOrderManager.eliminatePlayer(player.username);
            player.isSpectator = true;
            player.lives = 0;
        }
    }
}
