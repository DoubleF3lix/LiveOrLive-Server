namespace liveorlive_server {
    /// <summary>
    /// Manager for the turn order. Must call <c>Advance</c> to initialize the turn order before accessing <c>PlayerForCurrentTurn</c>.
    /// </summary>
    /// <param name="players">The list of players for the lobby. Spectators are automatically filtered out and can be safely included.</param>
    public class TurnOrderManager(List<Player> players) {
        private readonly List<Player> nonSpectatorPlayers = players.Where(p => !p.IsSpectator).ToList();
        private int currentTurnIndex = -1;

        /// <summary>
        /// Gets the turn order by usernames. Used for display and doesn't change after initialization.
        /// </summary>
        public List<string> TurnOrder => this.nonSpectatorPlayers.Select(p => p.Username).ToList();

        /// <summary>
        /// Gets the player instance of the current turn.
        /// </summary>
        /// <exception cref="Exception">Thrown if accessed without first calling <c>Advance</c>.</exception>
        public Player PlayerForCurrentTurn {
            get {
                if (this.currentTurnIndex < 0) {
                    throw new Exception("PlayerForCurrentTurn accessed without being initialized");
                }
                return this.nonSpectatorPlayers[this.currentTurnIndex];
            }
        }

        /// <summary>
        /// Advances the turn by one, skipping spectators and dead players, looping around as necessary. Does not skip skipped players, since that should be handled by the lobby itself.
        /// </summary>
        /// <exception cref="Exception">Thrown if there are no eligible players left in the turn order (all dead).</exception>
        public void Advance() {
            if (nonSpectatorPlayers.All(p => p.Lives <= 0)) {
                throw new Exception("Can't advance empty turn order");
            }

            Player? currentPlayerForTurn;
            do {
                this.currentTurnIndex = (this.currentTurnIndex + 1) % this.nonSpectatorPlayers.Count;
                currentPlayerForTurn = this.nonSpectatorPlayers[currentTurnIndex];
            } while (currentPlayerForTurn.Lives <= 0);
        }
    }
}
