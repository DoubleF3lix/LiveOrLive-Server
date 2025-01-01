namespace liveorlive_server {
    public class GameData {
        public const bool LOOTING = true;
        public const bool VENGEANCE = true;

        public List<Player> players = [];
        public string? host = null;
        public bool gameStarted = false;
        public readonly string gameID = Guid.NewGuid().ToString();

        public string CurrentTurn { get { return this.turnOrderManager.CurrentTurn; } }
        public int damageForShot = 1;
        public bool quickshotEnabled = false;

        private readonly TurnOrderManager turnOrderManager;
        private readonly ItemDeck itemDeck;
        private readonly AmmoDeck ammoDeck;

        public GameData() {
            this.turnOrderManager = new TurnOrderManager();
            this.itemDeck = new ItemDeck(this.players.Count);
            this.ammoDeck = new AmmoDeck();
        }

        public void StartGame() {
            this.turnOrderManager.Populate(this.players);
            this.gameStarted = true;
        }

        public List<int> NewRound() {
            this.itemDeck.Refresh();
            // Give all players their items
            foreach (Player player in this.players) {
                player.SetItems(this.itemDeck.GetSetForPlayer());
            }
            this.ammoDeck.Refresh(); // Load the chamber
            return [this.ammoDeck.LiveCount, this.ammoDeck.BlankCount]; // Used in the outgoing packet
        }

        public Player StartNewTurn() {
            this.turnOrderManager.Advance();
            // Guarunteed to not return a null unless something has gone horribly wrong
            return this.GetCurrentPlayerForTurn()!;
        }

        public AmmoType PullAmmoFromChamber() {
            return this.ammoDeck.Pop();
        }

        public int GetAmmoInChamberCount() {
            return this.ammoDeck.Count;
        }

        public AmmoType PeekAmmoFromChamber() {
            return this.ammoDeck.Peek();
        }

        public int AddAmmoToChamberAndShuffle(AmmoType type) {
            int count = this.ammoDeck.AddAmmo(type);
            this.ammoDeck.Shuffle();
            return count;
        }

        public Player? GetCurrentPlayerForTurn() {
            return this.GetPlayerByUsername(this.turnOrderManager.CurrentTurn);
        }

        public List<Player> GetActivePlayers() {
            return this.players.Where(player => player.inGame == true && player.isSpectator == false).ToList();
        }

        public Player? GetPlayerByUsername(string? username) {
            if (username == null) return null;
            return this.players.Find(player => player.username == username);
        }

        // Remove player from the turnOrder list, adjusting the index backwards if necessary to avoid influencing order
        // Also marks as spectator
        public void EliminatePlayer(Player player) {
            this.turnOrderManager.EliminatePlayer(player.username);
            player.isSpectator = true;
            player.lives = 0;
        }
    }
}
