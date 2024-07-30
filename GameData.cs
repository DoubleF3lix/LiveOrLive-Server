﻿namespace liveorlive_server {
    public class GameData {
        public List<Player> players = [];
        public string? host = null;
        public bool gameStarted = false;
        public readonly string gameID = Guid.NewGuid().ToString();

        private List<string> turnOrder = []; // Usernames
        private int turnCount = -1;

        private ItemDeck itemDeck;
        private AmmoDeck ammoDeck;

        public GameData() {
            this.itemDeck = new ItemDeck(this.players.Count);
            this.ammoDeck = new AmmoDeck();
        }

        public void startGame() {
            this.turnOrder = this.players.Where(player => player.inGame == true).Select(player => player.username).ToList();
            this.turnCount = 1;
            this.gameStarted = true;
        }

        public List<int> newRound() {
            this.itemDeck.refresh();
            // Give all players their items
            foreach (Player player in this.players) {
                player.setItems(this.itemDeck.getSetForPlayer());
            }

            this.ammoDeck.refresh();
            return new List<int> { this.ammoDeck.liveCount, this.ammoDeck.blankCount };
        }

        private Player getPlayerByUsername(string username) {
            return this.players.Find(player => player.username == username);
        }

        public Player getCurrentPlayerForTurn() {
            return this.getPlayerByUsername(this.turnOrder[this.turnCount - 1]);
        }

        public List<Player> getActivePlayers() {
            return this.players.Where(player => player.inGame == true && player.isSpectator == false).ToList();
        }
    }
}
