using liveorlive_server.Deck;
using liveorlive_server.Enums;
using Tapper;

namespace liveorlive_server
{
    [TranspilationSource]
    public class GameData {
        public List<Player> players = [];
        public string? host = null;
        public bool gameStarted = false;
        public readonly string gameID = Guid.NewGuid().ToString();

        public Player CurrentTurn { get { return this.turnOrderManager.PlayerForCurrentTurn; } }
        public int damageForShot = 1;

        private readonly TurnOrderManager turnOrderManager;
        private readonly ItemDeck itemDeck;
        private readonly AmmoDeck ammoDeck;

        public GameData() {
            this.turnOrderManager = new TurnOrderManager(null);
            this.itemDeck = new ItemDeck(new Settings());
            this.ammoDeck = new AmmoDeck(new Settings());
        }

        public void StartGame() {
            //this.turnOrderManager.Populate(this.players);
            this.gameStarted = true;
        }

        public List<int> NewRound() {
            this.itemDeck.Refresh();
            // Give all players their items
            foreach (Player player in this.players) {
                // player.items = this.itemDeck.GetSetForPlayer();
            }
            this.ammoDeck.Refresh(); // Load the chamber
            return [this.ammoDeck.LiveCount, this.ammoDeck.BlankCount]; // Used in the outgoing packet
        }

        public Player StartNewTurn() {
            this.turnOrderManager.Advance();
            // Guarunteed to not return a null unless something has gone horribly wrong
            return this.GetCurrentPlayerForTurn()!;
        }

        public BulletType PullAmmoFromChamber() {
            return this.ammoDeck.Pop();
        }

        public int GetAmmoInChamberCount() {
            return this.ammoDeck.Count;
        }

        public BulletType PeekAmmoFromChamber() {
            return this.ammoDeck.Peek();
        }

        //public int AddAmmoToChamberAndShuffle(BulletType type) {
        //    int count = this.ammoDeck.AddAmmo(type);
        //    this.ammoDeck.Shuffle();
        //    return count;
        //}

        public Player? GetCurrentPlayerForTurn() {
            return this.GetPlayerByUsername(this.turnOrderManager.PlayerForCurrentTurn.Username);
        }

        public List<Player> GetActivePlayers() {
            return this.players.Where(player => player.InGame == true && player.IsSpectator == false).ToList();
        }

        public Player? GetPlayerByUsername(string? username) {
            if (username == null) return null;
            return this.players.Find(player => player.Username == username);
        }

        // Remove player from the turnOrder list, adjusting the index backwards if necessary to avoid influencing order
        // Also marks as spectator
        public void EliminatePlayer(Player player) {
            //this.turnOrderManager.EliminatePlayer(player.Username);
            player.IsSpectator = true;
            player.Lives = 0;
        }
    }
}
